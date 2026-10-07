using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Batch1Transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncidentId",
                table: "WorkOrders",
                type: "int",
                nullable: true);

            // NOTE (hand-edited): EF scaffolded this as AlterColumn nvarchar(max) -> int.
            // SQL Server refuses that conversion outright, so the old text column is dropped
            // and the enum column is added instead. No incident has ever been recorded
            // (there was no screen for it), so nothing is lost.
            migrationBuilder.DropColumn(
                name: "IncidentType",
                table: "Incidents");

            migrationBuilder.AddColumn<int>(
                name: "IncidentType",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastCustodianId",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStage",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IncidentId",
                table: "DisposalRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_IncidentId",
                table: "WorkOrders",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_LastCustodianId",
                table: "Incidents",
                column: "LastCustodianId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRequests_IncidentId",
                table: "DisposalRequests",
                column: "IncidentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisposalRequests_Incidents_IncidentId",
                table: "DisposalRequests",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Employees_LastCustodianId",
                table: "Incidents",
                column: "LastCustodianId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Incidents_IncidentId",
                table: "WorkOrders",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisposalRequests_Incidents_IncidentId",
                table: "DisposalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Employees_LastCustodianId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Incidents_IncidentId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_IncidentId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_LastCustodianId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_DisposalRequests_IncidentId",
                table: "DisposalRequests");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "LastCustodianId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "LastStage",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "DisposalRequests");

            // NOTE (hand-edited): mirror of the Up change.
            migrationBuilder.DropColumn(
                name: "IncidentType",
                table: "Incidents");

            migrationBuilder.AddColumn<string>(
                name: "IncidentType",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
