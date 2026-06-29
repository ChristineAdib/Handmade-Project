using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationActiveDesignRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "CustomRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveDesignRequestId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomRequests_ConversationId",
                table: "CustomRequests",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ActiveDesignRequestId",
                table: "Conversations",
                column: "ActiveDesignRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_CustomRequests_ActiveDesignRequestId",
                table: "Conversations",
                column: "ActiveDesignRequestId",
                principalTable: "CustomRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomRequests_Conversations_ConversationId",
                table: "CustomRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_CustomRequests_ActiveDesignRequestId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomRequests_Conversations_ConversationId",
                table: "CustomRequests");

            migrationBuilder.DropIndex(
                name: "IX_CustomRequests_ConversationId",
                table: "CustomRequests");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ActiveDesignRequestId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "CustomRequests");

            migrationBuilder.DropColumn(
                name: "ActiveDesignRequestId",
                table: "Conversations");
        }
    }
}
