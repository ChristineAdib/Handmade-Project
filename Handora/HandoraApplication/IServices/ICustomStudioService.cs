using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.CustomStudioDTOs;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.Helpers;
using HandoraDomain.Consts;

namespace HandoraApplication.IServices
{
    public interface ICustomStudioService
    {
        // Commands
        Task<Result<CustomRequestDetailDto>> CreateCustomRequestAsync(string buyerId, CreateCustomRequestCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> UpdateWizardStepAsync(string buyerId, UpdateWizardStepCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> SaveConfigurationAsync(string buyerId, SaveConfigurationCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> UploadReferenceImageMetadataAsync(string buyerId, UploadReferenceImageMetadataCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> AnalyzePhotoForDollAsync(string buyerId, Guid requestId, string base64Image, string mimeType, string fileUrl, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> SaveGeneratedDesignAsync(string buyerId, SaveGeneratedDesignCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> ChooseGeneratedDesignAsync(string buyerId, ChooseGeneratedDesignCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> SelectSellerAsync(string buyerId, SelectSellerCommand command, CancellationToken ct = default);
        Task<Result<ConversationDto>> InitializeNegotiationAsync(string buyerId, Guid requestId, Guid shopId, CancellationToken ct = default);
        Task<Result<CustomOfferDto>> CreateSellerOfferAsync(string sellerUserId, CreateSellerOfferCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> AcceptOfferAsync(string buyerId, AcceptOfferCommand command, CancellationToken ct = default);
        Task<Result<CustomOfferDto>> RejectOfferAsync(string buyerId, RejectOfferCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> CancelCustomRequestAsync(string buyerId, CancelCustomRequestCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> ArchiveRequestAsync(string buyerId, ArchiveRequestCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> GenerateDesignAsync(string buyerId, Guid requestId, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> RefineDesignAsync(string buyerId, RefineDesignCommand command, CancellationToken ct = default);
        Task<Result<CustomOfferDto>> RequestChangesAsync(string buyerId, Guid offerId, string feedback, CancellationToken ct = default);
        Task<Result<OrderResponseDto>> CheckoutCustomRequestAsync(string buyerId, CheckoutCustomRequestCommand command, CancellationToken ct = default);
        Task<Result<CustomServiceDto>> CreateCustomServiceAsync(string sellerUserId, CreateCustomServiceCommand command, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> ApproveCustomServiceAsync(string buyerUserId, Guid serviceId, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> RejectCustomServiceAsync(string buyerUserId, Guid serviceId, CancellationToken ct = default);

        // Workspace Commands
        Task<Result<CustomRequestDetailDto>> UpdateWorkspaceProgressAsync(string sellerUserId, Guid requestId, int milestoneStep, string? trackingNumber, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> UploadWorkspacePhotoAsync(string sellerUserId, Guid requestId, string photoUrl, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> ConfirmWorkspaceDeliveryAsync(string buyerUserId, Guid requestId, CancellationToken ct = default);


        // Queries
        Task<Result<WizardStep>> GetWizardProgressAsync(GetWizardProgressQuery query, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> GetCustomRequestDetailsAsync(GetCustomRequestDetailsQuery query, CancellationToken ct = default, string? userId = null, string? userRole = null);
        Task<Result<CustomConfigurationDto>> GetConfigurationAsync(GetConfigurationQuery query, CancellationToken ct = default);
        Task<Result<List<GeneratedDesignDto>>> GetGeneratedDesignsAsync(GetGeneratedDesignsQuery query, CancellationToken ct = default);
        Task<Result<GeneratedDesignDto>> GetSelectedDesignAsync(GetSelectedDesignQuery query, CancellationToken ct = default);
        Task<Result<List<SellerRecommendationDto>>> GetRecommendedSellersAsync(GetRecommendedSellersQuery query, CancellationToken ct = default);
        Task<Result<CustomOfferDto>> GetSellerOfferAsync(GetSellerOfferQuery query, CancellationToken ct = default);
        Task<Result<List<GeneratedDesignDto>>> GetDesignHistoryAsync(GetDesignHistoryQuery query, CancellationToken ct = default);
        Task<Result<PagedResultDto<CustomRequestSummaryDto>>> GetBuyerRequestsAsync(GetBuyerRequestsQuery query, CancellationToken ct = default);
        Task<Result<PagedResultDto<CustomRequestSummaryDto>>> GetSellerRequestsAsync(GetSellerRequestsQuery query, CancellationToken ct = default);
        Task<Result<PagedResultDto<CustomRequestSummaryDto>>> SearchCustomRequestsAsync(SearchCustomRequestsQuery query, CancellationToken ct = default);
        Task<Result<CustomRequestDetailDto>> GetCustomRequestByConversationIdAsync(Guid conversationId, CancellationToken ct = default);
        Task<Result<ProjectWorkspaceDto>> GetWorkspaceDetailsAsync(Guid requestId, string userId, string userRole, CancellationToken ct = default);
        Task<bool> IsAssignedSellerAsync(Guid requestId, string sellerUserId, CancellationToken ct = default);
    }
}
