using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.Helpers;
using HandoraDomain.Models.CustomStudioEntities;

namespace HandoraApplication.IServices
{
    public interface IAdminCustomStudioService
    {
        // 1. Dashboard
        Task<Result<AdminCustomStudioDashboardDto>> GetDashboardMetricsAsync(CancellationToken ct = default);

        // 2. Custom Requests
        Task<Result<PagedList<AdminCustomRequestDto>>> GetRequestsAsync(
            string? search, string? buyerId, string? sellerId, int? status, 
            int? offerStatus, int? paymentStatus, string? productType, 
            DateTime? startDate, DateTime? endDate, string sortBy, int pageNumber, int pageSize, CancellationToken ct = default);
        
        Task<Result<CustomRequest>> GetRequestDetailsAsync(Guid requestId, CancellationToken ct = default);
        Task<Result> CancelRequestAsync(Guid requestId, string reason, CancellationToken ct = default);
        Task<Result> ArchiveRequestAsync(Guid requestId, CancellationToken ct = default);

        // 3. AI Generations
        Task<Result<PagedList<AdminAiGenerationDto>>> GetAiGenerationsAsync(
            string? provider, int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default);

        // 4. Artisan Analytics
        Task<Result<List<AdminArtisanDto>>> GetArtisanAnalyticsAsync(CancellationToken ct = default);

        // 5. Offers
        Task<Result<PagedList<AdminOfferDto>>> GetOffersAsync(
            int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<Result<AdminOfferMetricsDto>> GetOfferMetricsAsync(CancellationToken ct = default);

        // 6. Projects (Workspaces)
        Task<Result<PagedList<AdminProjectDto>>> GetProjectsAsync(
            int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default);

        // 7. Audit Log
        Task<Result<PagedList<CustomStudioAuditLog>>> GetAuditLogsAsync(
            string? eventName, string? search, int pageNumber, int pageSize, CancellationToken ct = default);
        Task LogActivityAsync(Guid? requestId, string eventName, string description, string? buyerId = null, string? sellerId = null, CancellationToken ct = default);

        // 8. Settings
        Task<Result<CustomStudioSetting>> GetSettingsAsync(CancellationToken ct = default);
        Task<Result<CustomStudioSetting>> UpdateSettingsAsync(CustomStudioSetting settings, CancellationToken ct = default);

        // 9. Exports
        Task<Result<string>> ExportRequestsToCsvAsync(CancellationToken ct = default);
        Task<Result<string>> ExportOffersToCsvAsync(CancellationToken ct = default);
        Task<Result<string>> ExportProjectsToCsvAsync(CancellationToken ct = default);
    }

    #region Service DTOs

    public class AdminCustomStudioDashboardDto
    {
        public int TotalRequests { get; set; }
        public int RequestsToday { get; set; }
        public int RequestsThisMonth { get; set; }
        public int CompletedProjects { get; set; }
        public int CancelledProjects { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }

        public double AvgCompletionTimeDays { get; set; }
        public decimal AvgOfferPrice { get; set; }
        public double AvgAiGenerationTimeSeconds { get; set; }
        public double ConversionRatePercent { get; set; }

        // Popular amigurumi design configurations
        public string MostPopularProductType { get; set; } = "CrochetDoll";
        public string MostPopularOutfit { get; set; } = "N/A";
        public string MostPopularHairStyle { get; set; } = "N/A";
        public string MostPopularAccessories { get; set; } = "N/A";
    }

    public class AdminCustomRequestDto
    {
        public Guid RequestId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int CurrentStep { get; set; }
        public string SelectedProduct { get; set; } = string.Empty;
        public string SelectedDesignImageUrl { get; set; } = string.Empty;
        public string OfferStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal? TargetBudget { get; set; }
        public bool IsArchived { get; set; }
    }

    public class AdminAiGenerationDto
    {
        public Guid DesignId { get; set; }
        public Guid RequestId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public double GenerationTimeSeconds { get; set; }
        public int PromptLength { get; set; }
        public string GenerationStatus { get; set; } = string.Empty;
        public double MatchingScore { get; set; }
        public int GenerationAttempts { get; set; }
        public string GeneratedImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminArtisanDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int RequestsMatchedCount { get; set; }
        public double OfferAcceptanceRate { get; set; }
        public double ProjectCompletionRate { get; set; }
        public decimal AverageRating { get; set; }
        public double AverageDeliveryTimeDays { get; set; }
        public decimal TotalCustomRevenue { get; set; }
        public int CompletedProjectsCount { get; set; }
    }

    public class AdminOfferDto
    {
        public Guid OfferId { get; set; }
        public Guid RequestId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DeliveryTimeDays { get; set; }
        public int RevisionsAllowed { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOfferMetricsDto
    {
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }
        public int ExpiredOffers { get; set; }
        public decimal AvgOfferPrice { get; set; }
        public double AvgNegotiationTimeHours { get; set; }
    }

    public class AdminProjectDto
    {
        public Guid WorkspaceId { get; set; }
        public Guid RequestId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Status { get; set; }
        public int MilestoneStep { get; set; }
        public string MilestoneName { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryDate { get; set; }
        public double CompletionPercentage { get; set; }
        public int ProgressPhotosCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PagedList<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedList() { }
        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    #endregion
}
