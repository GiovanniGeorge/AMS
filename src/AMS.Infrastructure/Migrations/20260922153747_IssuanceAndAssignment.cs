using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IssuanceAndAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetAssignments_Employees_EmployeeId",
                table: "AssetAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Approvals_ApprovalId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Departments_DepartmentId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Items_ItemId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ApprovalId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_DepartmentId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ItemId",
                table: "Issues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Issue_AssetOrItem",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Issues");

            // NOTE (hand-edited): EF scaffolded these as RenameColumn + RenameIndex, which would have
            // carried old EmployeeId values into IssueId. They are unrelated columns, so the old one
            // is dropped and the new one added.
            migrationBuilder.DropIndex(
                name: "IX_AssetAssignments_EmployeeId",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AssetAssignments");

            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "AssetAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_IssueId",
                table: "AssetAssignments",
                column: "IssueId");

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "Issues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Issues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "Issues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustodianId",
                table: "AssetAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "AssetAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Issues_IssueId",
                table: "AssetAssignments",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetAssignments_Issues_IssueId",
                table: "AssetAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AssetAssignments_IssueId",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "AssetAssignments");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "AssetAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_EmployeeId",
                table: "AssetAssignments",
                column: "EmployeeId");

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "Issues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Issues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "Issues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Issues",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustodianId",
                table: "AssetAssignments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "AssetAssignments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ApprovalId",
                table: "Issues",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_DepartmentId",
                table: "Issues",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ItemId",
                table: "Issues",
                column: "ItemId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Issue_AssetOrItem",
                table: "Issues",
                sql: "(CASE WHEN [AssetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ItemId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Employees_EmployeeId",
                table: "AssetAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Approvals_ApprovalId",
                table: "Issues",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Departments_DepartmentId",
                table: "Issues",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Items_ItemId",
                table: "Issues",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
