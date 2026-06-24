using System;
using System.Collections.Generic;
using HandoraDomain.Consts;

namespace HandoraApplication.DTOs.CustomStudioDTOs
{
    #region Commands

    public record CreateCustomRequestCommand(
        ProductType ProductType,
        decimal? TargetBudget,
        DateTime? DeadlineDate
    );

    public record UpdateWizardStepCommand(
        Guid RequestId,
        WizardStep WizardStep
    );

    public record SaveConfigurationCommand(
        Guid RequestId,
        ProductType ProductType,
        string ConfigurationDataJson
    );

    public record UploadReferenceImageMetadataCommand(
        Guid RequestId,
        string ReferenceImageUrl
    );

    public record SaveGeneratedDesignCommand(
        Guid RequestId,
        string ImageUrl,
        string Prompt,
        string Provider,
        long GenerationTimeMs,
        double MatchingScore,
        string PatternStepsMarkdown
    );

    public record ChooseGeneratedDesignCommand(
        Guid RequestId,
        Guid DesignId
    );

    public record SelectSellerCommand(
        Guid RequestId,
        Guid SellerShopId
    );

    public record CreateSellerOfferCommand(
        Guid RequestId,
        Guid ShopId,
        decimal Price,
        int DeliveryTimeDays,
        int RevisionsAllowed,
        List<string> Attachments,
        string Notes
    );

    public record AcceptOfferCommand(
        Guid RequestId,
        Guid OfferId
    );

    public record RejectOfferCommand(
        Guid RequestId,
        Guid OfferId
    );

    public record CancelCustomRequestCommand(
        Guid RequestId
    );

    public record ArchiveRequestCommand(
        Guid RequestId
    );

    public record CheckoutCustomRequestCommand(
        Guid RequestId,
        string FirstName,
        string LastName,
        string Street,
        string City,
        string Country,
        Guid DeliveryMethodId,
        string? CouponCode = null,
        string? Notes = null
    );

    #endregion

    #region Queries

    public record GetWizardProgressQuery(Guid RequestId);
    public record GetCustomRequestDetailsQuery(Guid RequestId);
    public record GetConfigurationQuery(Guid RequestId);
    public record GetGeneratedDesignsQuery(Guid RequestId);
    public record GetSelectedDesignQuery(Guid RequestId);
    public record GetRecommendedSellersQuery(Guid RequestId);
    public record GetSellerOfferQuery(Guid OfferId);
    public record GetDesignHistoryQuery(Guid RequestId);

    public record GetBuyerRequestsQuery(
        string BuyerId,
        int PageNumber = 1,
        int PageSize = 10
    );

    public record GetSellerRequestsQuery(
        Guid SellerShopId,
        int PageNumber = 1,
        int PageSize = 10
    );

    public record SearchCustomRequestsQuery(
        ProductType? ProductType = null,
        CustomRequestStatus? Status = null,
        string? SearchText = null,
        int PageNumber = 1,
        int PageSize = 10
    );

    #endregion
}
