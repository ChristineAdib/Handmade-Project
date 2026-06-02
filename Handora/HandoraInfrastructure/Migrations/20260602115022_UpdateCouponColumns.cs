using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCouponColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_Shops_ShopId",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_ShopId_IsActive",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Coupons",
                newName: "DiscountValue");

            migrationBuilder.RenameColumn(
                name: "UsageCount",
                table: "Coupons",
                newName: "CurrentUsageCount");

            migrationBuilder.RenameColumn(
                name: "MinOrderAmount",
                table: "Coupons",
                newName: "MinOrderValue");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Coupons",
                newName: "ExpiryDate");

            migrationBuilder.RenameIndex(
                name: "IX_Coupons_ExpiresAt",
                table: "Coupons",
                newName: "IX_Coupons_ExpiryDate");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "Coupons",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_SellerId_IsActive",
                table: "Coupons",
                columns: new[] { "SellerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_ShopId",
                table: "Coupons",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_AspNetUsers_SellerId",
                table: "Coupons",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_Shops_ShopId",
                table: "Coupons",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_AspNetUsers_SellerId",
                table: "Coupons");

            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_Shops_ShopId",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_SellerId_IsActive",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_ShopId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "MinOrderValue",
                table: "Coupons",
                newName: "MinOrderAmount");

            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "Coupons",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "DiscountValue",
                table: "Coupons",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "CurrentUsageCount",
                table: "Coupons",
                newName: "UsageCount");

            migrationBuilder.RenameIndex(
                name: "IX_Coupons_ExpiryDate",
                table: "Coupons",
                newName: "IX_Coupons_ExpiresAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_ShopId_IsActive",
                table: "Coupons",
                columns: new[] { "ShopId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_Shops_ShopId",
                table: "Coupons",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
