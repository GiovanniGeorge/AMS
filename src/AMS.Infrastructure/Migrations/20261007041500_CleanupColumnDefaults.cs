using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <summary>
    /// Hand-written migration: it changes no column, it only removes rubbish.
    ///
    /// When a nullable column was made required, EF wrote "AlterColumn ... defaultValue: 0",
    /// and SQL Server turned that into a permanent DEFAULT constraint on the column.
    /// AMS never wants those: a row inserted without a category, a status, a unit, an asset,
    /// an employee, a store or a custodian must fail loudly, not quietly become 0.
    /// The same applies to the DEFAULT 0 that came with Assets.LifecycleStage
    /// (the starting stage belongs in the C# entity, not in the database).
    ///
    /// The constraints have generated names (DF__Assets__Categor__12345678), so they are
    /// looked up in sys.default_constraints instead of being named here. The script is
    /// safe to run on a database that does not have them.
    /// </summary>
    public partial class CleanupColumnDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                   + N' DROP CONSTRAINT ' + QUOTENAME(d.name) + N';' + CHAR(13)
FROM sys.default_constraints AS d
JOIN sys.tables   AS t ON t.object_id = d.parent_object_id
JOIN sys.columns  AS c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
JOIN (VALUES
        ('Assets','CategoryId'),
        ('Assets','StatusId'),
        ('Assets','LifecycleStage'),
        ('Items','UnitOfMeasureId'),
        ('Issues','AssetId'),
        ('Issues','EmployeeId'),
        ('Issues','StoreId'),
        ('AssetAssignments','AssetId'),
        ('AssetAssignments','CustodianId')
     ) AS v(TableName, ColumnName)
  ON v.TableName = t.name AND v.ColumnName = c.name;

IF @sql <> N'' EXEC sp_executesql @sql;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty: putting the DEFAULT 0 constraints back would be a step backwards.
        }
    }
}
