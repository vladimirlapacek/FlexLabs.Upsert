﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.Sqlite provider
    /// </summary>
    public class SqliteUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.Sqlite";
        protected override string Column(string name) => "\"" + name + "\"";
        protected override string Parameter(int index) => "@p" + index;
        protected override string SourcePrefix => "\"S\".";
        protected override string TargetPrefix => "\"T\".";

        protected override string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns,
            ICollection<string> updateColumns, List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            result.Append($"INSERT INTO \"{entityType.Relational().TableName}\" AS \"T\" (");
            result.Append(string.Join(", ", insertColumns.Select(c => Column(c))));
            result.Append(") VALUES (");
            foreach (var entity in Enumerable.Range(0, entityCount))
            {
                result.Append(string.Join(", ", insertColumns.Select((v, i) => Parameter(i + insertColumns.Count * entity))));
                if (entity < entityCount - 1 && entityCount > 1)
                    result.Append("), (");
            }
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => Column(c))));
            result.Append(") DO UPDATE SET ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"{Column(c)} = {Parameter(i + insertColumns.Count * entityCount)}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count * entityCount + updateColumns.Count;
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => ExpandExpression(i + argumentOffset, e.ColumnName, e.Value))));
            }
            return result.ToString();
        }
    }
}
