using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleOffersPerConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "ProjectWorkspaces",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomOfferId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DesignSummaryJson",
                table: "GeneratedDesigns",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "CustomOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerId",
                table: "CustomOffers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "CustomOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DesignId",
                table: "CustomOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "CustomOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "CustomOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "CustomOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "CustomOffers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "CustomOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomOfferId",
                table: "Orders",
                column: "CustomOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_ConversationId",
                table: "CustomOffers",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_DesignId",
                table: "CustomOffers",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_OrderId",
                table: "CustomOffers",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_WorkspaceId",
                table: "CustomOffers",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomOffers_Conversations_ConversationId",
                table: "CustomOffers",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomOffers_GeneratedDesigns_DesignId",
                table: "CustomOffers",
                column: "DesignId",
                principalTable: "GeneratedDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomOffers_Orders_OrderId",
                table: "CustomOffers",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomOffers_ProjectWorkspaces_WorkspaceId",
                table: "CustomOffers",
                column: "WorkspaceId",
                principalTable: "ProjectWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomOffers_CustomOfferId",
                table: "Orders",
                column: "CustomOfferId",
                principalTable: "CustomOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomOffers_Conversations_ConversationId",
                table: "CustomOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomOffers_GeneratedDesigns_DesignId",
                table: "CustomOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomOffers_Orders_OrderId",
                table: "CustomOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomOffers_ProjectWorkspaces_WorkspaceId",
                table: "CustomOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomOffers_CustomOfferId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomOfferId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CustomOffers_ConversationId",
                table: "CustomOffers");

            migrationBuilder.DropIndex(
                name: "IX_CustomOffers_DesignId",
                table: "CustomOffers");

            migrationBuilder.DropIndex(
                name: "IX_CustomOffers_OrderId",
                table: "CustomOffers");

            migrationBuilder.DropIndex(
                name: "IX_CustomOffers_WorkspaceId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "CustomOfferId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "DesignId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "CustomOffers");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "CustomOffers");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "ProjectWorkspaces",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "DesignSummaryJson",
                table: "GeneratedDesigns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
