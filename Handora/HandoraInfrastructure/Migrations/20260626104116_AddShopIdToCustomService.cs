using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopIdToCustomService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomRequestId1",
                table: "CustomServices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "CustomServices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_CustomRequestId1",
                table: "CustomServices",
                column: "CustomRequestId1",
                unique: true,
                filter: "[CustomRequestId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_ShopId",
                table: "CustomServices",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomServices_CustomRequests_CustomRequestId1",
                table: "CustomServices",
                column: "CustomRequestId1",
                principalTable: "CustomRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomServices_Shops_ShopId",
                table: "CustomServices",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomServices_CustomRequests_CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomServices_Shops_ShopId",
                table: "CustomServices");

            migrationBuilder.DropIndex(
                name: "IX_CustomServices_CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.DropIndex(
                name: "IX_CustomServices_ShopId",
                table: "CustomServices");

            migrationBuilder.DropColumn(
                name: "CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "CustomServices");
        }
    }
}
