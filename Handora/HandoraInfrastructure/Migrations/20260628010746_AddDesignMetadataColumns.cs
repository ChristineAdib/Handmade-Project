using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "GeneratedDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "GeneratedDesigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NegativePrompt",
                table: "GeneratedDesigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seed",
                table: "GeneratedDesigns",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "GeneratedDesigns");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "GeneratedDesigns");

            migrationBuilder.DropColumn(
                name: "NegativePrompt",
                table: "GeneratedDesigns");

            migrationBuilder.DropColumn(
                name: "Seed",
                table: "GeneratedDesigns");
        }
    }
}
