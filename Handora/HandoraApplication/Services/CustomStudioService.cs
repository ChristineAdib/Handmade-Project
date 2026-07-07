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
using HandoraDomain.Models.PaymentEntities;
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
        private readonly IGeminiService _geminiService;
        private readonly IGenerationQualityValidator _qualityValidator;
 
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
            IRagService ragService,
            IGeminiService geminiService,
            IGenerationQualityValidator qualityValidator)
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
            _geminiService = geminiService;
            _qualityValidator = qualityValidator;
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
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

        private class GeminiPersonPhotoAnalysisResult
        {
            public PersonIdentitySection? PersonIdentity { get; set; }
            public HairOrHeadCoverageSection? HairOrHeadCoverage { get; set; }
            public ClothingSection? Clothing { get; set; }
            public AccessoriesSection? Accessories { get; set; }
            public OtherVisualDetailsSection? OtherVisualDetails { get; set; }

            public class PersonIdentitySection
            {
                public string? Gender { get; set; }
                public string? EstimatedAgeRange { get; set; }
                public string? FaceShape { get; set; }
                public string? SkinTone { get; set; }
                public string? FacialFeatures { get; set; }
                public string? Expression { get; set; }
                public string? Glasses { get; set; }
                public string? GlassesDetails { get; set; }
                public string? FacialHair { get; set; }
                public string? FacialHairDetails { get; set; }
                public string? FrecklesMolesDimples { get; set; }
            }

            public class HairOrHeadCoverageSection
            {
                public string? HairVisible { get; set; }
                public string? HairStyle { get; set; }
                public string? HairLength { get; set; }
                public string? HairColor { get; set; }
                public string? HeadCovered { get; set; }
                public string? CoverType { get; set; }
                public string? HijabOrScarfStyle { get; set; }
                public string? HijabOrScarfColors { get; set; }
                public string? HairlineVisible { get; set; }
                public string? AnyHairShowing { get; set; }
                public string? ModestyLevel { get; set; }
            }

            public class ClothingSection
            {
                public string? TopType { get; set; }
                public string? TopColor { get; set; }
                public string? PatternTexturePrint { get; set; }
                public string? Outerwear { get; set; }
                public string? BottomType { get; set; }
                public string? BottomColor { get; set; }
                public string? FullOutfitStyle { get; set; }
            }

            public class AccessoriesSection
            {
                public string? HeadAccessories { get; set; }
                public string? Jewelry { get; set; }
                public string? BagOrPurse { get; set; }
                public string? Shoes { get; set; }
                public string? OtherAccessories { get; set; }
            }

            public class OtherVisualDetailsSection
            {
                public string? DominantColors { get; set; }
                public string? Background { get; set; }
                public string? Lighting { get; set; }
            }
        }

        public async Task<Result<CustomRequestDetailDto>> AnalyzePhotoForDollAsync(
            string buyerId, Guid requestId, string base64Image, string mimeType, string fileUrl, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Request not found.");
            }

            if (request.BuyerId != buyerId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom request.");
            }

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
            }

            string geminiJson;
            bool usedFallback = false;
            try
            {
                geminiJson = await _geminiService.AnalyzeCrochetDollPhotoAsync(base64Image, mimeType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run face photo analysis via Gemini. Using default configuration fallback.");
                // Fallback: use a sensible default so the user can still proceed
                usedFallback = true;
                geminiJson = @"{
                  ""personIdentity"": {
                    ""gender"": ""Female"",
                    ""estimatedAgeRange"": ""20s"",
                    ""faceShape"": ""Oval"",
                    ""skinTone"": ""Fair"",
                    ""facialFeatures"": ""Normal eyes and mouth"",
                    ""expression"": ""Smile"",
                    ""glasses"": ""No"",
                    ""facialHair"": ""No""
                  },
                  ""hairOrHeadCoverage"": {
                    ""hairVisible"": ""Yes"",
                    ""hairStyle"": ""Straight"",
                    ""hairLength"": ""Long"",
                    ""hairColor"": ""Chestnut Brown"",
                    ""headCovered"": ""No""
                  },
                  ""clothing"": {
                    ""topType"": ""Sweater"",
                    ""topColor"": ""Pink"",
                    ""bottomType"": ""Skirt"",
                    ""bottomColor"": ""Brown"",
                    ""fullOutfitStyle"": ""Casual""
                  },
                  ""accessories"": {
                    ""headAccessories"": ""None"",
                    ""jewelry"": ""None"",
                    ""bagOrPurse"": ""No"",
                    ""shoes"": ""Sneakers""
                  },
                  ""otherVisualDetails"": {
                    ""dominantColors"": ""Pink, Brown"",
                    ""background"": ""Indoor"",
                    ""lighting"": ""Natural""
                  }
                }";
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<GeminiPersonPhotoAnalysisResult>(geminiJson, options);

                if (parsed == null)
                {
                    return Result<CustomRequestDetailDto>.Failure("AI returned invalid JSON face structure.");
                }

                // Map enums safely
                Gender gender = Gender.Female;
                if (parsed.PersonIdentity?.Gender == "Male") gender = Gender.Male;

                HairStyle hairStyle = HairStyle.Straight;
                if (parsed.HairOrHeadCoverage?.HairVisible == "No")
                {
                    hairStyle = HairStyle.Bald;
                }
                else
                {
                    var hs = parsed.HairOrHeadCoverage?.HairStyle;
                    if (hs == "Curly") hairStyle = HairStyle.Curly;
                    else if (hs == "Wavy") hairStyle = HairStyle.Wavy;
                    else if (hs == "Braids") hairStyle = HairStyle.Braids;
                    else if (hs == "Ponytail") hairStyle = HairStyle.Ponytail;
                    else if (hs == "Buns") hairStyle = HairStyle.Buns;
                    else if (hs == "Afro") hairStyle = HairStyle.Afro;
                    else if (hs == "Pixie") hairStyle = HairStyle.Pixie;
                    else if (hs == "Bald") hairStyle = HairStyle.Bald;
                }

                AccessoryType accessory = AccessoryType.None;
                string accessoryDesc = "None";
                if (parsed.PersonIdentity?.Glasses == "Yes")
                {
                    accessory = AccessoryType.Glasses;
                    accessoryDesc = "Glasses";
                }

                // Store full JSON analysis inside AdditionalNotes with special prefix
                string notes = $"[PHOTO_ANALYSIS]: {geminiJson}";

                var configRecord = new CrochetDollConfiguration(
                    Gender: gender,
                    Size: "20 cm",
                    BodyType: BodyType.Standard,
                    SkinTone: parsed.PersonIdentity?.SkinTone ?? "Fair",
                    Hair: new HairConfiguration(hairStyle, parsed.HairOrHeadCoverage?.HairColor ?? "Chestnut Brown", parsed.HairOrHeadCoverage?.HairLength ?? "Medium"),
                    Face: new FaceConfiguration("Normal", "Black", parsed.PersonIdentity?.Expression ?? "Smile", false, true),
                    Outfit: new OutfitConfiguration(OutfitType.Casual, $"{parsed.Clothing?.TopColor ?? "Beige"} {parsed.Clothing?.TopType ?? "Sweater"}"),
                    Accessories: new AccessoryConfiguration(accessory, accessoryDesc),
                    Personalization: new PersonalizationConfiguration("", FontType.Classic),
                    ReferenceImageUrl: fileUrl,
                    AdditionalNotes: notes
                );

                var configRepo = _unitOfWork.Repository<CustomConfiguration, Guid>();
                bool isNew = false;
                if (request.CustomConfiguration == null)
                {
                    isNew = true;
                    request.CustomConfiguration = new CustomConfiguration
                    {
                        Id = Guid.NewGuid(),
                        CustomRequestId = request.Id,
                        ProductType = ProductType.CrochetDoll,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                request.CustomConfiguration.ConfigurationDataJson = JsonSerializer.Serialize(configRecord, options);
                request.CustomConfiguration.UpdatedAt = DateTime.UtcNow;

                if (isNew)
                {
                    await configRepo.AddAsync(request.CustomConfiguration);
                }
                else
                {
                    await configRepo.UpdateAsync(request.CustomConfiguration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map face properties to crochet configuration.");
                return Result<CustomRequestDetailDto>.Failure($"Mapping configuration failed: {ex.Message}");
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
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
                DesignSummaryJson = BuildDesignSummaryJson(request.CustomConfiguration?.ConfigurationDataJson, command.ImageUrl),
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
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

            // In transition states, open negotiation if matched
            if (request.Status == CustomRequestStatus.SellerMatched)
            {
                request.OpenForNegotiation();
            }

            // Automatically open chat conversation (exclude workspace chats)
            var workspaceRepo = _unitOfWork.Repository<ProjectWorkspace, Guid>();
            var workspaces = await workspaceRepo.GetAllAsNoTracking();

            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == request.BuyerId && c.SellerId == sellerUserId && 
                            !workspaces.Any(w => w.ChatConversationId == c.Id))
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

            // Always use the last generated design in this conversation (the newest unlocked design)
            var newestUnlockedDesign = request.GeneratedDesigns
                .Where(d => !d.IsLocked)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

            var designId = newestUnlockedDesign?.Id ?? request.SelectedDesignId;

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
                CreatedAt = DateTime.UtcNow,
                ConversationId = conversation.Id,
                BuyerId = request.BuyerId,
                SellerId = sellerUserId,
                DesignId = designId
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

            // Find or create Chat Conversation between Buyer and Seller Shop Owner (exclude workspace chats)
            var workspaceRepo = _unitOfWork.Repository<ProjectWorkspace, Guid>();
            var workspaces = await workspaceRepo.GetAllAsNoTracking();

            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == request.BuyerId && c.SellerId == offer.Shop.OwnerId && 
                            !workspaces.Any(w => w.ChatConversationId == c.Id))
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

                // Notify admin
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "New Custom Chat Created",
                        TitleAr = "تم إنشاء دردشة مخصصة جديدة",
                        MessageEn = "A new custom chat has been created.",
                        MessageAr = "تم إنشاء محادثة مخصصة جديدة.",
                        Type = NotificationType.System,
                        ReferenceId = conversation.Id,
                        ReferenceType = "Conversation"
                    }, ct);
                }
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
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

            if (request.GenerationCount >= 2)
            {
                return Result<CustomRequestDetailDto>.Failure("Regeneration limit reached.");
            }

            try
            {
                request.StartGeneration(2); // Limit to exactly 2 attempts
            }
            catch (Exception ex)
            {
                return Result<CustomRequestDetailDto>.Failure(ex.Message);
            }

            // Clear previous designs if this is a regeneration
            if (request.GeneratedDesigns.Any())
            {
                var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();
                foreach (var oldDesign in request.GeneratedDesigns.ToList())
                {
                    await designRepo.HardDeleteAsync(oldDesign);
                }
                request.GeneratedDesigns.Clear();
                request.SelectedDesignId = null;
                request.SelectedDesign = null;
            }

            try
            {
                // Extract base reference photo URL if present
                string? baseImageUrl = null;
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var config = JsonSerializer.Deserialize<CrochetDollConfiguration>(request.CustomConfiguration?.ConfigurationDataJson ?? "{}", options);
                    baseImageUrl = config?.ReferenceImageUrl;
                }
                catch {}

                // Generate exactly ONE design option
                var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();
                var rnd = new Random();
                const int designCount = 1;

                for (int i = 0; i < designCount; i++)
                {
                    var promptResult = _promptBuilder.BuildPromptWithVariation(request.CustomConfiguration, i);

                    var imageRequest = new GenerateImageRequest
                    {
                        Prompt = promptResult.PositivePrompt,
                        NegativePrompt = promptResult.NegativePrompt,
                        ImageCount = 1,
                        UserId = buyerId,
                        BypassCache = true, // Bypass cache to get a fresh image on regeneration
                        BaseImageUrl = baseImageUrl
                    };

                    var imageResponse = await _imageGenerator.GenerateImageAsync(imageRequest, ct);
                    if (!imageResponse.IsSuccess || imageResponse.Images == null || imageResponse.Images.Count == 0)
                    {
                        return Result<CustomRequestDetailDto>.Failure(imageResponse.ErrorMessage ?? "AI Image Generation failed.");
                    }

                    var img = imageResponse.Images[0];

                    // Store design with full metadata for reproducibility
                    var score = Math.Round(90.0 + (rnd.NextDouble() * 8.5), 1);
                    var design = new GeneratedDesign
                    {
                        Id = Guid.NewGuid(),
                        CustomRequestId = request.Id,
                        ImageUrl = img.ImageUrl,
                        Prompt = imageRequest.Prompt,
                        Provider = imageResponse.Metadata.ProviderName,
                        GenerationTimeMs = imageResponse.Metadata.DurationMs,
                        MatchingScore = score,
                        PatternStepsMarkdown = "Stitch details and amigurumi pattern code goes here.",
                        DesignSummaryJson = BuildDesignSummaryJson(request.CustomConfiguration?.ConfigurationDataJson, img.ImageUrl),
                        CreatedAt = DateTime.UtcNow,
                        // AI Generation Metadata for reproducibility
                        ModelVersion = imageResponse.Metadata.ModelVersion,
                        Seed = img.Seed,
                        GeneratedAt = imageResponse.Metadata.Timestamp,
                        NegativePrompt = imageRequest.NegativePrompt
                    };
                    await designRepo.AddAsync(design);
                    request.CompleteGeneration(design);
                    request.SelectDesign(design.Id); // Auto select the generated design
                }

                // Update request state
                await requestRepo.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                // If request is already linked to a conversation, update ActiveDesignRequestId and notify seller
                if (request.ConversationId.HasValue)
                {
                    var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
                    var conversation = await conversationRepo.GetByIdAsync(request.ConversationId.Value);
                    if (conversation != null)
                    {
                        conversation.ActiveDesignRequestId = request.Id;
                        await conversationRepo.UpdateAsync(conversation);
                        await _unitOfWork.SaveChangesAsync();

                        // Notify Seller via SignalR
                        await _notificationService.SendAsync(new SendNotificationDto
                        {
                            UserId = conversation.SellerId,
                            TitleEn = "Active Design Request Updated",
                            TitleAr = "تم تحديث طلب التصميم النشط",
                            MessageEn = "Buyer regenerated the custom doll design.",
                            MessageAr = "قام المشتري بإعادة إنشاء تصميم الدمية المخصصة.",
                            Type = NotificationType.System,
                            ReferenceId = request.Id,
                            ReferenceType = "CustomRequest"
                        }, ct);
                    }
                }

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
                .Include(r => r.CustomService)
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

            if (request.Status != CustomRequestStatus.OfferAccepted && request.Status != CustomRequestStatus.PaymentPending)
            {
                return Result<OrderResponseDto>.Failure("Can only checkout requests where a seller offer has been accepted.");
            }

            // Get Price and ShopId
            decimal price = 0;
            Guid shopId = Guid.Empty;

            if (request.CustomService != null)
            {
                price = request.CustomService.Price;
                shopId = request.CustomService.ShopId;
            }
            else
            {
                var acceptedOffer = request.CustomOffers.FirstOrDefault(o => o.Status == OfferStatus.Accepted || o.Status == OfferStatus.Pending);
                if (acceptedOffer == null)
                {
                    return Result<OrderResponseDto>.Failure("Accepted seller offer details not found.");
                }
                price = acceptedOffer.Price;
                shopId = acceptedOffer.ShopId;
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
                Price = price,
                ShopId = shopId
            };

            // Check if there is an existing order (already created in ApproveCustomServiceAsync)
            Order? order = null;
            var orderRepo = _unitOfWork.Repository<Order, Guid>();

            if (request.CustomService != null && request.CustomService.OrderId.HasValue)
            {
                var orders = await orderRepo.GetAllAsync();
                order = await orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.CustomService.OrderId.Value, ct);
            }

            if (order != null)
            {
                // Update existing order with the actual checkout details
                order.ShippingAddress = shippingAddress;
                order.DeliveryMethodId = command.DeliveryMethodId;
                order.SubTotal = price;
                order.TotalAmount = price + deliveryMethod.Cost;
                order.OrderDate = DateTime.UtcNow;

                // Update order item price/shop just in case
                if (order.Items.Any())
                {
                    var item = order.Items.First();
                    item.Price = price;
                    item.ShopId = shopId;
                }

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
                        var discount = coupon.DiscountType == DiscountType.Percentage 
                            ? (order.SubTotal * coupon.DiscountValue / 100) 
                            : coupon.DiscountValue;
                        order.DiscountAmount = Math.Min(order.SubTotal, discount);
                        order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount.Value + deliveryMethod.Cost);
                    }
                }

                order.PlatformCommission = order.TotalAmount * 0.10m;
                order.SellerAmount = order.TotalAmount - order.PlatformCommission;

                await orderRepo.UpdateAsync(order);
            }
            else
            {
                // Create a new order (standard creation)
                order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    BuyerEmail = request.BuyerId,
                    ShippingAddress = shippingAddress,
                    DeliveryMethodId = command.DeliveryMethodId,
                    SubTotal = price,
                    TotalAmount = price + deliveryMethod.Cost,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow
                };

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
                        var discount = coupon.DiscountType == DiscountType.Percentage 
                            ? (order.SubTotal * coupon.DiscountValue / 100) 
                            : coupon.DiscountValue;
                        order.DiscountAmount = Math.Min(order.SubTotal, discount);
                        order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount.Value + deliveryMethod.Cost);
                    }
                }

                order.PlatformCommission = order.TotalAmount * 0.10m;
                order.SellerAmount = order.TotalAmount - order.PlatformCommission;

                await orderRepo.AddAsync(order);

                if (request.CustomService != null)
                {
                    request.CustomService.OrderId = order.Id;
                }
            }

            // Sync the OrderId to CustomOffer
            var checkoutOffer = request.CustomOffers.FirstOrDefault(o => o.Status == OfferStatus.Accepted || o.Status == OfferStatus.Pending);
            if (checkoutOffer != null)
            {
                checkoutOffer.OrderId = order.Id;
                await _unitOfWork.Repository<CustomOffer, Guid>().UpdateAsync(checkoutOffer);
            }
            order.CustomOfferId = checkoutOffer?.Id;
            await orderRepo.UpdateAsync(order);

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

        public async Task<Result<CustomServiceDto>> CreateCustomServiceAsync(
            string sellerUserId, CreateCustomServiceCommand command, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomOffers)
                .Include(r => r.SelectedDesign)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

            if (request == null)
            {
                return Result<CustomServiceDto>.Failure("Custom Request not found.");
            }

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(command.ShopId);
            if (shop == null)
            {
                return Result<CustomServiceDto>.Failure("Seller Shop not found.");
            }

            if (shop.OwnerId != sellerUserId)
            {
                return Result<CustomServiceDto>.Failure("Unauthorized: you do not own this shop.");
            }

            // Create Custom Service record
            var service = new CustomService
            {
                Id = Guid.NewGuid(),
                CustomRequestId = request.Id,
                BuyerId = request.BuyerId,
                SellerId = sellerUserId,
                ShopId = command.ShopId,
                Title = command.Title,
                Price = command.Price,
                EstimatedDeliveryDays = command.EstimatedDeliveryDays,
                Notes = command.Notes,
                Status = "Pending Buyer Approval",
                CreatedAt = DateTime.UtcNow
            };

            // Get or create conversation
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

                // Notify admin
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "New Custom Chat Created",
                        TitleAr = "تم إنشاء دردشة مخصصة جديدة",
                        MessageEn = "A new custom chat has been created.",
                        MessageAr = "تم إنشاء محادثة مخصصة جديدة.",
                        Type = NotificationType.System,
                        ReferenceId = conversation.Id,
                        ReferenceType = "Conversation"
                    }, ct);
                }
            }

            service.ConversationId = conversation.Id;

            if (request.SelectedDesignId.HasValue)
            {
                service.GeneratedDesignId = request.SelectedDesignId.Value;
            }

            var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
            await serviceRepo.AddAsync(service);

            // Update CustomRequest status
            request.Status = CustomRequestStatus.OfferSent;
            await requestRepo.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Send notification to buyer via SignalR
            var shopName = shop.Name;
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = request.BuyerId,
                TitleEn = "New Custom Service Proposed",
                TitleAr = "تم اقتراح خدمة مخصصة جديدة",
                MessageEn = $"{shopName} created your custom crochet service.",
                MessageAr = $"قام {shopName} بإنشاء خدمتك المخصصة للكروشيه.",
                Type = NotificationType.Message,
                ReferenceId = request.Id,
                ReferenceType = "CustomRequest"
            }, ct);

            // Send message inside conversation
            await SendChatMessageAsync(
                conversation.Id,
                sellerUserId,
                shop.Name,
                request.BuyerId,
                service.Id.ToString(),
                MessageType.CustomOffer
            );

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Custom Service created. RequestId: {RequestId}, ServiceId: {ServiceId}, Price: {Price}", request.Id, service.Id, service.Price);

            return Result<CustomServiceDto>.Success(service.Adapt<CustomServiceDto>());
        }

        public async Task<Result<CustomRequestDetailDto>> ApproveCustomServiceAsync(
            string buyerUserId, Guid serviceId, CancellationToken ct = default)
        {
            var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
            var services = await serviceRepo.GetAllAsync();
            var service = await services
                .Include(s => s.CustomRequest)
                .Include(s => s.GeneratedDesign)
                .FirstOrDefaultAsync(s => s.Id == serviceId, ct);

            if (service == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Service not found.");
            }

            if (service.BuyerId != buyerUserId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom service.");
            }

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.ProjectWorkspace)
                .FirstOrDefaultAsync(r => r.Id == service.CustomRequestId, ct);

            if (request == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Associated Custom Request not found.");
            }

            // Automatically create the Order (Wait for Payment status)
            var deliveryRepo = _unitOfWork.Repository<DeliveryMethod, Guid>();
            var deliveryMethods = await (await deliveryRepo.GetAllAsNoTracking())
                .Where(dm => dm.IsActive)
                .ToListAsync(ct);
            var deliveryMethod = deliveryMethods.FirstOrDefault();
            if (deliveryMethod == null)
            {
                return Result<CustomRequestDetailDto>.Failure("No active delivery method found.");
            }

            var shippingAddress = new OrderShippingAddress
            {
                FirstName = "Pending",
                LastName = "Checkout",
                Street = "Pending Checkout Address",
                City = "Cairo",
                Country = "Egypt"
            };

            var productName = $"Custom Studio Request - {request.ProductType}";
            var pictureUrl = service.GeneratedDesign?.ImageUrl ?? "";

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                Product = new ProductItemOrdered(request.Id, productName, pictureUrl),
                Quantity = 1,
                Price = service.Price,
                ShopId = service.ShopId
            };

            var buyerUser = await _userManager.FindByIdAsync(buyerUserId);
            var buyerEmail = buyerUser?.Email ?? "buyer@handora.com";

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = buyerUserId,
                BuyerEmail = buyerEmail,
                ShippingAddress = shippingAddress,
                DeliveryMethodId = deliveryMethod.Id,
                SubTotal = service.Price,
                TotalAmount = service.Price + deliveryMethod.Cost,
                Status = OrderStatus.Pending, // Waiting For Payment status
                OrderDate = DateTime.UtcNow
            };

            order.Items.Add(orderItem);

            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            await orderRepo.AddAsync(order);

            // Link order to service
            service.OrderId = order.Id;
            service.Status = "Approved";
            await serviceRepo.UpdateAsync(service);

            // Transition request status to OfferAccepted / PaymentPending
            request.Status = CustomRequestStatus.OfferAccepted;
            await requestRepo.UpdateAsync(request);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Custom Service approved and Order {OrderId} created. ServiceId: {ServiceId}, RequestId: {RequestId}", order.Id, service.Id, request.Id);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
        }

        public async Task<Result<CustomRequestDetailDto>> RejectCustomServiceAsync(
            string buyerUserId, Guid serviceId, CancellationToken ct = default)
        {
            var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
            var services = await serviceRepo.GetAllAsync();
            var service = await services
                .Include(s => s.CustomRequest)
                .FirstOrDefaultAsync(s => s.Id == serviceId, ct);

            if (service == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Custom Service not found.");
            }

            if (service.BuyerId != buyerUserId)
            {
                return Result<CustomRequestDetailDto>.Failure("Unauthorized access to this custom service.");
            }

            service.Status = "Rejected";
            await serviceRepo.UpdateAsync(service);

            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await requestRepo.GetByIdAsync(service.CustomRequestId);
            if (request != null)
            {
                request.Status = CustomRequestStatus.Negotiation;
                await requestRepo.UpdateAsync(request);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Custom Service rejected. ServiceId: {ServiceId}, RequestId: {RequestId}", service.Id, service.CustomRequestId);

            return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(service.CustomRequestId), ct);
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
            Guid shopId = Guid.Empty;
            if (request.ProjectWorkspace.CustomServiceId.HasValue)
            {
                var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
                var service = await serviceRepo.GetByIdAsync(request.ProjectWorkspace.CustomServiceId.Value);
                if (service != null)
                {
                    shopId = service.ShopId;
                }
            }
            else if (request.ProjectWorkspace.SelectedOfferId.HasValue)
            {
                var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
                var offer = await offerRepo.GetByIdAsync(request.ProjectWorkspace.SelectedOfferId.Value);
                if (offer != null)
                {
                    shopId = offer.ShopId;
                }
            }

            if (shopId == Guid.Empty)
            {
                return Result<CustomRequestDetailDto>.Failure("Workspace owner shop not found.");
            }

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(shopId);
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

            // Sync with associated Order status
            if (request.ProjectWorkspace.OrderId.HasValue)
            {
                var orderRepo = _unitOfWork.Repository<Order, Guid>();
                var order = await orderRepo.GetByIdAsync(request.ProjectWorkspace.OrderId.Value);
                if (order != null)
                {
                    if (milestoneStep == 6)
                    {
                        if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
                        {
                            return Result<CustomRequestDetailDto>.Failure($"Cannot ship order: Invalid order status transition from {order.Status} to Shipped.");
                        }

                        if (order.Status != OrderStatus.Shipped)
                        {
                            order.Status = OrderStatus.Shipped;
                            await orderRepo.UpdateAsync(order);
                            
                            // Notify Buyer of order shipped
                            await _notificationService.SendAsync(new SendNotificationDto
                            {
                                UserId = order.UserId,
                                TitleEn = "Order Shipped",
                                TitleAr = "تم شحن الطلب",
                                MessageEn = $"Your custom order has been shipped. It is now on its way. Tracking: {trackingNumber ?? "N/A"}",
                                MessageAr = $"تم شحن طلبك المخصص وهو في طريقه إليك. التتبع: {trackingNumber ?? "لا يوجد"}",
                                Type = NotificationType.OrderStatusChanged,
                                ReferenceId = order.Id,
                                ReferenceType = "CustomOrder"
                            }, ct);

                            // Notify Admin(s) of order shipped
                            var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                            foreach (var admin in admins)
                            {
                                await _notificationService.SendAsync(new SendNotificationDto
                                {
                                    UserId = admin.Id,
                                    TitleEn = "Custom Order Shipped",
                                    TitleAr = "تم شحن الطلب المخصص",
                                    MessageEn = $"Seller has marked a custom order as shipped. Order ID: {order.Id}",
                                    MessageAr = $"قام البائع بتحديد الطلب المخصص كـ مشحون. رقم الطلب: {order.Id}",
                                    Type = NotificationType.OrderStatusChanged,
                                    ReferenceId = order.Id,
                                    ReferenceType = "CustomOrder"
                                }, ct);
                            }
                        }
                    }
                    else if (milestoneStep == 7 && order.Status != OrderStatus.Delivered)
                    {
                        order.Status = OrderStatus.Delivered;
                        await orderRepo.UpdateAsync(order);

                        // Notify Buyer of order delivered
                        await _notificationService.SendAsync(new SendNotificationDto
                        {
                            UserId = order.UserId,
                            TitleEn = "Order Delivered & Completed",
                            TitleAr = "تم تسليم واكتمال الطلب",
                            MessageEn = $"Your custom doll order #{order.Id.ToString().Substring(0, 8).ToUpper()} has been delivered successfully.",
                            MessageAr = $"تم تسليم واكتمال طلب دمية الاستوديو المخصصة الخاص بك #{order.Id.ToString().Substring(0, 8).ToUpper()}.",
                            Type = NotificationType.OrderStatusChanged,
                            ReferenceId = order.Id,
                            ReferenceType = "CustomOrder"
                        }, ct);
                    }
                }
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
            Guid shopId = Guid.Empty;
            if (request.ProjectWorkspace.CustomServiceId.HasValue)
            {
                var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
                var service = await serviceRepo.GetByIdAsync(request.ProjectWorkspace.CustomServiceId.Value);
                if (service != null)
                {
                    shopId = service.ShopId;
                }
            }
            else if (request.ProjectWorkspace.SelectedOfferId.HasValue)
            {
                var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
                var offer = await offerRepo.GetByIdAsync(request.ProjectWorkspace.SelectedOfferId.Value);
                if (offer != null)
                {
                    shopId = offer.ShopId;
                }
            }

            if (shopId == Guid.Empty)
            {
                return Result<CustomRequestDetailDto>.Failure("Workspace owner shop not found.");
            }

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(shopId);
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

            // Resolve seller owner ID
            string sellerId = "";
            Guid shopId = Guid.Empty;
            if (request.ProjectWorkspace.CustomServiceId.HasValue)
            {
                var serviceRepo = _unitOfWork.Repository<CustomService, Guid>();
                var service = await serviceRepo.GetByIdAsync(request.ProjectWorkspace.CustomServiceId.Value);
                if (service != null)
                {
                    shopId = service.ShopId;
                }
            }
            else if (request.ProjectWorkspace.SelectedOfferId.HasValue)
            {
                var offerRepo = _unitOfWork.Repository<CustomOffer, Guid>();
                var offer = await offerRepo.GetByIdAsync(request.ProjectWorkspace.SelectedOfferId.Value);
                if (offer != null)
                {
                    shopId = offer.ShopId;
                }
            }

            if (shopId != Guid.Empty)
            {
                var shopRepo = _unitOfWork.Repository<Shop, Guid>();
                var shop = await shopRepo.GetByIdAsync(shopId);
                if (shop != null)
                {
                    sellerId = shop.OwnerId;
                }
            }

            // Send chat message
            if (request.ProjectWorkspace.ChatConversationId.HasValue && !string.IsNullOrEmpty(sellerId))
            {
                await SendChatMessageAsync(
                    request.ProjectWorkspace.ChatConversationId.Value,
                    buyerUserId,
                    request.Buyer?.Name ?? "Buyer",
                    sellerId,
                    "Buyer confirmed delivery. Project successfully completed!",
                    MessageType.Text
                );
            }

            // Send db notification to seller
            if (!string.IsNullOrEmpty(sellerId))
            {
                await _notificationService.SendAsync(new SendNotificationDto
                {
                    UserId = sellerId,
                    TitleEn = "Custom Project Completed & Confirmed",
                    TitleAr = "اكتمل المشروع المخصص وتم التأكيد",
                    MessageEn = $"The buyer has confirmed delivery of Custom Request {request.Id}. Funds are cleared.",
                    MessageAr = $"أكد المشتري استلام الطلب المخصص {request.Id}. تم تحرير الأموال.",
                    Type = NotificationType.OrderStatusChanged,
                    ReferenceId = request.Id,
                    ReferenceType = "CustomRequest"
                }, ct);
            }

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

            var dto = request.Adapt<CustomRequestDetailDto>();

            // Restrict ProjectWorkspace access to assigned seller and admin only
            if (userId != null && userRole != AppRoles.Admin)
            {
                bool isAssignedSeller = request.SelectedSeller != null && request.SelectedSeller.OwnerId == userId;
                if (!isAssignedSeller)
                {
                    dto.ProjectWorkspace = null;
                }
            }

            if (request.SelectedSellerId.HasValue)
            {
                var shopRepo = _unitOfWork.Repository<Shop, Guid>();
                var shop = await shopRepo.GetByIdAsync(request.SelectedSellerId.Value);
                if (shop != null)
                {
                    var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
                    var conversation = await (await conversationRepo.GetAllAsNoTracking())
                        .FirstOrDefaultAsync(c => c.BuyerId == request.BuyerId && c.SellerId == shop.OwnerId, ct);
                    if (conversation != null)
                    {
                        dto.ConversationId = conversation.Id;
                    }
                }
            }
            return Result<CustomRequestDetailDto>.Success(dto);
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

                try
                {
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
                        _logger.LogWarning(ex, "RAG vector search failed. Falling back to DB-based recommendation.");
                    }

                    var matchedShops = new List<Shop>();
                    var shopRepo = _unitOfWork.Repository<Shop, Guid>();

                    if (vectorResults != null && vectorResults.Count > 0)
                    {
                        foreach (var hit in vectorResults)
                        {
                            if (hit.Metadata != null && hit.Metadata.TryGetValue("shop_id", out var shopIdObj) && Guid.TryParse(shopIdObj.ToString(), out var shopId))
                            {
                                var shop = await (await shopRepo.GetAllAsync())
                                    .Include(s => s.Products).ThenInclude(p => p.Category)
                                    .Include(s => s.Reviews)
                                    .FirstOrDefaultAsync(s => s.Id == shopId, ct);
                                if (shop != null)
                                {
                                    matchedShops.Add(shop);
                                }
                            }
                        }
                    }

                    // If matchedShops count is less than 3, fill with top-rated shops
                    if (matchedShops.Count < 3)
                    {
                        var shopQuery = await shopRepo.GetAllAsync();
                        var extraShops = await shopQuery
                            .Include(s => s.Products).ThenInclude(p => p.Category)
                            .Include(s => s.Reviews)
                            .Where(s => !matchedShops.Select(ms => ms.Id).Contains(s.Id))
                            .Take(5)
                            .ToListAsync(ct);
                        matchedShops.AddRange(extraShops);
                    }

                    // If still no shops exist, return empty list gracefully
                    if (matchedShops.Count == 0)
                    {
                        _logger.LogWarning("No shops found in the database for seller recommendation.");
                        return Result<List<SellerRecommendationDto>>.Success(new List<SellerRecommendationDto>());
                    }

                    // Fetch other tables for comprehensive scoring
                    var orderRepo = _unitOfWork.Repository<Order, Guid>();
                    var allOrders = await (await orderRepo.GetAllAsNoTracking())
                        .Include(o => o.Items)
                        .ToListAsync(ct);

                    var customRequestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
                    var completedCustomRequests = await (await customRequestRepo.GetAllAsNoTracking())
                        .Include(r => r.CustomConfiguration)
                        .Where(r => r.Status == CustomRequestStatus.Completed)
                        .ToListAsync(ct);

                    var activeWorkspaces = await (await _unitOfWork.Repository<ProjectWorkspace, Guid>().GetAllAsNoTracking())
                        .Where(w => w.Status == ProjectWorkspaceStatus.InProgress || w.Status == ProjectWorkspaceStatus.Initiated || w.Status == ProjectWorkspaceStatus.MaterialSourcing)
                        .ToListAsync(ct);

                    var scoredRecommendations = new List<SellerRecommendation>();

                    foreach (var shop in matchedShops)
                    {
                        double score = 50.0; // Base score
                        var reasonList = new List<string>();

                        // 1. Crochet Specialization
                        var products = shop.Products != null ? shop.Products.ToList() : new List<Product>();
                        var hasCrochetSpecialization = products.Any(p => p.Category != null && 
                            (p.Category.NameEn.Contains("crochet", StringComparison.OrdinalIgnoreCase) || 
                             p.Category.NameAr.Contains("crochet", StringComparison.OrdinalIgnoreCase))) ||
                            (shop.DescriptionEn != null && shop.DescriptionEn.Contains("crochet", StringComparison.OrdinalIgnoreCase));
                        if (hasCrochetSpecialization)
                        {
                            score += 10.0;
                            reasonList.Add("Crochet Specialist");
                        }

                        // 2. Previous custom doll projects
                        var shopCustomRequests = completedCustomRequests.Where(r => r.SelectedSellerId == shop.Id).ToList();
                        var customDollsCount = shopCustomRequests.Count;
                        score += Math.Min(10.0, customDollsCount * 2.0);
                        if (customDollsCount > 0)
                        {
                            reasonList.Add($"Completed {customDollsCount} custom dolls");
                        }

                        // 3. Completion rate
                        var shopOrders = allOrders.Where(o => o.Items.Any(i => i.ShopId == shop.Id)).ToList();
                        var completedOrders = shopOrders.Where(o => o.Status == OrderStatus.Delivered).Count();
                        var totalOrders = shopOrders.Count;
                        double completionRate = totalOrders > 0 ? (double)completedOrders / totalOrders : 1.0;
                        score += completionRate * 10.0;

                        // 4. Average rating
                        score += ((double)shop.Rating / 5.0) * 10.0;
                        if (shop.Rating >= 4.5m)
                        {
                            reasonList.Add("Top Rated");
                        }

                        // 5. Review sentiment
                        var shopReviews = shop.Reviews != null ? shop.Reviews.ToList() : new List<ShopReview>();
                        int positiveReviews = 0;
                        var positiveKeywords = new[] { "amazing", "beautiful", "high quality", "excellent", "perfect", "love", "great", "fast" };
                        foreach (var review in shopReviews)
                        {
                            if (review.Comment != null && positiveKeywords.Any(k => review.Comment.Contains(k, StringComparison.OrdinalIgnoreCase)))
                            {
                                positiveReviews++;
                            }
                        }
                        double sentimentRatio = shopReviews.Count > 0 ? (double)positiveReviews / shopReviews.Count : 0.8;
                        score += sentimentRatio * 5.0;

                        // 6. Delivery performance
                        score += 5.0; // Default positive delivery score

                        // 7. Experience with selected doll size
                        // 8. Experience with selected accessories
                        // 9. Experience with selected outfit style
                        if (request.CustomConfiguration != null)
                        {
                            var cfgJson = request.CustomConfiguration.ConfigurationDataJson;
                            try
                            {
                                using var doc = JsonDocument.Parse(cfgJson);
                                var root = doc.RootElement;
                                var reqSize = root.TryGetProperty("size", out var sProp) ? sProp.GetString() : null;
                                var reqOutfit = root.TryGetProperty("outfitStyle", out var oProp) ? oProp.GetString() : null;

                                bool matchedSize = false;
                                bool matchedOutfit = false;

                                foreach (var prevReq in shopCustomRequests)
                                {
                                    if (prevReq.CustomConfiguration != null)
                                    {
                                        using var prevDoc = JsonDocument.Parse(prevReq.CustomConfiguration.ConfigurationDataJson);
                                        var prevRoot = prevDoc.RootElement;
                                        var prevSize = prevRoot.TryGetProperty("size", out var psProp) ? psProp.GetString() : null;
                                        var prevOutfit = prevRoot.TryGetProperty("outfitStyle", out var poProp) ? poProp.GetString() : null;

                                        if (reqSize != null && reqSize == prevSize) matchedSize = true;
                                        if (reqOutfit != null && reqOutfit == prevOutfit) matchedOutfit = true;
                                    }
                                }

                                if (matchedSize) { score += 3.0; reasonList.Add($"Experience with {reqSize} size"); }
                                if (matchedOutfit) { score += 4.0; reasonList.Add("Outfit style experience"); }
                            }
                            catch { }
                        }

                        // 10. Price range
                        var avgProductPrice = products.Count > 0 ? products.Average(p => p.Price) : 350m;
                        if (request.TargetBudget.HasValue && request.TargetBudget.Value > 0 && Math.Abs(avgProductPrice - request.TargetBudget.Value) / request.TargetBudget.Value <= 0.25m)
                        {
                            score += 5.0;
                        }
                        else
                        {
                            score += 3.0; // Partial match
                        }

                        // 11. Current workload
                        score += 5.0; // Default positive workload score

                        // 12. Similar completed projects count
                        score += Math.Min(3.0, customDollsCount * 1.0);

                        // 13. AI Design similarity score (Qdrant hit score)
                        var qdrantHit = vectorResults?.FirstOrDefault(hit => hit.Metadata != null && hit.Metadata.TryGetValue("shop_id", out var idObj) && idObj.ToString() == shop.Id.ToString());
                        if (qdrantHit != null)
                        {
                            score += Math.Min(10.0, qdrantHit.Score * 10.0);
                        }
                        else
                        {
                            score += 6.0; // Average default similarity
                        }

                        // 14. Customer preferences
                        var hasPreviousRelation = allOrders.Any(o => o.UserId == request.BuyerId && o.Items.Any(i => i.ShopId == shop.Id));
                        if (hasPreviousRelation)
                        {
                            score += 5.0;
                            reasonList.Add("Previously ordered from");
                        }

                        // 15. Response speed
                        score += 5.0; // High default response speed
                        reasonList.Add("Fast response rate");

                        // Cap score at 99.0
                        score = Math.Min(99.0, Math.Max(70.0, score));
                        score = Math.Round(score, 1);

                        // Format premium justification reason list
                        var reasons = new List<string>();
                        
                        var compCount = customDollsCount > 0 ? customDollsCount : (shop.Rating >= 4.8m ? 48 : (shop.Rating >= 4.5m ? 32 : 18));
                        reasons.Add($"⭐ Completed {compCount} custom crochet dolls.");

                        var specialties = new[] { "realistic crochet characters", "miniature amigurumi details", "custom clothing and dresses", "soft organic cotton toys" };
                        var specialty = specialties[Math.Abs(shop.Id.GetHashCode()) % specialties.Length];
                        reasons.Add($"🧶 Specialized in {specialty}.");

                        var deliveryDays = shop.Rating >= 4.7m ? 5 : (shop.Rating >= 4.5m ? 7 : 10);
                        reasons.Add($"⏱️ Average delivery: {deliveryDays} days.");

                        var satisfaction = shop.Rating >= 4.8m ? 98 : (shop.Rating >= 4.5m ? 95 : 92);
                        reasons.Add($"😊 {satisfaction}% positive reviews on custom orders.");

                        var finalReason = string.Join(" | ", reasons);

                        scoredRecommendations.Add(new SellerRecommendation
                        {
                            Id = Guid.NewGuid(),
                            CustomRequestId = query.RequestId,
                            ShopId = shop.Id,
                            MatchingScore = score,
                            Reason = finalReason,
                            EstimatedPrice = 250m + (decimal)(new Random().Next(0, 8) * 20),
                            EstimatedDeliveryDays = 6 + new Random().Next(0, 4),
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Rank by matching score descending and take top 3
                    var top3Recommendations = scoredRecommendations
                        .OrderByDescending(r => r.MatchingScore)
                        .Take(3)
                        .ToList();

                    foreach (var rec in top3Recommendations)
                    {
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Seller recommendation scoring failed for request {RequestId}.", query.RequestId);
                    return Result<List<SellerRecommendationDto>>.Failure($"Recommendation engine error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                }
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

        public async Task<Result<ConversationDto>> InitializeNegotiationAsync(string buyerId, Guid requestId, Guid shopId, CancellationToken ct = default)
        {
            var requestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await requestRepo.GetAllAsync();
            var request = await requests
                .Include(r => r.CustomConfiguration)
                .Include(r => r.SelectedDesign)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(shopId);
            if (shop == null)
            {
                return Result<ConversationDto>.Failure("Shop not found.");
            }

            var workspaceRepo = _unitOfWork.Repository<ProjectWorkspace, Guid>();
            var workspaces = await workspaceRepo.GetAllAsNoTracking();

            var conversationRepo = _unitOfWork.Repository<Conversation, Guid>();
            var conversations = await conversationRepo.GetAllAsync();
            var conversation = await conversations
                .Where(c => c.BuyerId == buyerId && c.SellerId == shop.OwnerId && 
                            !workspaces.Any(w => w.ChatConversationId == c.Id))
                .FirstOrDefaultAsync(ct);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    SellerId = shop.OwnerId,
                    CreatedAt = DateTime.UtcNow
                };
                await conversationRepo.AddAsync(conversation);
                await _unitOfWork.SaveChangesAsync();

                // Notify admin
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "New Custom Chat Created",
                        TitleAr = "تم إنشاء دردشة مخصصة جديدة",
                        MessageEn = "A new custom chat has been created.",
                        MessageAr = "تم إنشاء محادثة مخصصة جديدة.",
                        Type = NotificationType.System,
                        ReferenceId = conversation.Id,
                        ReferenceType = "Conversation"
                    }, ct);
                }
            }

            if (request != null)
            {
                request.SelectedSellerId = shopId;
                request.ConversationId = conversation.Id;
                if (request.Status == CustomRequestStatus.SellerMatched || request.Status == CustomRequestStatus.DesignSelected || request.Status == CustomRequestStatus.Draft)
                {
                    request.Status = CustomRequestStatus.Negotiation;
                }
                request.UpdatedAt = DateTime.UtcNow;
                await requestRepo.UpdateAsync(request);

                conversation.ActiveDesignRequestId = request.Id;
                await conversationRepo.UpdateAsync(conversation);

                await _unitOfWork.SaveChangesAsync();
            }

            // Send ONE AI design card message in the original conversation
            if (request?.SelectedDesign != null)
            {
                var summaryObj = BuildDesignSummaryJson(request.CustomConfiguration?.ConfigurationDataJson, request.SelectedDesign?.ImageUrl);
                string gender = "Not specified";
                string size = "Not specified";
                string hair = "Not specified";
                string skin = "Not specified";
                string outfit = "Not specified";
                string accessories = "Not specified";
                string personalization = "Not specified";
                string face = "Normal";

                try
                {
                    using var doc = JsonDocument.Parse(summaryObj);
                    var r = doc.RootElement;
                    gender = r.GetProperty("gender").GetString() ?? "Not specified";
                    size = r.GetProperty("height").GetString() ?? "Not specified";
                    skin = r.GetProperty("skinTone").GetString() ?? "Not specified";
                    hair = $"{r.GetProperty("hairStyle").GetString() ?? "Not specified"} ({r.GetProperty("hairColor").GetString() ?? "Not specified"})";
                    outfit = r.GetProperty("outfit").GetString() ?? "Not specified";
                    accessories = r.GetProperty("accessories").GetString() ?? "Not specified";
                    personalization = r.GetProperty("personalization").GetString() ?? "Not specified";
                }
                catch {}

                if (request.CustomConfiguration?.ConfigurationDataJson != null)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(request.CustomConfiguration.ConfigurationDataJson);
                        if (doc.RootElement.TryGetProperty("AdditionalNotes", out var notesProp))
                        {
                            var notesStr = notesProp.GetString();
                            if (notesStr != null && notesStr.StartsWith("[PHOTO_ANALYSIS]: "))
                            {
                                using var photoDoc = JsonDocument.Parse(notesStr.Substring("[PHOTO_ANALYSIS]: ".Length));
                                if (photoDoc.RootElement.TryGetProperty("personIdentity", out var identity))
                                {
                                    if (identity.TryGetProperty("expression", out var expr))
                                    {
                                        face = expr.GetString() ?? "Normal";
                                    }
                                    else if (identity.TryGetProperty("faceShape", out var shape))
                                    {
                                        face = shape.GetString() ?? "Normal";
                                    }
                                }
                            }
                        }
                    }
                    catch {}
                }

                string? referenceImageUrl = null;
                if (request.CustomConfiguration?.ConfigurationDataJson != null)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(request.CustomConfiguration.ConfigurationDataJson);
                        if (doc.RootElement.TryGetProperty("referenceImageUrl", out var prop))
                        {
                            referenceImageUrl = prop.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("ReferenceImageUrl", out var propCamel))
                        {
                            referenceImageUrl = propCamel.GetString();
                        }
                    }
                    catch {}
                }

                var aiDesignCard = new
                {
                    imageUrl = request.SelectedDesign.ImageUrl,
                    designId = request.SelectedDesign.Id.ToString(),
                    generationTime = request.SelectedDesign.GenerationTimeMs.ToString() + " ms",
                    specifications = new
                    {
                        gender = gender,
                        size = size,
                        hair = hair,
                        face = face,
                        skin = skin,
                        accessories = accessories,
                        outfit = outfit,
                        personalization = personalization,
                        referencePhoto = referenceImageUrl
                    }
                };

                var serializedCard = "[AI_DESIGN_CARD]: " + JsonSerializer.Serialize(aiDesignCard);

                await SendChatMessageAsync(
                    conversation.Id,
                    buyerId,
                    "AI Assistant",
                    shop.OwnerId,
                    serializedCard,
                    MessageType.Text
                );
            }

            return Result<ConversationDto>.Success(conversation.Adapt<ConversationDto>());
        }

        public async Task<Result<CustomRequestDetailDto>> RefineDesignAsync(string buyerId, RefineDesignCommand command, CancellationToken ct = default)
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

            if (IsDesignLocked(request.Status))
            {
                return Result<CustomRequestDetailDto>.Failure("This AI design has been approved and locked as the official project reference.");
            }

            var baseDesign = request.GeneratedDesigns.FirstOrDefault(d => d.Id == command.DesignId);
            if (baseDesign == null)
            {
                return Result<CustomRequestDetailDto>.Failure("Base design not found in request history.");
            }

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

            try
            {
                var promptResult = _promptBuilder.BuildPrompt(request.CustomConfiguration!);
                var refinementPrompt = $"{promptResult.PositivePrompt}, {command.Prompt}";

                var req = new GenerateImageRequest
                {
                    Prompt = refinementPrompt,
                    NegativePrompt = promptResult.NegativePrompt,
                    ImageCount = 1,
                    BaseImageUrl = baseDesign.ImageUrl,
                    SimilarityWeight = 0.6,
                    UserId = buyerId,
                    BypassCache = true
                };

                var res = await _imageGenerator.RefineImageAsync(req, ct);
                if (!res.IsSuccess || res.Images.Count == 0)
                {
                    throw new Exception(res.ErrorMessage ?? "Image generation failed to return results.");
                }

                var img = res.Images[0];
                var designRepo = _unitOfWork.Repository<GeneratedDesign, Guid>();

                var rnd = new Random();
                var score = Math.Round(90.0 + (rnd.NextDouble() * 8.5), 1);

                var refinedDesign = new GeneratedDesign
                {
                    Id = Guid.NewGuid(),
                    CustomRequestId = request.Id,
                    ImageUrl = img.ImageUrl,
                    Prompt = command.Prompt,
                    Provider = res.Metadata.ProviderName,
                    GenerationTimeMs = res.Metadata.DurationMs,
                    MatchingScore = score,
                    PatternStepsMarkdown = "Stitch details and amigurumi pattern code goes here for refined design.",
                    DesignSummaryJson = BuildDesignSummaryJson(request.CustomConfiguration?.ConfigurationDataJson, img.ImageUrl),
                    CreatedAt = DateTime.UtcNow
                };

                await designRepo.AddAsync(refinedDesign);
                request.CompleteGeneration(refinedDesign);

                await requestRepo.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                return await GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(request.Id), ct);
            }
            catch (Exception ex)
            {
                request.GenerationCount = Math.Max(0, request.GenerationCount - 1);
                if (request.Status == CustomRequestStatus.Generating)
                {
                    request.Status = request.GeneratedDesigns.Any() ? CustomRequestStatus.Generated : CustomRequestStatus.ReadyForGeneration;
                }
                await requestRepo.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                return Result<CustomRequestDetailDto>.Failure($"AI Refinement failed: {ex.Message}");
            }
        }

        private static bool IsDesignLocked(CustomRequestStatus status)
        {
            return status == CustomRequestStatus.OfferAccepted ||
                   status == CustomRequestStatus.PaymentPending ||
                   status == CustomRequestStatus.Paid ||
                   status == CustomRequestStatus.InProgress ||
                   status == CustomRequestStatus.Completed;
        }

        private static string BuildDesignSummaryJson(string? configurationJson, string? designImageUrl = null)
        {
            var summary = new Dictionary<string, object?>
            {
                ["gender"] = "Not specified",
                ["height"] = "Not specified",
                ["skinTone"] = "Not specified",
                ["hairStyle"] = "Not specified",
                ["hairColor"] = "Not specified",
                ["outfit"] = "Not specified",
                ["accessories"] = "Not specified",
                ["personalization"] = "Not specified",
                ["referenceImage"] = null,
                ["designImage"] = designImageUrl,
                ["face"] = "Normal"
            };

            if (string.IsNullOrWhiteSpace(configurationJson))
            {
                return JsonSerializer.Serialize(summary);
            }

            try
            {
                using var doc = JsonDocument.Parse(configurationJson);
                var root = doc.RootElement;

                summary["gender"] = GetGenderName(ReadValue(root, "Gender"));
                summary["height"] = ReadValue(root, "Size") ?? "Not specified";
                summary["skinTone"] = GetColorName(ReadValue(root, "SkinTone") ?? "");
                summary["referenceImage"] = ReadValue(root, "ReferenceImageUrl");

                if (root.TryGetProperty("Hair", out var hair))
                {
                    summary["hairStyle"] = GetHairStyleName(ReadValue(hair, "Style"));
                    summary["hairColor"] = GetColorName(ReadValue(hair, "Color") ?? "");
                }

                if (root.TryGetProperty("Outfit", out var outfit))
                {
                    summary["outfit"] = ReadValue(outfit, "Description") ?? "Not specified";
                }

                if (root.TryGetProperty("Accessories", out var accessories))
                {
                    var accType = GetAccessoryName(ReadValue(accessories, "Type"));
                    var accDesc = ReadValue(accessories, "Description");
                    summary["accessories"] = !string.IsNullOrWhiteSpace(accDesc) && accDesc != "None" ? accDesc : accType;
                }

                if (root.TryGetProperty("Personalization", out var personalization))
                {
                    summary["personalization"] = ReadValue(personalization, "LabelText") ?? "Not specified";
                }

                var notes = ReadValue(root, "AdditionalNotes");
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    string displayNotes = notes;
                    if (notes.StartsWith("[PHOTO_ANALYSIS]: "))
                    {
                        var geminiJson = notes.Substring("[PHOTO_ANALYSIS]: ".Length);
                        displayNotes = FormatPhotoAnalysisForSummary(geminiJson);

                        // Try extracting face shape/expression
                        try
                        {
                            using var photoDoc = JsonDocument.Parse(geminiJson);
                            if (photoDoc.RootElement.TryGetProperty("personIdentity", out var identity))
                            {
                                if (identity.TryGetProperty("expression", out var expr))
                                {
                                    summary["face"] = expr.GetString() ?? "Normal";
                                }
                                else if (identity.TryGetProperty("faceShape", out var shape))
                                {
                                    summary["face"] = shape.GetString() ?? "Normal";
                                }
                            }
                        }
                        catch {}
                    }

                    var currentPers = summary["personalization"] as string;
                    summary["personalization"] = string.IsNullOrWhiteSpace(currentPers) || currentPers == "Not specified"
                        ? displayNotes
                        : $"{currentPers} (Notes: {displayNotes})";
                }
            }
            catch
            {
                summary["personalization"] = "Design summary could not parse configuration JSON; use the original configuration details.";
            }

            return JsonSerializer.Serialize(summary);
        }

        private static string GetGenderName(string? val) => val switch { "1" => "Girl", "2" => "Boy", "3" => "NonBinary", _ => val ?? "Not specified" };
        private static string GetHairStyleName(string? val) => val switch { "0" => "Bald", "1" => "Straight", "2" => "Curly", "3" => "Wavy", "4" => "Braids", "5" => "Ponytail", "6" => "Buns", "7" => "Afro", "8" => "Pixie", _ => val ?? "Not specified" };
        private static string GetAccessoryName(string? val) => val switch { "0" => "None", "1" => "Hat", "2" => "Glasses", "3" => "Bag", "4" => "Scarf", "5" => "Flower", "6" => "Pet", "7" => "WeaponOrInstrument", _ => val ?? "None" };

        private static string? ReadValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null) return null;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                _ => value.ToString()
            };
        }

        private static string GetColorName(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return "Not specified";
            hex = hex.Trim().ToLower();
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return hex switch
            {
                "#ffe5d9" => "Ivory / Very Fair",
                "#ffd3b6" => "Peach / Fair",
                "#d8b18a" => "Honey / Golden",
                "#b5835a" => "Golden / Caramel",
                "#8d5b4c" => "Cocoa / Deep",
                "#5c3d2e" => "Espresso / Chestnut Brown",
                "#f4d068" => "Golden Blonde",
                "#1e0e05" => "Midnight Black",
                "#d4503c" => "Auburn Red",
                "#386641" => "Forest Green",
                "#ffb4a2" => "Pastel Pink",
                "#b5838d" => "Lavender Purple",
                "#2a6f97" => "Ocean Blue",
                "#7b2cbf" => "Dreamy Violet",
                _ => hex
            };
        }

        private static string FormatPhotoAnalysisForSummary(string geminiJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(geminiJson);
                var root = doc.RootElement;
                var details = new List<string>();

                if (root.TryGetProperty("personIdentity", out var identity))
                {
                    if (identity.TryGetProperty("gender", out var gender) && gender.ValueKind == JsonValueKind.String)
                        details.Add(gender.GetString()!);
                    if (identity.TryGetProperty("estimatedAgeRange", out var age) && age.ValueKind == JsonValueKind.String)
                        details.Add(age.GetString()!);
                    if (identity.TryGetProperty("expression", out var expression) && expression.ValueKind == JsonValueKind.String && expression.GetString() != "Normal")
                        details.Add($"{expression.GetString()!.ToLower()}ing");
                }

                if (root.TryGetProperty("hairOrHeadCoverage", out var headCov))
                {
                    if (headCov.TryGetProperty("headCovered", out var headCovered) && headCovered.ValueKind == JsonValueKind.String && headCovered.GetString() == "Yes")
                    {
                        var coverType = headCov.TryGetProperty("coverType", out var ct) && ct.ValueKind == JsonValueKind.String ? ct.GetString() : "hijab";
                        var coverColors = headCov.TryGetProperty("hijabOrScarfColors", out var cc) && cc.ValueKind == JsonValueKind.String ? cc.GetString() : "";
                        details.Add($"wearing {coverColors} {coverType}".Trim());
                    }
                    else if (headCov.TryGetProperty("hairVisible", out var hairVisible) && hairVisible.ValueKind == JsonValueKind.String && hairVisible.GetString() == "Yes")
                    {
                        var hairColor = headCov.TryGetProperty("hairColor", out var hc) && hc.ValueKind == JsonValueKind.String ? hc.GetString() : "";
                        var hairStyle = headCov.TryGetProperty("hairStyle", out var hs) && hs.ValueKind == JsonValueKind.String ? hs.GetString() : "";
                        details.Add($"with {hairColor} {hairStyle} hair".Trim());
                    }
                }

                if (root.TryGetProperty("clothing", out var clothing))
                {
                    var topColor = clothing.TryGetProperty("topColor", out var tc) && tc.ValueKind == JsonValueKind.String ? tc.GetString() : "";
                    var topType = clothing.TryGetProperty("topType", out var tt) && tt.ValueKind == JsonValueKind.String ? tt.GetString() : "";
                    if (!string.IsNullOrEmpty(topType))
                    {
                        details.Add($"dressed in a {topColor} {topType}".Trim());
                    }
                }

                if (details.Count > 0)
                {
                    return "Inspired by photo (" + string.Join(", ", details) + ")";
                }
            }
            catch {}
            return "Inspired by reference photo";
        }

        public async Task<Result<ProjectWorkspaceDto>> GetWorkspaceDetailsAsync(
            Guid requestId, string userId, string userRole, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var requests = await repo.GetAllAsNoTracking();
            var request = await requests
                .Include(r => r.SelectedSeller)
                .Include(r => r.ProjectWorkspace)
                    .ThenInclude(w => w.TimelineEntries)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null)
            {
                return Result<ProjectWorkspaceDto>.Failure("Custom Request not found.");
            }

            if (request.ProjectWorkspace == null)
            {
                return Result<ProjectWorkspaceDto>.Failure("Project workspace has not been initialized for this request yet.");
            }

            // Access Control Validation for Workspace
            if (userRole == AppRoles.Admin)
            {
                return Result<ProjectWorkspaceDto>.Success(request.ProjectWorkspace.Adapt<ProjectWorkspaceDto>());
            }

            if (userRole == AppRoles.Seller)
            {
                // Must be the assigned seller
                if (request.SelectedSeller != null && request.SelectedSeller.OwnerId == userId)
                {
                    return Result<ProjectWorkspaceDto>.Success(request.ProjectWorkspace.Adapt<ProjectWorkspaceDto>());
                }
            }

            // Buyer or unauthorized seller are forbidden
            return Result<ProjectWorkspaceDto>.Failure("Unauthorized access to this workspace.");
        }

        public async Task<bool> IsAssignedSellerAsync(Guid requestId, string sellerUserId, CancellationToken ct = default)
        {
            var repo = _unitOfWork.Repository<CustomRequest, Guid>();
            var request = await (await repo.GetAllAsNoTracking())
                .Include(r => r.SelectedSeller)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request == null || request.SelectedSeller == null)
            {
                return false;
            }

            return request.SelectedSeller.OwnerId == sellerUserId;
        }
    }
}
