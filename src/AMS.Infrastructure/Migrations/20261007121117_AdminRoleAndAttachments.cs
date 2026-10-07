using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminRoleAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreventiveMaintenancePlanId",
                table: "WorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "Attachments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PreventiveMaintenancePlanId",
                table: "WorkOrders",
                column: "PreventiveMaintenancePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_PreventiveMaintenancePlans_PreventiveMaintenancePlanId",
                table: "WorkOrders",
                column: "PreventiveMaintenancePlanId",
                principalTable: "PreventiveMaintenancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // NOTE (hand-added): the role the code checks is now called Admin, not Approver.
            // The same person approves requests and posts depreciation, so the existing role
            // row is renamed and everybody who had it keeps it.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Approver')
   AND NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Admin')
BEGIN
    UPDATE [Roles] SET [Name] = N'Admin' WHERE [Name] = N'Approver';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_PreventiveMaintenancePlans_PreventiveMaintenancePlanId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_PreventiveMaintenancePlanId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PreventiveMaintenancePlanId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Attachments");

            // NOTE (hand-added): mirror of the Up change.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Admin')
   AND NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Approver')
BEGIN
    UPDATE [Roles] SET [Name] = N'Approver' WHERE [Name] = N'Admin';
END
");
        }
    }
}
