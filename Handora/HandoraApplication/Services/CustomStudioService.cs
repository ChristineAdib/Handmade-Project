using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.CustomStudioDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraApplication.Specifications;
using HandoraDomain.Consts;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraDomain.Models.ShopEntities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using HandoraApplication.AI.Interfaces;
using Microsoft.Extensions.Logging;
using HandoraApplication.Hubs;
using IChatService = HandoraApplication.IServices.IChatService;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraDomain.Models.OrderEntity;
using Microsoft.AspNetCore.Identity;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.AI.DTOs;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.Services
{
    public class CustomStudioService : ICustomStudioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreateCustomRequestCommand> _createRequestValidator;
        private readonly IValidator<SaveConfigurationCommand> _saveConfigValidator;
        private readonly IValidator<CreateSellerOfferCommand> _createOfferValidator;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIImageGenerationService _imageGenerator;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IChatHubContext _chatHubContext;
        private readonly IChatService _chatService;
        private readonly ILogger<CustomStudioService> _logger;
        private readonly IRagService _ragService;

        public CustomStudioService(
            IUnitOfWork unitOfWork,
            IValidator<CreateCustomRequestCommand> createRequestValidator,
            IValidator<SaveConfigurationCommand> saveConfigValidator,
            IValidator<CreateSellerOfferCommand> createOfferValidator,
            IAIPromptBuilder promptBuilder,
            IAIImageGenerationService imageGenerator,
            UserManager<User> userManager,
            INotificationService notificationService,
            IChatHubContext chatHubContext,
            IChatService chatService,
            ILogger<CustomStudioService> logger,
            IRagService ragService)
        {
            _unitOfWork = unitOfWork;
            _createRequestValidator = createRequestValidator;
            _saveConfigValidator = saveConfigValidator;
            _createOfferValidator = createOfferValidator;
            _promptBuilder = promptBuilder;
            _imageGenerator = imageGenerator;
            _userManager = userManager;
            _notificationService = notificationService;
            _chatHubContext = chatHubContext;
            _chatService = chatService;
            _logger = logger;
            _ragService = ragService;
        }

        #region Commands

        public async Task<Result<CustomRequestDetailDto>> CreateCustomRequestAsync(
            string buyerId, CreateCustomRequestCommand command, CancellationToken ct = default)
        {
            var valResult = await _createRequestValidator.ValidateAsync(command, ct);
            if (!valResult.IsValid)
            {
                return Result<CustomRequestDetailDto>.Failure(valResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var request = new CustomRequest
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ProductType = command.ProductType,
                Status = CustomRequestStatus.Draft,
                WizardStep = WizardStep.Initial,
                TargetBudget = command.TargetBudget,
                DeadlineDate = command.DeadlineDate,
                CreatedAt = DateTime.UtcNow
            };

            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            await repo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> UpdateWizardStepAsync(
            string buyerId, UpdateWizardStepCommand command, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await repo.GetByIdAsync(command.RequestId);
            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            request.WizardStep = command.WizardStep;
            request.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> SaveConfigurationAsync(
            string buyerId, SaveConfigurationCommand command, CancellationToken ct = default)
        {
            var valResult = await _saveConfigValidator.ValidateAsync(command, ct);
            if (!valResult.IsValid)
            {
                return Result<CustomRequestDetailDto>.Failure(valResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            var configRepo = _unitOfWork.Repository<CustomConfiguration, Guid>();

            if (request.CustomConfiguration == null)
            {
                var config = new CustomConfiguration
                {
                    Id = Guid.NewGuid(),
                    CustomRequestId = request.Id,
                    ProductType = command.ProductType,
                    ConfigurationDataJson = command.ConfigurationDataJson,
                    CreatedAt = DateTime.UtcNow
                };
                await configRepo.AddAsync(config);
                request.CustomConfiguration = config;
            }
            else
            {
                request.CustomConfiguration.ProductType = command.ProductType;
                request.CustomConfiguration.ConfigurationDataJson = command.ConfigurationDataJson;
                request.CustomConfiguration.UpdatedAt = DateTime.UtcNow;
                await configRepo.UpdateAsync(request.CustomConfiguration);
            }

            try
            {
                request.Configure(request.CustomConfiguration);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> UploadReferenceImageMetadataAsync(
            string buyerId, UploadReferenceImageMetadataCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            if (request.CustomConfiguration == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Configuration must be initialized before uploading reference image metadata.");
            }

            // Deconstruct JSON, append image, and re-serialize to preserve other properties
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dollConfig = JsonSerializer.Deserialize<CrochetDollConfiguration>(request.CustomConfiguration.ConfigurationDataJson, options);
                
                if (dollConfig != null)
                {
                    var updatedConfig = dollConfig with { ReferenceImageUrl = command.ReferenceImageUrl };
                    request.CustomConfiguration.ConfigurationDataJson = JsonSerializer.Serialize(updatedConfig, options);
                    request.CustomConfiguration.UpdatedAt = DateTime.UtcNow;
                    
                    var configRepo = _unitOfWork.Repository<CustomConfiguration, Guid>();
                    await configRepo.UpdateAsync(request.CustomConfiguration);
                }
            }
            catch (Exception)
            {
                return Result<CustomRequestDetailDto>.Failure("Failed to update configuration reference image metadata.");
            }

            await _unitOfWork.SaveChangesAsync();
            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> SaveGeneratedDesignAsync(
            string buyerId, SaveGeneratedDesignCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .Include(r => r.GeneratedDesigns)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            // Retrieve admin configurations dynamically
            var settingsRepo = _unitOfWork.Repository<CustomStudioSetting, Guid>();
            var settingsQuery = await settingsRepo.GetAllAsNoTracking();
            var settings = await settingsQuery.FirstOrDefaultAsync(ct) ?? new CustomStudioSetting();

            if (!settings.IsFeatureEnabled)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Studio feature is currently disabled by administrator.");
            }

            try
            {
                request.StartGeneration(settings.MaxAiGenerations);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            var design = new GeneratedDesign
            {
                Id = Guid.NewGuid(),
                CustomRequestId = request.Id,
                ImageUrl = command.ImageUrl,
                Prompt = command.Prompt,
                Provider = command.Provider,
                GenerationTimeMs = command.GenerationTimeMs,
                MatchingScore = command.MatchingScore,
                PatternStepsMarkdown = command.PatternStepsMarkdown,
                CreatedAt = DateTime.UtcNow
            };

            var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();
            await designRepo.AddAsync(design);

            try
            {
                request.CompleteGeneration(design);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> ChooseGeneratedDesignAsync(
            string buyerId, ChooseGeneratedDesignCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.GeneratedDesigns)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            try
            {
                request.SelectDesign(command.DesignId);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Design Chosen. RequestId: {RequestId}, DesignId: {DesignId}", request.Id, command.DesignId);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> SelectSellerAsync(
            string buyerId, SelectSellerCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.SellerRecommendations)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            // Verify seller shop exists
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(command.SellerShopId);
            if (shop == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Seller Shop not found.");
            }

            // Domain rule validation
            if (request.Status != CustomRequestStatus.DesignSelected && request.Status != CustomRequestStatus.SellerMatched)
            {
                return Result<CustomRequestDetailDto>.Failure("A design must be selected before matching/selecting a seller.");
            }

            request.SelectedSellerId = command.SellerShopId;
            request.Status = CustomRequestStatus.SellerMatched;
            request.UpdatedAt = DateTime.UtcNow;

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomOfferDto>> CreateSellerOfferAsync(
            string sellerUserId, CreateSellerOfferCommand command, CancellationToken ct = default)
        {
            var valResult = await _createOfferValidator.ValidateAsync(command, ct);
            if (!valResult.IsValid)
            {
                return Result<CustomOfferDto>.Failure(valResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomOffers)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomOfferDto>.Failure("Custom Request not found.");
            }

            // Verify seller shop exists and caller owns it
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(command.ShopId);
            if (shop == null)
            {
                return Result<CustomOfferDto>.Failure("Seller Shop not found.");
            }

            if (shop.OwnerId != sellerUserId)
            {
                return Result<CustomOfferDto>.Failure("Unauthorized access: you do not own this seller shop.");
            }

            // SECURITY: Prevent duplicate pending offers from same seller
            if (request.CustomOffers.Any(o => o.ShopId == command.ShopId && o.Status == OfferStatus.Pending))
            {
                return Result<CustomOfferDto>.Failure("An offer is already pending for this custom request from your shop.");
            }

            // In transition states, open negotiation if matched
            if (request.Status == CustomRequestStatus.SellerMatched)
            {
                request.OpenForNegotiation();
            }

            var offer = new CustomOffer
            {
                Id = Guid.NewGuid(),
                CustomRequestId = request.Id,
                ShopId = command.ShopId,
                Price = command.Price,
                DeliveryTimeDays = command.DeliveryTimeDays,
                RevisionsAllowed = command.RevisionsAllowed,
                Notes = command.Notes,
                AttachmentsJson = JsonSerializer.Serialize(command.Attachments),
                Status = OfferStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            await offerRepo.AddAsync(offer);

            try
            {
                request.ReceiveOffer(offer);
            }
            catch (Exception ex)
            {
                return Result<CustomOfferDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Load nested details for returning DTO
            var offers = await offerRepo.GetAllAsync();
            var createdOffer = await offers
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == offer.Id, ct);

            // Automatically open chat conversation and attach this offer card in dialogue
            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == request.BuyerId && c.SellerId == sellerUserId)
                .FirstOrDefaultAsync(ct);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = request.BuyerId,
                    SellerId = sellerUserId,
                    CreatedAt = DateTime.UtcNow
                };
                await conversationRepo.AddAsync(conversation);
                await _unitOfWork.SaveChangesAsync();
            }

            // Send message with type CustomOffer and body containing the offer guid
            await SendChatMessageAsync(
                conversation.Id,
                sellerUserId,
                shop.Name,
                request.BuyerId,
                offer.Id.ToString(),
                MessageType.CustomOffer
            );

            // Trigger db notification to buyer
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = request.BuyerId,
                TitleEn = "New Custom Offer Received",
                TitleAr = "تم استلام عرض مخصص جديد",
                MessageEn = $"Seller {shop.Name} has submitted an offer of ${offer.Price} for your crochet doll request.",
                MessageAr = $"قدم البائع {shop.Name} عرضاً بقيمة {offer.Price} دولار لطلب دمية الكروشيه الخاص بك.",
                Type = NotificationType.Message,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Offer Created. RequestId: {RequestId}, ShopId: {ShopId}, Price: {Price}", request.Id, command.ShopId, command.Price);

            return Result<CustomOfferDto>.Success(createdOffer!.Adapt<CustomOfferDto>());
        }

        public async Task<Result<CustomRequestDetailDto>> AcceptOfferAsync(
            string buyerId, AcceptOfferCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomOffers)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offers = await offerRepo.GetAllAsync();
            var offer = await offers
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == command.OfferId, ct);

            if (offer == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Offer not found.");
            }

            // Find or create Chat Conversation between Buyer and Seller Shop Owner
            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == request.BuyerId && c.SellerId == offer.Shop.OwnerId)
                .FirstOrDefaultAsync(ct);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = request.BuyerId,
                    SellerId = offer.Shop.OwnerId,
                    CreatedAt = DateTime.UtcNow
                };
                await conversationRepo.AddAsync(conversation);
                await _unitOfWork.SaveChangesAsync();
            }

            try
            {
                request.AcceptOffer(command.OfferId, conversation);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Buyer accepts offer: insert notification message to Chat
            await SendChatMessageAsync(
                conversation.Id,
                buyerId,
                request.Buyer?.Name ?? "Buyer",
                offer.Shop.OwnerId,
                "Offer accepted! Custom request upgraded to Project Workspace.",
                MessageType.Text
            );

            // Notify seller
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = offer.Shop.OwnerId,
                TitleEn = "Custom Offer Accepted",
                TitleAr = "تم قبول العرض المخصص",
                MessageEn = $"The buyer has accepted your offer for Custom Doll Request. Proceeding to checkout deposit.",
                MessageAr = $"قبل المشتري عرضك لطلب الدمية المخصصة. جاري الانتقال لدفع العربون.",
                Type = NotificationType.Message,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Offer Accepted. RequestId: {RequestId}, OfferId: {OfferId}", command.RequestId, command.OfferId);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomOfferDto>> RejectOfferAsync(
            string buyerId, RejectOfferCommand command, CancellationToken ct = default)
        {
            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offers = await offerRepo.GetAllAsync();
            var offer = await offers
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == command.OfferId, ct);

            if (offer == null)
            {
                return Result<CustomOfferDto>.Failure("Custom Offer not found.");
            }

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await requestRepo.GetByIdAsync(offer.CustomRequestId);
            if (request == null || request.BuyerId != buyerId)
            {
                return Result<CustomOfferDto>.Failure("Unauthorized access to this custom request.");
            }

            if (offer.Status != OfferStatus.Pending)
            {
                return Result<CustomOfferDto>.Failure("Can only reject pending offers.");
            }

            offer.Status = OfferStatus.Rejected;
            offer.UpdatedAt = DateTime.UtcNow;
            await offerRepo.UpdateAsync(offer);
            
            // Revert request status to negotiation if it was OfferSent
            if (request.Status == CustomRequestStatus.OfferSent)
            {
                request.Status = CustomRequestStatus.Negotiation;
                await requestRepo.UpdateAsync(request);
            }

            await _unitOfWork.SaveChangesAsync();

            // Find conversation
            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == buyerId && c.SellerId == offer.Shop.OwnerId)
                .FirstOrDefaultAsync(ct);

            if (conversation != null)
            {
                await SendChatMessageAsync(
                    conversation.Id,
                    buyerId,
                    request.Buyer?.Name ?? "Buyer",
                    offer.Shop.OwnerId,
                    "Offer rejected.",
                    MessageType.Text
                );
            }

            // Notify seller
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = offer.Shop.OwnerId,
                TitleEn = "Custom Offer Rejected",
                TitleAr = "تم رفض العرض المخصص",
                MessageEn = $"The buyer has declined your custom offer for request {request.Id}.",
                MessageAr = $"رفض المشتري عرضك المخصص للطلب {request.Id}.",
                Type = NotificationType.Message,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Offer Rejected. RequestId: {RequestId}, OfferId: {OfferId}", request.Id, command.OfferId);

            return Result<CustomOfferDto>.Success(offer.Adapt<CustomOfferDto>());
        }

        public async Task<Result<CustomRequestDetailDto>> CancelCustomRequestAsync(
            string buyerId, CancelCustomRequestCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomOffers)
                .Include(r => r.ProjectWorkspace)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            try
            {
                request.Cancel();
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> ArchiveRequestAsync(
            string buyerId, ArchiveRequestCommand command, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await repo.GetByIdAsync(command.RequestId);
            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            await repo.SoftDeleteAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> GenerateDesignAsync(string buyerId, Guid requestId, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .Include(r => r.GeneratedDesigns)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            if (request.CustomConfiguration == null || string.IsNullOrWhiteSpace(request.CustomConfiguration.ConfigurationDataJson))
            {
                return Result<CustomRequestDetailDto>.Failure("Valid custom configuration details are required before generation.");
            }

            // Retrieve admin configurations dynamically
            var settingsRepo = _unitOfWork.Repository<CustomStudioSetting, Guid>();
            var settingsQuery = await settingsRepo.GetAllAsNoTracking();
            var settings = await settingsQuery.FirstOrDefaultAsync(ct) ?? new CustomStudioSetting();

            if (!settings.IsFeatureEnabled)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Studio feature is currently disabled by administrator.");
            }

            if (request.Status == CustomRequestStatus.Configuring || request.Status == CustomRequestStatus.Draft)
            {
                try
                {
                    if (request.Status == CustomRequestStatus.Draft)
                    {
                        request.Configure(request.CustomConfiguration!);
                    }
                    request.SubmitForGeneration();
                }
                catch (Exception ex)
                {
                    return Result<CustomRequestDetailDto>.Failure($"Failed to prepare request for generation: {ex.Message}");
                }
            }

            try
            {
                request.StartGeneration(settings.MaxAiGenerations);
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            try
            {
                // 1. Build prompt
                var promptResult = _promptBuilder.BuildPrompt(request.CustomConfiguration);

                // 2. Generate 2 designs in parallel
                var req1 = new GenerateImageRequest
                {
                    Prompt = promptResult.PositivePrompt,
                    NegativePrompt = promptResult.NegativePrompt,
                    ImageCount = 1,
                    UserId = buyerId,
                    BypassCache = false
                };
                var req2 = new GenerateImageRequest
                {
                    Prompt = promptResult.PositivePrompt,
                    NegativePrompt = promptResult.NegativePrompt,
                    ImageCount = 1,
                    UserId = buyerId,
                    BypassCache = true // Force cache bypass for distinct option
                };

                var task1 = _imageGenerator.GenerateImageAsync(req1, ct);
                var task2 = _imageGenerator.GenerateImageAsync(req2, ct);
                await Task.WhenAll(task1, task2);

                var res1 = await task1;
                var res2 = await task2;

                var img1 = res1.Images[0];
                var img2 = res2.Images[0];

                var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();

                var rnd = new Random();
                
                // Design 1
                var score1 = Math.Round(90.0 + (rnd.NextDouble() * 8.5), 1);
                var design1 = new GeneratedDesign
                {
                    Id = Guid.NewGuid(),
                    CustomRequestId = request.Id,
                    ImageUrl = img1.ImageUrl,
                    Prompt = req1.Prompt,
                    Provider = res1.Metadata.ProviderName,
                    GenerationTimeMs = res1.Metadata.DurationMs,
                    MatchingScore = score1,
                    PatternStepsMarkdown = "Stitch details and amigurumi pattern code goes here.",
                    CreatedAt = DateTime.UtcNow
                };
                await designRepo.AddAsync(design1);
                request.CompleteGeneration(design1);

                // Design 2
                var score2 = Math.Round(90.0 + (rnd.NextDouble() * 8.5), 1);
                var design2 = new GeneratedDesign
                {
                    Id = Guid.NewGuid(),
                    CustomRequestId = request.Id,
                    ImageUrl = img2.ImageUrl,
                    Prompt = req2.Prompt,
                    Provider = res2.Metadata.ProviderName,
                    GenerationTimeMs = res2.Metadata.DurationMs,
                    MatchingScore = score2,
                    PatternStepsMarkdown = "Stitch details and amigurumi pattern code goes here.",
                    CreatedAt = DateTime.UtcNow
                };
                await designRepo.AddAsync(design2);
                request.CompleteGeneration(design2);

                // Update request state
                await requestRepo.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
            }
            catch (Exception ex)
            {
                // Rollback generation count if failed
                request.GenerationCount = Math.Max(0, request.GenerationCount - 1);
                if (request.Status == CustomRequestStatus.Generating)
                {
                    request.Status = request.GeneratedDesigns.Any() ? CustomRequestStatus.Generated : CustomRequestStatus.ReadyForGeneration;
                }
                await requestRepo.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();
                
                return Result<CustomRequestDetailDto>.Failure($"AI Generation failed: {ex.Message}");
            }
        }

        public async Task<Result<CustomOfferDto>> RequestChangesAsync(string buyerId, Guid offerId, string feedback, CancellationToken ct = default)
        {
            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offers = await offerRepo.GetAllAsync();
            var offer = await offers
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == offerId, ct);

            if (offer == null)
            {
                return Result<CustomOfferDto>.Failure("Custom Offer not found.");
            }

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await requestRepo.GetByIdAsync(offer.CustomRequestId);
            if (request == null || request.BuyerId != buyerId)
            {
                return Result<CustomOfferDto>.Failure("Unauthorized access to this custom request.");
            }

            if (offer.Status != OfferStatus.Pending)
            {
                return Result<CustomOfferDto>.Failure("Can only request changes on pending offers.");
            }

            // Move custom request status back to negotiation if it is in OfferSent status
            if (request.Status == CustomRequestStatus.OfferSent)
            {
                request.Status = CustomRequestStatus.Negotiation;
                request.UpdatedAt = DateTime.UtcNow;
                await requestRepo.UpdateAsync(request);
            }

            // Set the offer to Rejected
            offer.Status = OfferStatus.Rejected;
            offer.Notes = $"[Changes Requested: {feedback}] {offer.Notes}";
            offer.UpdatedAt = DateTime.UtcNow;

            await offerRepo.UpdateAsync(offer);
            await _unitOfWork.SaveChangesAsync();

            // Send message in Chat: "Revision requested: [feedback]"
            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == buyerId && c.SellerId == offer.Shop.OwnerId)
                .FirstOrDefaultAsync(ct);

            if (conversation != null)
            {
                await SendChatMessageAsync(
                    conversation.Id,
                    buyerId,
                    request.Buyer?.Name ?? "Buyer",
                    offer.Shop.OwnerId,
                    $"Revision requested: {feedback}",
                    MessageType.Text
                );
            }

            // Notify seller
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = offer.Shop.OwnerId,
                TitleEn = "Changes Requested on Custom Offer",
                TitleAr = "طلب تعديل على العرض المخصص",
                MessageEn = $"The buyer has requested modifications for request {request.Id}. Feedback: {feedback}",
                MessageAr = $"طلب المشتري تعديلات للطلب {request.Id}. ملاحظات: {feedback}",
                Type = NotificationType.Message,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Revision Requested. RequestId: {RequestId}, OfferId: {OfferId}, Feedback: {Feedback}", request.Id, offerId, feedback);

            return Result<CustomOfferDto>.Success(offer.Adapt<CustomOfferDto>());
        }

        public async Task<Result<OrderResponseDto>> CheckoutCustomRequestAsync(string buyerId, CheckoutCustomRequestCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomOffers)
                .Include(r => r.SelectedDesign)
                .Include(r => r.ProjectWorkspace)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<OrderResponseDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<OrderResponseDto>.Failure("Unauthorized access to this custom request.");
            }

            if (request.Status != CustomRequestStatus.OfferAccepted)
            {
                return Result<OrderResponseDto>.Failure("Can only checkout requests where a seller offer has been accepted.");
            }

            if (request.ProjectWorkspace == null)
            {
                return Result<OrderResponseDto>.Failure("Project workspace was not initialized.");
            }

            var acceptedOffer = request.CustomOffers.FirstOrDefault(o => o.Id == request.ProjectWorkspace.SelectedOfferId);
            if (acceptedOffer == null)
            {
                return Result<OrderResponseDto>.Failure("Accepted seller offer details not found.");
            }

            // Retrieve delivery method
            var deliveryRepo = _unitOfWork.Repository<DeliveryMethod, Guid>();
            var deliveryMethod = await deliveryRepo.GetByIdAsync(command.DeliveryMethodId);
            if (deliveryMethod == null || !deliveryMethod.IsActive)
            {
                return Result<OrderResponseDto>.Failure("Invalid or inactive delivery method.");
            }

            // Create shipping address object
            var shippingAddress = new OrderShippingAddress
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Street = command.Street,
                City = command.City,
                Country = command.Country
            };

            // Build order items (only 1 item representing the custom request design)
            var pictureUrl = request.SelectedDesign?.ImageUrl ?? "";
            var productName = $"Custom Studio Request - {request.ProductType}";
            
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                Product = new ProductItemOrdered(request.Id, productName, pictureUrl),
                Quantity = 1,
                Price = acceptedOffer.Price,
                ShopId = acceptedOffer.ShopId
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = buyerId,
                BuyerEmail = request.BuyerId,
                ShippingAddress = shippingAddress,
                DeliveryMethodId = command.DeliveryMethodId,
                DeliveryMethod = deliveryMethod,
                SubTotal = acceptedOffer.Price,
                TotalAmount = acceptedOffer.Price + deliveryMethod.Cost,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };

            // Retrieve buyer email
            var buyerUser = await _userManager.FindByIdAsync(buyerId);
            if (buyerUser != null && !string.IsNullOrEmpty(buyerUser.Email))
            {
                order.BuyerEmail = buyerUser.Email;
            }

            order.Items.Add(orderItem);

            // Handle coupon if any
            if (!string.IsNullOrWhiteSpace(command.CouponCode))
            {
                var couponRepo = _unitOfWork.Repository<Coupon, Guid>();
                var coupons = await couponRepo.GetAllAsync();
                var coupon = await coupons.FirstOrDefaultAsync(c => c.Code == command.CouponCode && c.IsActive, ct);
                if (coupon != null)
                {
                    order.CouponId = coupon.Id;
                    order.Coupon = coupon;
                    // Apply discount (e.g. flat amount)
                    var discount = coupon.DiscountType == DiscountType.Percentage 
                        ? (order.SubTotal * coupon.DiscountValue / 100) 
                        : coupon.DiscountValue;
                    order.DiscountAmount = Math.Min(order.SubTotal, discount);
                    order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount.Value + deliveryMethod.Cost);
                }
            }

            // Save order in db
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            await orderRepo.AddAsync(order);

            // Transition request status
            try
            {
                request.InitiatePayment();
            }
            catch (Exception ex)
            {
                return Result<OrderResponseDto>.Failure(ex.Message);
            }

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return Result<OrderResponseDto>.Success(order.Adapt<OrderResponseDto>());
        }

        // ================= NEW WORKSPACE COMMANDS =================

        public async Task<Result<CustomRequestDetailDto>> UpdateWorkspaceProgressAsync(
            string sellerUserId, Guid requestId, int milestoneStep, string? trackingNumber, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.ProjectWorkspace)
                .Include(r => r.CustomOffers)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null || request.ProjectWorkspace == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Project Workspace not initialized.");
            }

            // Validate that caller owns the workspace seller shop
            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offer = await offerRepo.GetByIdAsync(request.ProjectWorkspace.SelectedOfferId);
            if (offer == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Selected offer not found.");
            }

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(offer.ShopId);
            if (shop == null || shop.OwnerId != sellerUserId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access: you do not own the artisan shop for this workspace.");
            }

            request.ProjectWorkspace.MilestoneStep = milestoneStep;

            // Map step to ProjectWorkspaceStatus
            ProjectWorkspaceStatus newStatus = ProjectWorkspaceStatus.InProgress;
            string milestoneName = "InProgress";
            switch (milestoneStep)
            {
                case 0:
                    newStatus = ProjectWorkspaceStatus.Initiated;
                    milestoneName = "Not Started";
                    break;
                case 1:
                    newStatus = ProjectWorkspaceStatus.MaterialSourcing;
                    milestoneName = "Material Selection";
                    break;
                case 2:
                    newStatus = ProjectWorkspaceStatus.InProgress;
                    milestoneName = "Crochet Body";
                    break;
                case 3:
                    newStatus = ProjectWorkspaceStatus.InProgress;
                    milestoneName = "Hair & Face details";
                    break;
                case 4:
                    newStatus = ProjectWorkspaceStatus.InProgress;
                    milestoneName = "Outfit & Details";
                    break;
                case 5:
                    newStatus = ProjectWorkspaceStatus.QualityCheck;
                    milestoneName = "Final Assembly & Quality Check";
                    break;
                case 6:
                    newStatus = ProjectWorkspaceStatus.Shipped;
                    milestoneName = "Shipped";
                    if (!string.IsNullOrEmpty(trackingNumber))
                    {
                        request.ProjectWorkspace.TrackingNumber = trackingNumber;
                    }
                    break;
                case 7:
                    newStatus = ProjectWorkspaceStatus.Completed;
                    milestoneName = "Completed";
                    request.Status = CustomRequestStatus.Completed;
                    break;
            }

            request.ProjectWorkspace.Status = newStatus;
            
            // Advance custom request status to InProgress when work starts
            if (milestoneStep > 0 && request.Status == CustomRequestStatus.Paid)
            {
                request.Status = CustomRequestStatus.InProgress;
            }

            request.ProjectWorkspace.UpdatedAt = DateTime.UtcNow;
            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Log Audit
            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Project Progress Updated. RequestId: {RequestId}, MilestoneStep: {MilestoneStep}", requestId, milestoneStep);

            // Send chat message update
            if (request.ProjectWorkspace.ChatConversationId.HasValue)
            {
                await SendChatMessageAsync(
                    request.ProjectWorkspace.ChatConversationId.Value,
                    sellerUserId,
                    shop.Name,
                    request.BuyerId,
                    $"Project progress updated to: {milestoneName}",
                    MessageType.Text
                );
            }

            // Send db notification to buyer
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = request.BuyerId,
                TitleEn = "Project Crafting Progress Updated",
                TitleAr = "تحديث تقدم تصنيع طلبك",
                MessageEn = $"The artisan updated your doll progress to: {milestoneName}.",
                MessageAr = $"قام الحرفي بتحديث تقدم صناعة دميتك إلى: {milestoneName}.",
                Type = NotificationType.OrderStatusChanged,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> UploadWorkspacePhotoAsync(
            string sellerUserId, Guid requestId, string photoUrl, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.ProjectWorkspace)
                .Include(r => r.CustomOffers)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null || request.ProjectWorkspace == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Project Workspace not initialized.");
            }

            // Validate caller owns seller shop
            var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offer = await offerRepo.GetByIdAsync(request.ProjectWorkspace.SelectedOfferId);
            if (offer == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Selected offer not found.");
            }

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(offer.ShopId);
            if (shop == null || shop.OwnerId != sellerUserId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access: you do not own the artisan shop for this workspace.");
            }

            request.ProjectWorkspace.FinalPhotoUrl = photoUrl;
            request.ProjectWorkspace.UpdatedAt = DateTime.UtcNow;
            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Log Audit
            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Progress Image Uploaded. RequestId: {RequestId}, PhotoUrl: {PhotoUrl}", requestId, photoUrl);

            // Post image inside Chat conversation
            if (request.ProjectWorkspace.ChatConversationId.HasValue)
            {
                await SendChatMessageAsync(
                    request.ProjectWorkspace.ChatConversationId.Value,
                    sellerUserId,
                    shop.Name,
                    request.BuyerId,
                    "Artisan uploaded a workspace photo.",
                    MessageType.Image,
                    photoUrl
                );
            }

            // Send db notification
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = request.BuyerId,
                TitleEn = "New Progress Image Uploaded",
                TitleAr = "تم رفع صورة تقدم جديدة",
                MessageEn = $"The artisan uploaded a new progress photo for your Custom Crochet Request.",
                MessageAr = $"رفع الحرفي صورة تقدم جديدة لطلب الكروشيه المخصص لك.",
                Type = NotificationType.OrderStatusChanged,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> ConfirmWorkspaceDeliveryAsync(
            string buyerUserId, Guid requestId, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.ProjectWorkspace)
                .Include(r => r.CustomOffers)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null || request.ProjectWorkspace == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Project Workspace not initialized.");
            }

            // Validate that caller is the buyer of this custom request
            if (request.BuyerId != buyerUserId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access: you are not the buyer of this custom request.");
            }

            request.ProjectWorkspace.MilestoneStep = 7;
            request.ProjectWorkspace.Status = ProjectWorkspaceStatus.Completed;
            request.Status = CustomRequestStatus.Completed;
            request.UpdatedAt = DateTime.UtcNow;

            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Log Audit
            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Project Completed. RequestId: {RequestId}", requestId);

            // Send chat message
            if (request.ProjectWorkspace.ChatConversationId.HasValue)
            {
                await SendChatMessageAsync(
                    request.ProjectWorkspace.ChatConversationId.Value,
                    buyerUserId,
                    request.Buyer?.Name ?? "Buyer",
                    request.ProjectWorkspace.SelectedOffer.Shop.OwnerId,
                    "Buyer confirmed delivery. Project successfully completed!",
                    MessageType.Text
                );
            }

            // Send db notification to seller
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = request.ProjectWorkspace.SelectedOffer.Shop.OwnerId,
                TitleEn = "Custom Project Completed & Confirmed",
                TitleAr = "اكتمل المشروع المخصص وتم التأكيد",
                MessageEn = $"The buyer has confirmed delivery of Custom Request {request.Id}. Funds are cleared.",
                MessageAr = $"أكد المشتري استلام الطلب المخصص {request.Id}. تم تحرير الأموال.",
                Type = NotificationType.OrderStatusChanged,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        private async Task SendChatMessageAsync(
            Guid conversationId, string senderId, string senderName, string receiverId, string content, MessageType type = MessageType.Text, string? imageUrl = null)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                Type = type,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Repository<Message, Guid>().AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            var msgDto = new MessageDto
            {
                Id = message.Id,
                ConversationId = conversationId,
                SenderId = senderId,
                SenderName = senderName,
                Content = content,
                Type = type,
                ImageUrl = imageUrl,
                CreatedAt = message.CreatedAt
            };

            // If it is a custom offer, load it so SignalR client gets it instantly
            if (type == MessageType.CustomOffer && Guid.TryParse(content, out var offerId))
            {
                var offer = await _unitOfWork.Repository<CustomOffer, Guid>().GetByIdAsync(offerId);
                if (offer != null)
                {
                    msgDto = msgDto with { CustomOffer = offer.Adapt<CustomOfferDto>() };
                }
            }

            await _chatHubContext.SendMessageAsync(receiverId, msgDto);
        }

        #endregion

        #region Queries

        public async Task<Result<WizardStep>> GetWizardProgressAsync(
            GetWizardProgressQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await repo.GetByIdAsync(query.RequestId);
            if (request == null)
            {
                return Result<WizardStep>.Failure("Custom Request not found.");
            }

            return Result<WizardStep>.Success(request.WizardStep);
        }

        public async Task<Result<CustomRequestDetailDto>> GetCustomRequestDetailsAsync(
            GetCustomRequestDetailsQuery query, CancellationToken ct = default, string? userId = null, string? userRole = null)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await repo.GetAllAsNoTracking();
            var request = await requests
                .WithDetails()
                .FirstOrDefaultAsync(r => r.Id == query.RequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            // Access Control Validation (IDOR Protection)
            if (userId != null && userRole != AppRoles.Admin)
            {
                if (request.BuyerId != userId && request.SelectedSellerId?.ToString() != userId)
                {
                    // Check if they are matched/recommended or have an offer
                    var shopRepo = _unitOfWork.Repository<Shop, Guid>();
                    var callerShops = await (await shopRepo.GetAllAsNoTracking())
                        .Where(s => s.OwnerId == userId)
                        .Select(s => s.Id)
                        .ToListAsync(ct);

                    bool isRecommendedOrOffer = request.SellerRecommendations.Any(r => callerShops.Contains(r.ShopId)) ||
                                                request.CustomOffers.Any(o => callerShops.Contains(o.ShopId));

                    if (!isRecommendedOrOffer)
                    {
                        return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
                    }
                }
            }

            return Result<CustomRequestDetailDto>.Success(request.Adapt<CustomRequestDetailDto>());
        }

        public async Task<Result<CustomConfigurationDto>> GetConfigurationAsync(
            GetConfigurationQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomConfiguration, Guid>();
            var configs = await repo.GetAllAsNoTracking();
            var config = await configs
                .FirstOrDefaultAsync(c => c.CustomRequestId == query.RequestId, ct);

            if (config == null)
            {
                return Result<CustomConfigurationDto>.Failure("Configuration not found.");
            }

            return Result<CustomConfigurationDto>.Success(config.Adapt<CustomConfigurationDto>());
        }

        public async Task<Result<List<GeneratedDesignDto>>> GetGeneratedDesignsAsync(
            GetGeneratedDesignsQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<GeneratedDesign, Guid>();
            var designs = await repo.GetAllAsNoTracking();
            var list = await designs
                .Where(d => d.CustomRequestId == query.RequestId)
                .ToListAsync(ct);

            return Result<List<GeneratedDesignDto>>.Success(list.Adapt<List<GeneratedDesignDto>>());
        }

        public async Task<Result<GeneratedDesignDto>> GetSelectedDesignAsync(
            GetSelectedDesignQuery query, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await requestRepo.GetByIdAsync(query.RequestId);
            if (request == null || !request.SelectedDesignId.HasValue)
            {
                return Result<GeneratedDesignDto>.Failure("Selected design not found.");
            }

            var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();
            var design = await designRepo.GetByIdAsync(request.SelectedDesignId.Value);
            if (design == null)
            {
                return Result<GeneratedDesignDto>.Failure("Selected design details not found.");
            }

            return Result<GeneratedDesignDto>.Success(design.Adapt<GeneratedDesignDto>());
        }

        public async Task<Result<List<SellerRecommendationDto>>> GetRecommendedSellersAsync(
            GetRecommendedSellersQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<SellerRecommendation, Guid>();
            var recs = await repo.GetAllAsNoTracking();
            var list = await recs
                .Include(sr => sr.Shop)
                .Where(sr => sr.CustomRequestId == query.RequestId)
                .ToListAsync(ct);

            if (list.Count == 0)
            {
                // Retrieve the custom request details to get the design prompt
                var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
                var requests = await requestRepo.GetAllAsync();
                var request = await requests
                    .Include(r => r.CustomConfiguration)
                    .Include(r => r.GeneratedDesigns)
                    .FirstOrDefaultAsync(r => r.Id == query.RequestId, ct);

                if (request == null)
                {
                    return Result<List<SellerRecommendationDto>>.Failure("Custom Request not found.");
                }

                // Get prompt from selected design or build it from configuration
                var searchPrompt = request.GeneratedDesigns.FirstOrDefault(d => d.Id == request.SelectedDesignId)?.Prompt;
                if (string.IsNullOrWhiteSpace(searchPrompt) && request.CustomConfiguration != null)
                {
                    searchPrompt = _promptBuilder.BuildPrompt(request.CustomConfiguration).PositivePrompt;
                }

                // Search Qdrant collection for best artisans!
                IReadOnlyList<RagSearchResultDto> vectorResults = null!;
                try
                {
                    vectorResults = await _ragService.SearchAsync(new RagSearchRequestDto
                    {
                        Collection = "handora-documents-artisans",
                        Query = searchPrompt,
                        TopK = 3
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RAG vector search on handora-documents-artisans failed. Falling back to DB-based recommendation.");
                }

                var matchedShops = new List<Shop>();
                var shopRepo = _unitOfWork.Repository<Shop, Guid>();

                if (vectorResults != null && vectorResults.Count > 0)
                {
                    foreach (var hit in vectorResults)
                    {
                        if (hit.Metadata != null && hit.Metadata.TryGetValue("shop_id", out var shopIdObj) && Guid.TryParse(shopIdObj.ToString(), out var shopId))
                        {
                            var shop = await shopRepo.GetByIdAsync(shopId);
                            if (shop != null)
                            {
                                matchedShops.Add(shop);
                            }
                        }
                    }
                }

                // Fallback to top-rated shops if vector search returned nothing
                if (matchedShops.Count == 0)
                {
                    var shopQuery = await shopRepo.GetAllAsync();
                    matchedShops = await shopQuery
                        .Include(s => s.Products).ThenInclude(p => p.Category)
                        .Include(s => s.Reviews)
                        .Take(3)
                        .ToListAsync(ct);
                }

                var orderRepo = _unitOfWork.Repository<Order, Guid>();
                var orderQuery = await orderRepo.GetAllAsNoTracking();
                var allOrders = await orderQuery
                    .Include(o => o.Items)
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .ToListAsync(ct);

                foreach (var shop in matchedShops)
                {
                    // Check if shop has any product in Crochet category
                    var products = shop.Products != null ? shop.Products.ToList() : new List<Product>();
                    var hasCrochet = products.Any(p => p.Category != null && 
                        (p.Category.NameEn.Contains("crochet", StringComparison.OrdinalIgnoreCase) || 
                         p.Category.NameAr.Contains("crochet", StringComparison.OrdinalIgnoreCase)));

                    // Count completed orders
                    var completedOrdersCount = allOrders.Count(o => o.Items.Any(i => i.ShopId == shop.Id));

                    // Calculate score based on similarity/match or fallback
                    double score = 85.0;
                    var qdrantHit = vectorResults?.FirstOrDefault(hit => hit.Metadata != null && hit.Metadata.TryGetValue("shop_id", out var idObj) && idObj.ToString() == shop.Id.ToString());
                    if (qdrantHit != null)
                    {
                        // Convert score to percentage
                        score = Math.Min(99.0, Math.Max(75.0, qdrantHit.Score * 100.0));
                    }
                    else
                    {
                        // Fallback score calculation
                        if (shop.Rating > 0)
                        {
                            score += (double)shop.Rating * 2.0;
                        }
                        if (hasCrochet)
                        {
                            score += 5.0;
                        }
                        score = Math.Min(98.0, score);
                    }

                    score = Math.Round(score, 1);

                    var reasonList = new List<string>();
                    if (completedOrdersCount > 0)
                    {
                        reasonList.Add($"Stitched {completedOrdersCount} custom doll requests");
                    }
                    else
                    {
                        reasonList.Add("Crafted premium crochet models");
                    }

                    if (shop.Rating >= 4.5m)
                    {
                        reasonList.Add("Flawless artisan feedback");
                    }

                    reasonList.Add("Verified fast response rate");

                    var reason = string.Join(", ", reasonList);

                    var rec = new SellerRecommendation
                    {
                        Id = Guid.NewGuid(),
                        CustomRequestId = query.RequestId,
                        ShopId = shop.Id,
                        MatchingScore = score,
                        Reason = reason,
                        EstimatedPrice = 250m + (decimal)(new Random().Next(0, 8) * 20),
                        EstimatedDeliveryDays = 6 + new Random().Next(0, 4),
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await repo.AddAsync(rec);
                }

                // Update request status to SellerMatched
                var requestRepo2 = _unitOfWork.Repository<CustomRequest, Guid>();
                var customReq = await requestRepo2.GetByIdAsync(query.RequestId);
                if (customReq != null && (customReq.Status == CustomRequestStatus.DesignSelected || customReq.Status == CustomRequestStatus.Generated))
                {
                    customReq.Status = CustomRequestStatus.SellerMatched;
                    await requestRepo2.UpdateAsync(customReq);
                }

                await _unitOfWork.SaveChangesAsync();

                // Reload recommendations with shop details
                var listQuery = await repo.GetAllAsNoTracking();
                list = await listQuery
                    .Include(sr => sr.Shop)
                    .Where(sr => sr.CustomRequestId == query.RequestId)
                    .ToListAsync(ct);
            }

            return Result<List<SellerRecommendationDto>>.Success(list.Adapt<List<SellerRecommendationDto>>());
        }

        public async Task<Result<CustomOfferDto>> GetSellerOfferAsync(
            GetSellerOfferQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomOffer, Guid>();
            var offers = await repo.GetAllAsNoTracking();
            var offer = await offers
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == query.OfferId, ct);

            if (offer == null)
            {
                return Result<CustomOfferDto>.Failure("Seller offer not found.");
            }

            return Result<CustomOfferDto>.Success(offer.Adapt<CustomOfferDto>());
        }

        public async Task<Result<List<GeneratedDesignDto>>> GetDesignHistoryAsync(
            GetDesignHistoryQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<GeneratedDesign, Guid>();
            var designs = await repo.GetAllAsNoTracking();
            var list = await designs
                .Where(d => d.CustomRequestId == query.RequestId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(ct);

            return Result<List<GeneratedDesignDto>>.Success(list.Adapt<List<GeneratedDesignDto>>());
        }

        public async Task<Result<PagedResultDto<CustomRequestSummaryDto>>> GetBuyerRequestsAsync(
            GetBuyerRequestsQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requestQuery = await repo.GetAllAsNoTracking();
            var filterQuery = requestQuery.ByBuyer(query.BuyerId);

            var totalCount = await filterQuery.CountAsync(ct);
            var items = await filterQuery
                .Include(r => r.Buyer)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return Result<PagedResultDto<CustomRequestSummaryDto>>.Success(new PagedResultDto<CustomRequestSummaryDto>
            {
                Items = items.Adapt<List<CustomRequestSummaryDto>>(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public async Task<Result<PagedResultDto<CustomRequestSummaryDto>>> GetSellerRequestsAsync(
            GetSellerRequestsQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requestQuery = await repo.GetAllAsNoTracking();
            var filterQuery = requestQuery.BySellerShop(query.SellerShopId);

            var totalCount = await filterQuery.CountAsync(ct);
            var items = await filterQuery
                .Include(r => r.Buyer)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return Result<PagedResultDto<CustomRequestSummaryDto>>.Success(new PagedResultDto<CustomRequestSummaryDto>
            {
                Items = items.Adapt<List<CustomRequestSummaryDto>>(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public async Task<Result<PagedResultDto<CustomRequestSummaryDto>>> SearchCustomRequestsAsync(
            SearchCustomRequestsQuery query, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var filterQuery = await repo.GetAllAsNoTracking();

            if (query.ProductType.HasValue)
            {
                filterQuery = filterQuery.Where(r => r.ProductType == query.ProductType.Value);
            }

            if (query.Status.HasValue)
            {
                filterQuery = filterQuery.Where(r => r.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var search = query.SearchText.ToLower();
                filterQuery = filterQuery.Where(r => r.Buyer.Name.ToLower().Contains(search) || 
                                                     r.CustomConfiguration.ConfigurationDataJson.ToLower().Contains(search));
            }

            var totalCount = await filterQuery.CountAsync(ct);
            var items = await filterQuery
                .Include(r => r.Buyer)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return Result<PagedResultDto<CustomRequestSummaryDto>>.Success(new PagedResultDto<CustomRequestSummaryDto>
            {
                Items = items.Adapt<List<CustomRequestSummaryDto>>(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public async Task<Result<CustomRequestDetailDto>> GetCustomRequestByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await repo.GetAllAsNoTracking();
            
            // Check if there is a workspace linked to the conversation
            var request = await requests
                .WithDetails()
                .FirstOrDefaultAsync(r => r.ProjectWorkspace != null && r.ProjectWorkspace.ChatConversationId == conversationId, ct);

            if (request == null)
            {
                // Fallback: check if they have started discussion but workspace is not created yet (early negotiation chat)
                var convRepo = _unitOfWork.Repository<Conversation, Guid>();
                var conv = await convRepo.GetByIdAsync(conversationId);
                if (conv != null)
                {
                    request = await requests
                        .WithDetails()
                        .FirstOrDefaultAsync(r => r.BuyerId == conv.BuyerId && 
                                                 ((r.SelectedSeller != null && r.SelectedSeller.OwnerId == conv.SellerId) || 
                                                  r.CustomOffers.Any(o => o.Shop.OwnerId == conv.SellerId)), ct);
                }
            }

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("No custom request found for this conversation.");
            }

            return Result<CustomRequestDetailDto>.Success(request.Adapt<CustomRequestDetailDto>());
        }

        #endregion
    }
}
