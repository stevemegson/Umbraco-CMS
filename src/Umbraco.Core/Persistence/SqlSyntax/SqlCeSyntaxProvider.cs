﻿using System.Linq;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Umbraco.Core.Persistence.SqlSyntax
{
    /// <summary>
    /// Static class that provides simple access to the Sql CE SqlSyntax Providers singleton
    /// </summary>
    internal static class SqlCeSyntax
    {
        public static ISqlSyntaxProvider Provider { get { return SqlCeSyntaxProvider.Instance; } }
    }

    /// <summary>
    /// Represents an SqlSyntaxProvider for Sql Ce
    /// </summary>
    internal class SqlCeSyntaxProvider : SqlSyntaxProviderBase<SqlCeSyntaxProvider>
    {
        public static SqlCeSyntaxProvider Instance = new SqlCeSyntaxProvider();

        private SqlCeSyntaxProvider()
        {
            StringLengthColumnDefinitionFormat = StringLengthUnicodeColumnDefinitionFormat;
            StringColumnDefinition = string.Format(StringLengthColumnDefinitionFormat, DefaultStringLength);

            AutoIncrementDefinition = "IDENTITY(1,1)";
            StringColumnDefinition = "NVARCHAR(255)";
            GuidColumnDefinition = "UniqueIdentifier";
            RealColumnDefinition = "FLOAT";
            BoolColumnDefinition = "BIT";
            DecimalColumnDefinition = "DECIMAL(38,6)";
            TimeColumnDefinition = "TIME"; //SQLSERVER 2008+
            BlobColumnDefinition = "VARBINARY(MAX)";

            InitColumnTypeMap();
        }

        public override string GetIndexType(IndexTypes indexTypes)
        {
            string indexType;
            //NOTE Sql Ce doesn't support clustered indexes
            if (indexTypes == IndexTypes.Clustered)
            {
                indexType = "NONCLUSTERED";
            }
            else
            {
                indexType = indexTypes == IndexTypes.NonClustered
                    ? "NONCLUSTERED"
                    : "UNIQUE NONCLUSTERED";
            }
            return indexType;
        }

        public override string GetQuotedTableName(string tableName)
        {
            return string.Format("[{0}]", tableName);
        }

        public override string GetQuotedColumnName(string columnName)
        {
            return string.Format("[{0}]", columnName);
        }

        public override string GetQuotedName(string name)
        {
            return string.Format("[{0}]", name);
        }

        public override string FormatPrimaryKey(TableDefinition table)
        {
            var columnDefinition = table.Columns.FirstOrDefault(x => x.IsPrimaryKey);
            if (columnDefinition == null)
                return string.Empty;

            string constraintName = string.IsNullOrEmpty(columnDefinition.PrimaryKeyName)
                                        ? string.Format("PK_{0}", table.Name)
                                        : columnDefinition.PrimaryKeyName;

            string columns = string.IsNullOrEmpty(columnDefinition.PrimaryKeyColumns)
                                 ? GetQuotedColumnName(columnDefinition.Name)
                                 : columnDefinition.PrimaryKeyColumns;

            return string.Format(CreateConstraint,
                                 GetQuotedTableName(table.Name),
                                 GetQuotedName(constraintName),
                                 "PRIMARY KEY",
                                 columns);
        }

        public override bool DoesTableExist(Database db, string tableName)
        {
            var result =
                db.ExecuteScalar<long>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName",
                                       new { TableName = tableName });

            return result > 0;
        }

        protected override string FormatIdentity(ColumnDefinition column)
        {
            return column.IsIdentity ? GetIdentityString(column) : string.Empty;
        }

        private static string GetIdentityString(ColumnDefinition column)
        {
            if (column.Seeding != default(int))
                return string.Format("IDENTITY({0},1)", column.Seeding);

            return "IDENTITY(1,1)";
        }

        protected override string FormatSystemMethods(SystemMethods systemMethod)
        {
            switch (systemMethod)
            {
                case SystemMethods.NewGuid:
                    return "NEWID()";
                case SystemMethods.NewSequentialId:
                    return "NEWSEQUENTIALID()";
                case SystemMethods.CurrentDateTime:
                    return "GETDATE()";
                case SystemMethods.CurrentUTCDateTime:
                    return "GETUTCDATE()";
            }

            return null;
        }

        public override string AddColumn { get { return "ALTER TABLE {0} ADD {1}"; } }

        public override string DropIndex { get { return "DROP INDEX {1}.{0}"; } }
    }
}