using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DeliveryMethods");

            migrationBuilder.RenameColumn(
                name: "DeliveryTime",
                table: "DeliveryMethods",
                newName: "DeliveryTimeEn");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "DeliveryMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "DeliveryMethods",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTimeAr",
                table: "DeliveryMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "DeliveryMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "DeliveryMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_IsActive",
                table: "DeliveryMethods",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryMethods_IsActive",
                table: "DeliveryMethods");

            migrationBuilder.DropColumn(
                name: "DeliveryTimeAr",
                table: "DeliveryMethods");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "DeliveryMethods");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "DeliveryMethods");

            migrationBuilder.RenameColumn(
                name: "DeliveryTimeEn",
                table: "DeliveryMethods",
                newName: "DeliveryTime");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "DeliveryMethods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "DeliveryMethods",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DeliveryMethods",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "DeliveryMethods",
                columns: new[] { "Id", "Cost", "CreatedAt", "CreatedBy", "DeletedAt", "DeliveryTime", "Description", "IsActive", "IsDeleted", "ShortName", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, 15.00m, new DateTime(2026, 5, 22, 0, 17, 55, 497, DateTimeKind.Utc).AddTicks(1395), null, null, "5-7 Days", "Standard Delivery", true, false, "Standard", null, null },
                    { 2, 35.00m, new DateTime(2026, 5, 22, 0, 17, 55, 497, DateTimeKind.Utc).AddTicks(1401), null, null, "2-3 Days", "Express Delivery", true, false, "Express", null, null },
                    { 3, 60.00m, new DateTime(2026, 5, 22, 0, 17, 55, 497, DateTimeKind.Utc).AddTicks(1403), null, null, "1 Day", "Next Day Delivery", true, false, "Next Day", null, null }
                });
        }
    }
}
