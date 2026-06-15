using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandoraInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanSelfReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Delete product self-reviews (where reviewer owns the shop of the product)
            migrationBuilder.Sql(@"
                DELETE r
                FROM Reviews r
                INNER JOIN Products p ON r.ProductId = p.Id
                INNER JOIN Shops s ON p.ShopId = s.Id
                WHERE r.UserId = s.OwnerId;
            ");

            // 2. Delete shop self-reviews (where reviewer owns the shop)
            migrationBuilder.Sql(@"
                DELETE sr
                FROM ShopReviews sr
                INNER JOIN Shops s ON sr.ShopId = s.Id
                WHERE sr.UserId = s.OwnerId;
            ");

            // 3. Recalculate average ratings and review counts for Products
            migrationBuilder.Sql(@"
                UPDATE p
                SET 
                    p.ReviewCount = ISNULL(r.Cnt, 0),
                    p.AverageRating = ISNULL(r.AvgRating, 0)
                FROM Products p
                LEFT JOIN (
                    SELECT ProductId, COUNT(*) as Cnt, AVG(CAST(Rating as decimal(18,2))) as AvgRating
                    FROM Reviews
                    WHERE IsDeleted = 0
                    GROUP BY ProductId
                ) r ON p.Id = r.ProductId;
            ");

            // 4. Recalculate average ratings and review counts for Shops
            migrationBuilder.Sql(@"
                UPDATE s
                SET 
                    s.ReviewCount = ISNULL(sr.Cnt, 0),
                    s.Rating = ISNULL(sr.AvgRating, 0)
                FROM Shops s
                LEFT JOIN (
                    SELECT ShopId, COUNT(*) as Cnt, AVG(CAST(Rating as decimal(18,2))) as AvgRating
                    FROM ShopReviews
                    WHERE IsDeleted = 0
                    GROUP BY ShopId
                ) sr ON s.Id = sr.ShopId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data cleanup deletion cannot be undone.
        }
    }
}
