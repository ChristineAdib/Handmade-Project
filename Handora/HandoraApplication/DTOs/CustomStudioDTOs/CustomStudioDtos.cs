using System;
using System.Collections.Generic;

namespace HandoraApplication.DTOs.CustomStudioDTOs
{
    public class CustomRequestDetailDto
    {
        public Guid Id { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WizardStep { get; set; } = string.Empty;
        public int GenerationCount { get; set; }
        public decimal? TargetBudget { get; set; }
        public DateTime? DeadlineDate { get; set; }

        public Guid? SelectedDesignId { get; set; }
        public Guid? SelectedSellerId { get; set; }
        public string? SelectedSellerName { get; set; }

        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public CustomConfigurationDto? CustomConfiguration { get; set; }
        public List<GeneratedDesignDto> GeneratedDesigns { get; set; } = new();
        public List<SellerRecommendationDto> SellerRecommendations { get; set; } = new();
        public List<CustomOfferDto> CustomOffers { get; set; } = new();
        public ProjectWorkspaceDto? ProjectWorkspace { get; set; }
    }

    public class CustomRequestSummaryDto
    {
        public Guid Id { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int GenerationCount { get; set; }
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal? TargetBudget { get; set; }
        public DateTime? DeadlineDate { get; set; }
    }

    public class CustomConfigurationDto
    {
        public Guid Id { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public string ConfigurationDataJson { get; set; } = string.Empty;
    }

    public class GeneratedDesignDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public long GenerationTimeMs { get; set; }
        public double MatchingScore { get; set; }
        public bool IsSelected { get; set; }
        public bool IsSaved { get; set; }
        public bool IsDownloaded { get; set; }
        public string PatternStepsMarkdown { get; set; } = string.Empty;
    }

    public class SellerRecommendationDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? ShopLogo { get; set; }
        public double MatchingScore { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal EstimatedPrice { get; set; }
        public int EstimatedDeliveryDays { get; set; }
    }

    public class CustomOfferDto
    {
        public Guid Id { get; set; }
        public Guid CustomRequestId { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? ShopLogo { get; set; }
        public decimal Price { get; set; }
        public int DeliveryTimeDays { get; set; }
        public int RevisionsAllowed { get; set; }
        public List<string> Attachments { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ProjectWorkspaceDto
    {
        public Guid Id { get; set; }
        public Guid CustomRequestId { get; set; }
        public Guid SelectedOfferId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MilestoneStep { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? FinalPhotoUrl { get; set; }
        public string? TrackingNumber { get; set; }
        public Guid? ChatConversationId { get; set; }
    }
}
