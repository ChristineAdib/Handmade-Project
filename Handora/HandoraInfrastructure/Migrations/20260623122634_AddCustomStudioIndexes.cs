using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomStudioIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomRequests_CreatedAt",
                table: "CustomRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_CreatedAt",
                table: "CustomOffers",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomRequests_CreatedAt",
                table: "CustomRequests");

            migrationBuilder.DropIndex(
                name: "IX_CustomOffers_CreatedAt",
                table: "CustomOffers");
        }
    }
}
