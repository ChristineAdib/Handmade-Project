using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.DTOs.CustomStudioDTOs;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.DTOs.Common;
using HandoraApplication.IServices;
using HandoraApplication.AI.Interfaces;
using HandoraApplication.AI.DTOs;
using HandoraDomain.Consts;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;


namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/custom-studio")]
    [Produces("application/json")]
    [Authorize]
    public class CustomStudioController : ControllerBase
    {
        private readonly ICustomStudioService _customStudioService;
        private readonly IChatService _chatService;
        private readonly IFileService _fileService;
        private readonly IImageValidationService _imageValidationService;
        private readonly IAIImageGenerationService _aiImageGenerationService;

        public CustomStudioController(
            ICustomStudioService customStudioService,
            IChatService chatService,
            IFileService fileService,
            IImageValidationService imageValidationService,
            IAIImageGenerationService aiImageGenerationService)
        {
            _customStudioService = customStudioService ?? throw new ArgumentNullException(nameof(customStudioService));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _imageValidationService = imageValidationService ?? throw new ArgumentNullException(nameof(imageValidationService));
            _aiImageGenerationService = aiImageGenerationService ?? throw new ArgumentNullException(nameof(aiImageGenerationService));
        }

        #region Custom Request Endpoints

        /// <summary>
        /// Create a new custom request.
        /// </summary>
        [HttpPost("request")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCustomRequest(
            [FromBody] CreateCustomRequestCommand command, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.CreateCustomRequestAsync(buyerId, command, ct);
            
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to create request", result.Errors));
        }

        /// <summary>
        /// Get request details.
        /// </summary>
        [HttpGet("request/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomRequestDetails(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var result = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }

            if (result.Errors?.Any(e => e.Contains("Unauthorized")) == true)
            {
                return Forbid();
            }
            return NotFound(ApiResponse<object>.Fail("Request not found", result.Errors));
        }

        /// <summary>
        /// Return all buyer requests (paginated).
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDto<CustomRequestSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyRequests(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.GetBuyerRequestsAsync(new GetBuyerRequestsQuery(buyerId, pageNumber, pageSize), ct);
            
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<PagedResultDto<CustomRequestSummaryDto>>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to fetch requests", result.Errors));
        }

        /// <summary>
        /// Return all requests assigned/offered by seller (paginated).
        /// </summary>
        [HttpGet("seller/requests")]
        [Authorize(Roles = AppRoles.Seller)]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDto<CustomRequestSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellerRequests(
            [FromQuery] Guid shopId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _customStudioService.GetSellerRequestsAsync(new GetSellerRequestsQuery(shopId, pageNumber, pageSize), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<PagedResultDto<CustomRequestSummaryDto>>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to fetch seller requests", result.Errors));
        }

        /// <summary>
        /// Update configuration (supports partial updates).
        /// </summary>
        [HttpPut("request/{id:guid}")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateConfiguration(
            Guid id, [FromBody] SaveConfigurationCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.SaveConfigurationAsync(buyerId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to save configuration", result.Errors));
        }

        /// <summary>
        /// Cancel/delete request.
        /// </summary>
        [HttpDelete("request/{id:guid}")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelRequest(Guid id, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.CancelCustomRequestAsync(buyerId, new CancelCustomRequestCommand(id), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to cancel request", result.Errors));
        }

        #endregion

        #region Configurator Wizard Endpoints

        /// <summary>
        /// Return current wizard progress.
        /// </summary>
        [HttpGet("request/{id:guid}/progress")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWizardProgress(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var authResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!authResult.IsSuccess)
            {
                if (authResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", authResult.Errors));
            }

            var result = await _customStudioService.GetWizardProgressAsync(new GetWizardProgressQuery(id), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<string>.Ok(result.Data.ToString()));
            }
            return NotFound(ApiResponse<object>.Fail("Request progress not found", result.Errors));
        }

        /// <summary>
        /// Save current wizard step.
        /// </summary>
        [HttpPost("request/{id:guid}/step")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveWizardStep(
            Guid id, [FromBody] UpdateWizardStepCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.UpdateWizardStepAsync(buyerId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to save wizard step", result.Errors));
        }

        #endregion

        #region Reference Image Endpoints

        /// <summary>
        /// Upload reference image.
        /// </summary>
        [HttpPost("request/{id:guid}/reference-image")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadReferenceImage(
            Guid id, IFormFile file, CancellationToken ct)
        {
            var validation = _imageValidationService.ValidateImage(file);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail(validation.ErrorMessage));
            }

            try
            {
                var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var imageUrl = await _fileService.UploadFileAsync(file, "reference_images");
                var result = await _customStudioService.UploadReferenceImageMetadataAsync(
                    buyerId, new UploadReferenceImageMetadataCommand(id, imageUrl), ct);
                
                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
                }
                return BadRequest(ApiResponse<object>.Fail("Failed to register reference image", result.Errors));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Image upload failed: {ex.Message}"));
            }
        }

        #endregion

        #region AI Generation Endpoints

        /// <summary>
        /// Check AI Provider health and diagnostics.
        /// </summary>
        [HttpGet("ai-health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AIHealthCheckResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAiHealth(CancellationToken ct)
        {
            var health = await _aiImageGenerationService.CheckHealthAsync(ct);
            if (health.IsHealthy)
            {
                return Ok(ApiResponse<AIHealthCheckResult>.Ok(health, "AI Provider is healthy."));
            }
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                ApiResponse<AIHealthCheckResult>.Fail("AI Provider is unhealthy.", new[] { health.Details }));
        }

        /// <summary>
        /// Generate AI design images.
        /// </summary>
        [HttpPost("request/{id:guid}/generate")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateAiImages(Guid id, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.GenerateDesignAsync(buyerId, id, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("AI Generation failed", result.Errors));
        }

        /// <summary>
        /// Return generated designs.
        /// </summary>
        [HttpGet("request/{id:guid}/designs")]
        [ProducesResponseType(typeof(ApiResponse<List<GeneratedDesignDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGeneratedDesigns(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var authResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!authResult.IsSuccess)
            {
                if (authResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", authResult.Errors));
            }

            var result = await _customStudioService.GetGeneratedDesignsAsync(new GetGeneratedDesignsQuery(id), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<List<GeneratedDesignDto>>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to retrieve designs", result.Errors));
        }

        /// <summary>
        /// Select generated design.
        /// </summary>
        [HttpPost("request/{id:guid}/designs/{designId:guid}/select")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SelectGeneratedDesign(
            Guid id, Guid designId, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.ChooseGeneratedDesignAsync(buyerId, new ChooseGeneratedDesignCommand(id, designId), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to select design", result.Errors));
        }

        /// <summary>
        /// Save design details.
        /// </summary>
        [HttpPost("request/{id:guid}/designs/{designId:guid}/save")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveDesign(
            Guid id, Guid designId, [FromBody] SaveGeneratedDesignCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.SaveGeneratedDesignAsync(buyerId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to save design details", result.Errors));
        }

        /// <summary>
        /// Return design history.
        /// </summary>
        [HttpGet("request/{id:guid}/history")]
        [ProducesResponseType(typeof(ApiResponse<List<GeneratedDesignDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDesignHistory(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var authResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!authResult.IsSuccess)
            {
                if (authResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", authResult.Errors));
            }

            var result = await _customStudioService.GetDesignHistoryAsync(new GetDesignHistoryQuery(id), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<List<GeneratedDesignDto>>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to retrieve design history", result.Errors));
        }

        #endregion

        #region Seller Matching Endpoints

        /// <summary>
        /// Return recommended matching sellers.
        /// </summary>
        [HttpGet("request/{id:guid}/recommended-sellers")]
        [ProducesResponseType(typeof(ApiResponse<List<SellerRecommendationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecommendedSellers(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var authResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!authResult.IsSuccess)
            {
                if (authResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", authResult.Errors));
            }

            var result = await _customStudioService.GetRecommendedSellersAsync(new GetRecommendedSellersQuery(id), ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<List<SellerRecommendationDto>>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to fetch recommended sellers", result.Errors));
        }

        #endregion

        #region Negotiation & Chat Endpoints

        /// <summary>
        /// Create a discussion using the existing Chat module.
        /// </summary>
        [HttpPost("request/{id:guid}/seller/{sellerId}")]
        [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDiscussion(
            Guid id, string sellerId, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            try
            {
                // If sellerId can be parsed as a shop Guid, start conversation by shop
                if (Guid.TryParse(sellerId, out var shopId))
                {
                    var result = await _chatService.StartConversationByShopAsync(buyerId, shopId, ct);
                    return Ok(ApiResponse<ConversationDto>.Ok(result));
                }
                else
                {
                    // Otherwise, start conversation by seller's user ID
                    var result = await _chatService.StartConversationAsync(buyerId, sellerId, ct);
                    return Ok(ApiResponse<ConversationDto>.Ok(result));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Failed to create discussion: {ex.Message}"));
            }
        }

        /// <summary>
        /// Find Custom Request by associated conversation ID.
        /// </summary>
        [HttpGet("conversation/{conversationId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomRequestByConversation(
            Guid conversationId, CancellationToken ct)
        {
            var result = await _customStudioService.GetCustomRequestByConversationIdAsync(conversationId, ct);
            if (result.IsSuccess)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

                var authResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(result.Data!.Id), ct, userId, userRole);
                if (!authResult.IsSuccess)
                {
                    return Forbid();
                }

                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return NotFound(ApiResponse<object>.Fail("No custom request linked to this conversation", result.Errors));
        }

        #endregion

        #region Offer Endpoints

        /// <summary>
        /// Create a custom offer by a seller shop.
        /// </summary>
        [HttpPost("request/{id:guid}/offer")]
        [Authorize(Roles = AppRoles.Seller)]
        [ProducesResponseType(typeof(ApiResponse<CustomOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSellerOffer(
            Guid id, [FromBody] CreateSellerOfferCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var sellerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.CreateSellerOfferAsync(sellerUserId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomOfferDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to create offer", result.Errors));
        }

        /// <summary>
        /// Get the latest seller offer for a custom request.
        /// </summary>
        [HttpGet("request/{id:guid}/offer")]
        [ProducesResponseType(typeof(ApiResponse<CustomOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSellerOffer(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var requestResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!requestResult.IsSuccess)
            {
                if (requestResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", requestResult.Errors));
            }

            var latestOffer = requestResult.Data!.CustomOffers
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (latestOffer == null)
            {
                return NotFound(ApiResponse<object>.Fail("No custom offers exist for this request yet."));
            }

            return Ok(ApiResponse<CustomOfferDto>.Ok(latestOffer));
        }

        /// <summary>
        /// Accept offer.
        /// </summary>
        [HttpPost("request/{id:guid}/offer/accept")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptOffer(
            Guid id, [FromBody] AcceptOfferCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.AcceptOfferAsync(buyerId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to accept offer", result.Errors));
        }

        /// <summary>
        /// Reject offer.
        /// </summary>
        [HttpPost("request/{id:guid}/offer/reject")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectOffer(
            Guid id, [FromBody] RejectOfferCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.RejectOfferAsync(buyerId, command, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomOfferDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to reject offer", result.Errors));
        }

        public class RequestChangesRequest
        {
            public Guid OfferId { get; set; }
            public string Feedback { get; set; } = string.Empty;
        }

        /// <summary>
        /// Buyer requests changes on offer.
        /// </summary>
        [HttpPost("request/{id:guid}/offer/request-changes")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestChanges(
            Guid id, [FromBody] RequestChangesRequest requestBody, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.RequestChangesAsync(buyerId, requestBody.OfferId, requestBody.Feedback, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomOfferDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to request changes", result.Errors));
        }

        #endregion

        #region Checkout Endpoint

        /// <summary>
        /// Convert accepted offer into a pending Order.
        /// </summary>
        [HttpPost("request/{id:guid}/checkout")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Checkout(
            Guid id, [FromBody] CheckoutCustomRequestCommand command, CancellationToken ct)
        {
            if (id != command.RequestId)
            {
                return BadRequest(ApiResponse<object>.Fail("Request ID mismatch"));
            }

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.CheckoutCustomRequestAsync(buyerId, command, ct);
            
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<OrderResponseDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to complete checkout", result.Errors));
        }

        #endregion

        #region Workspace Endpoints

        /// <summary>
        /// Return project workspace progress and details.
        /// </summary>
        [HttpGet("request/{id:guid}/workspace")]
        [ProducesResponseType(typeof(ApiResponse<ProjectWorkspaceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceDetails(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : (User.IsInRole(AppRoles.Seller) ? AppRoles.Seller : AppRoles.Buyer);

            var requestResult = await _customStudioService.GetCustomRequestDetailsAsync(new GetCustomRequestDetailsQuery(id), ct, userId, userRole);
            if (!requestResult.IsSuccess)
            {
                if (requestResult.Errors?.Any(e => e.Contains("Unauthorized")) == true)
                {
                    return Forbid();
                }
                return NotFound(ApiResponse<object>.Fail("Request not found", requestResult.Errors));
            }

            if (requestResult.Data!.ProjectWorkspace == null)
            {
                return NotFound(ApiResponse<object>.Fail("Project workspace has not been initialized for this request yet. Accept an offer to initialize."));
            }

            return Ok(ApiResponse<ProjectWorkspaceDto>.Ok(requestResult.Data.ProjectWorkspace));
        }

        public class UpdateProgressRequest
        {
            public int MilestoneStep { get; set; }
            public string? TrackingNumber { get; set; }
        }

        /// <summary>
        /// Update crafting milestone step and progress details.
        /// </summary>
        [HttpPost("request/{id:guid}/workspace/progress")]
        [Authorize(Roles = AppRoles.Seller)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateWorkspaceProgress(
            Guid id, [FromBody] UpdateProgressRequest requestBody, CancellationToken ct)
        {
            var sellerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.UpdateWorkspaceProgressAsync(
                sellerUserId, id, requestBody.MilestoneStep, requestBody.TrackingNumber, ct);
            
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to update project progress", result.Errors));
        }

        /// <summary>
        /// Upload artisan final/progress workspace photo.
        /// </summary>
        [HttpPost("request/{id:guid}/workspace/photo")]
        [Authorize(Roles = AppRoles.Seller)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadWorkspacePhoto(
            Guid id, IFormFile file, CancellationToken ct)
        {
            var validation = _imageValidationService.ValidateImage(file);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail(validation.ErrorMessage));
            }

            try
            {
                var sellerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var photoUrl = await _fileService.UploadFileAsync(file, "workspace_images");
                var result = await _customStudioService.UploadWorkspacePhotoAsync(
                    sellerUserId, id, photoUrl, ct);
                
                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
                }
                return BadRequest(ApiResponse<object>.Fail("Failed to register workspace image", result.Errors));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Image upload failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Buyer confirms delivery of custom project.
        /// </summary>
        [HttpPost("request/{id:guid}/workspace/confirm")]
        [Authorize(Roles = AppRoles.Buyer)]
        [ProducesResponseType(typeof(ApiResponse<CustomRequestDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmWorkspaceDelivery(
            Guid id, CancellationToken ct)
        {
            var buyerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _customStudioService.ConfirmWorkspaceDeliveryAsync(buyerUserId, id, ct);
            if (result.IsSuccess)
            {
                return Ok(ApiResponse<CustomRequestDetailDto>.Ok(result.Data!));
            }
            return BadRequest(ApiResponse<object>.Fail("Failed to confirm delivery", result.Errors));
        }

        #endregion
    }
}
