using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneLoginPerEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");
        }
    }
}
