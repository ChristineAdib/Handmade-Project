using HandoraDomain.Models.ProductEntities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface IAiReviewService
    {
        Task<ReviewSummaryResult> GenerateSummaryAsync(
            string? existingSummary,
            string? existingPros,
            string? existingCons,
            List<string> newReviews);
    }
}
