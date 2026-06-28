using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomServiceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomServices_CustomRequests_CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.DropIndex(
                name: "IX_CustomServices_CustomRequestId",
                table: "CustomServices");

            migrationBuilder.DropIndex(
                name: "IX_CustomServices_CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.DropColumn(
                name: "CustomRequestId1",
                table: "CustomServices");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_CustomRequestId",
                table: "CustomServices",
                column: "CustomRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomServices_CustomRequestId",
                table: "CustomServices");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomRequestId1",
                table: "CustomServices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_CustomRequestId",
                table: "CustomServices",
                column: "CustomRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_CustomRequestId1",
                table: "CustomServices",
                column: "CustomRequestId1",
                unique: true,
                filter: "[CustomRequestId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomServices_CustomRequests_CustomRequestId1",
                table: "CustomServices",
                column: "CustomRequestId1",
                principalTable: "CustomRequests",
                principalColumn: "Id");
        }
    }
}
