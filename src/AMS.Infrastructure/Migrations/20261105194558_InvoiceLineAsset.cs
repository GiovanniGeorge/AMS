using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceLineAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "InvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_AssetId",
                table: "InvoiceLines",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Assets_AssetId",
                table: "InvoiceLines",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Assets_AssetId",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_AssetId",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "InvoiceLines");
        }
    }
}
