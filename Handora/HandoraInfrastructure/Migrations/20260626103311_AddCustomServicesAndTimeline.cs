using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomServicesAndTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomServiceId",
                table: "ProjectWorkspaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "ProjectWorkspaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDeliveryDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedDesignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomServices_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomServices_AspNetUsers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomServices_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomServices_CustomRequests_CustomRequestId",
                        column: x => x.CustomRequestId,
                        principalTable: "CustomRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomServices_GeneratedDesigns_GeneratedDesignId",
                        column: x => x.GeneratedDesignId,
                        principalTable: "GeneratedDesigns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceTimelineEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ProjectWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceTimelineEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceTimelineEntries_ProjectWorkspaces_ProjectWorkspaceId",
                        column: x => x.ProjectWorkspaceId,
                        principalTable: "ProjectWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_CustomServiceId",
                table: "ProjectWorkspaces",
                column: "CustomServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_OrderId",
                table: "ProjectWorkspaces",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_BuyerId",
                table: "CustomServices",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_ConversationId",
                table: "CustomServices",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_CustomRequestId",
                table: "CustomServices",
                column: "CustomRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_GeneratedDesignId",
                table: "CustomServices",
                column: "GeneratedDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomServices_SellerId",
                table: "CustomServices",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTimelineEntries_ProjectWorkspaceId",
                table: "WorkspaceTimelineEntries",
                column: "ProjectWorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectWorkspaces_CustomServices_CustomServiceId",
                table: "ProjectWorkspaces",
                column: "CustomServiceId",
                principalTable: "CustomServices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectWorkspaces_Orders_OrderId",
                table: "ProjectWorkspaces",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectWorkspaces_CustomServices_CustomServiceId",
                table: "ProjectWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectWorkspaces_Orders_OrderId",
                table: "ProjectWorkspaces");

            migrationBuilder.DropTable(
                name: "CustomServices");

            migrationBuilder.DropTable(
                name: "WorkspaceTimelineEntries");

            migrationBuilder.DropIndex(
                name: "IX_ProjectWorkspaces_CustomServiceId",
                table: "ProjectWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_ProjectWorkspaces_OrderId",
                table: "ProjectWorkspaces");

            migrationBuilder.DropColumn(
                name: "CustomServiceId",
                table: "ProjectWorkspaces");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "ProjectWorkspaces");
        }
    }
}
