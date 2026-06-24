using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomStudioTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    ConfigurationDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeliveryTimeDays = table.Column<int>(type: "int", nullable: false),
                    RevisionsAllowed = table.Column<int>(type: "int", nullable: false),
                    AttachmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomOffers_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WizardStep = table.Column<int>(type: "int", nullable: false),
                    GenerationCount = table.Column<int>(type: "int", nullable: false),
                    TargetBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SelectedDesignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedSellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomRequests_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomRequests_Shops_SelectedSellerId",
                        column: x => x.SelectedSellerId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedDesigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenerationTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    MatchingScore = table.Column<double>(type: "float", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    IsSaved = table.Column<bool>(type: "bit", nullable: false),
                    IsDownloaded = table.Column<bool>(type: "bit", nullable: false),
                    PatternStepsMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedDesigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedDesigns_CustomRequests_CustomRequestId",
                        column: x => x.CustomRequestId,
                        principalTable: "CustomRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MilestoneStep = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    FinalPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_Conversations_ChatConversationId",
                        column: x => x.ChatConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_CustomOffers_SelectedOfferId",
                        column: x => x.SelectedOfferId,
                        principalTable: "CustomOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_CustomRequests_CustomRequestId",
                        column: x => x.CustomRequestId,
                        principalTable: "CustomRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellerRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchingScore = table.Column<double>(type: "float", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedDeliveryDays = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_CustomRequests_CustomRequestId",
                        column: x => x.CustomRequestId,
                        principalTable: "CustomRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomConfigurations_CustomRequestId",
                table: "CustomConfigurations",
                column: "CustomRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_CustomRequestId",
                table: "CustomOffers",
                column: "CustomRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomOffers_ShopId",
                table: "CustomOffers",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomRequests_BuyerId",
                table: "CustomRequests",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomRequests_SelectedDesignId",
                table: "CustomRequests",
                column: "SelectedDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomRequests_SelectedSellerId",
                table: "CustomRequests",
                column: "SelectedSellerId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedDesigns_CustomRequestId",
                table: "GeneratedDesigns",
                column: "CustomRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_ChatConversationId",
                table: "ProjectWorkspaces",
                column: "ChatConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_CustomRequestId",
                table: "ProjectWorkspaces",
                column: "CustomRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_SelectedOfferId",
                table: "ProjectWorkspaces",
                column: "SelectedOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_CustomRequestId",
                table: "SellerRecommendations",
                column: "CustomRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_ShopId",
                table: "SellerRecommendations",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomConfigurations_CustomRequests_CustomRequestId",
                table: "CustomConfigurations",
                column: "CustomRequestId",
                principalTable: "CustomRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomOffers_CustomRequests_CustomRequestId",
                table: "CustomOffers",
                column: "CustomRequestId",
                principalTable: "CustomRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomRequests_GeneratedDesigns_SelectedDesignId",
                table: "CustomRequests",
                column: "SelectedDesignId",
                principalTable: "GeneratedDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneratedDesigns_CustomRequests_CustomRequestId",
                table: "GeneratedDesigns");

            migrationBuilder.DropTable(
                name: "CustomConfigurations");

            migrationBuilder.DropTable(
                name: "ProjectWorkspaces");

            migrationBuilder.DropTable(
                name: "SellerRecommendations");

            migrationBuilder.DropTable(
                name: "CustomOffers");

            migrationBuilder.DropTable(
                name: "CustomRequests");

            migrationBuilder.DropTable(
                name: "GeneratedDesigns");
        }
    }
}
