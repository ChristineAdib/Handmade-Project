using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatUserModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 0, 15, 24, 949, DateTimeKind.Utc).AddTicks(981));

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 0, 15, 24, 949, DateTimeKind.Utc).AddTicks(988));

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 0, 15, 24, 949, DateTimeKind.Utc).AddTicks(990));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 21, 23, 50, 21, 180, DateTimeKind.Utc).AddTicks(6225));

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 21, 23, 50, 21, 180, DateTimeKind.Utc).AddTicks(6235));

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 21, 23, 50, 21, 180, DateTimeKind.Utc).AddTicks(6237));
        }
    }
}
