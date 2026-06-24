using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Consts;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.PaymentEntities;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services
{
    public class AdminCustomStudioService : IAdminCustomStudioService
    {
        private readonly IUnitOfWork _uow;

        public AdminCustomStudioService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        // ─── Helper for State/Status Labels ──────────────────────────────────────
        private string GetRequestStatusLabel(CustomRequestStatus status)
        {
            return status switch
            {
                CustomRequestStatus.Draft => "Draft",
                CustomRequestStatus.Configuring => "Configuring",
                CustomRequestStatus.ReadyForGeneration => "Ready for Generation",
                CustomRequestStatus.Generating => "Generating",
                CustomRequestStatus.Generated => "Generated",
                CustomRequestStatus.DesignSelected => "Design Selected",
                CustomRequestStatus.SellerMatched => "Seller Matched",
                CustomRequestStatus.Negotiation => "Negotiation",
                CustomRequestStatus.OfferSent => "Offer Sent",
                CustomRequestStatus.OfferAccepted => "Offer Accepted",
                CustomRequestStatus.PaymentPending => "Payment Pending",
                CustomRequestStatus.Paid => "Paid (Deposit)",
                CustomRequestStatus.InProgress => "In Crafting Progress",
                CustomRequestStatus.Completed => "Completed / Delivered",
                CustomRequestStatus.Cancelled => "Cancelled",
                CustomRequestStatus.Rejected => "Rejected",
                _ => "Unknown"
            };
        }

        // ─── 1. Dashboard ────────────────────────────────────────────────────────
        public async Task<Result<AdminCustomStudioDashboardDto>> GetDashboardMetricsAsync(CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfToday = now.Date;
                var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var projectsRepo = _uow.Repository<ProjectWorkspace, Guid>();
                var offersRepo = _uow.Repository<CustomOffer, Guid>();
                var designsRepo = _uow.Repository<GeneratedDesign, Guid>();
                var configsRepo = _uow.Repository<CustomConfiguration, Guid>();

                var requestsQuery = await requestsRepo.GetAllAsNoTracking();
                var projectsQuery = await projectsRepo.GetAllAsNoTracking();
                var offersQuery = await offersRepo.GetAllAsNoTracking();
                var designsQuery = await designsRepo.GetAllAsNoTracking();
                var configsQuery = await configsRepo.GetAllAsNoTracking();

                var totalRequests = await requestsQuery.CountAsync(ct);
                var requestsToday = await requestsQuery.CountAsync(r => r.CreatedAt >= startOfToday, ct);
                var requestsThisMonth = await requestsQuery.CountAsync(r => r.CreatedAt >= startOfMonth, ct);
                var completedProjects = await projectsQuery.CountAsync(w => w.Status == ProjectWorkspaceStatus.Completed, ct);
                var cancelledRequests = await requestsQuery.CountAsync(r => r.Status == CustomRequestStatus.Cancelled, ct);
                
                var pendingOffers = await offersQuery.CountAsync(o => o.Status == OfferStatus.Pending, ct);
                var acceptedOffers = await offersQuery.CountAsync(o => o.Status == OfferStatus.Accepted, ct);
                var rejectedOffers = await offersQuery.CountAsync(o => o.Status == OfferStatus.Rejected, ct);

                // Avg Completion Time
                var completedWorkspaces = await projectsQuery
                    .Where(w => w.Status == ProjectWorkspaceStatus.Completed)
                    .Select(w => new { w.CreatedAt, w.UpdatedAt })
                    .ToListAsync(ct);
                double avgCompletionTime = completedWorkspaces.Any()
                    ? completedWorkspaces.Average(w => ((w.UpdatedAt ?? DateTime.UtcNow) - w.CreatedAt).TotalDays)
                    : 0.0;

                // Avg Offer Price
                var avgOfferPrice = await offersQuery.AverageAsync(o => (decimal?)o.Price, ct) ?? 0m;

                // Avg Generation Time
                var avgGenTimeMs = await designsQuery.AverageAsync(d => (double?)d.GenerationTimeMs, ct) ?? 0;
                double avgGenTimeSeconds = avgGenTimeMs / 1000.0;

                // Conversion Rate (converted requests with accepted bids)
                var acceptedBidsRequestsCount = await requestsQuery.CountAsync(r => r.ProjectWorkspace != null, ct);
                double conversionRate = totalRequests > 0 ? (double)acceptedBidsRequestsCount / totalRequests * 100.0 : 0.0;

                // Popular Choices
                var configs = await configsQuery.Select(c => c.ConfigurationDataJson).ToListAsync(ct);
                var hairStyles = new Dictionary<string, int>();
                var outfits = new Dictionary<string, int>();
                var accessories = new Dictionary<string, int>();

                foreach (var json in configs)
                {
                    if (string.IsNullOrWhiteSpace(json)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("hair", out var hairProp) && hairProp.TryGetProperty("style", out var styleProp))
                        {
                            var val = styleProp.ToString();
                            if (!string.IsNullOrEmpty(val)) hairStyles[val] = hairStyles.GetValueOrDefault(val) + 1;
                        }
                        if (root.TryGetProperty("outfit", out var outfitProp) && outfitProp.TryGetProperty("type", out var outfitTypeProp))
                        {
                            var val = outfitTypeProp.ToString();
                            if (!string.IsNullOrEmpty(val)) outfits[val] = outfits.GetValueOrDefault(val) + 1;
                        }
                        if (root.TryGetProperty("accessories", out var accProp) && accProp.TryGetProperty("type", out var accTypeProp))
                        {
                            var val = accTypeProp.ToString();
                            if (!string.IsNullOrEmpty(val)) accessories[val] = accessories.GetValueOrDefault(val) + 1;
                        }
                    }
                    catch { /* Safe Ignore */ }
                }

                var popHair = hairStyles.OrderByDescending(x => x.Value).Select(x => x.Key).FirstOrDefault() ?? "N/A";
                var popOutfit = outfits.OrderByDescending(x => x.Value).Select(x => x.Key).FirstOrDefault() ?? "N/A";
                var popAcc = accessories.OrderByDescending(x => x.Value).Select(x => x.Key).FirstOrDefault() ?? "N/A";

                var dto = new AdminCustomStudioDashboardDto
                {
                    TotalRequests = totalRequests,
                    RequestsToday = requestsToday,
                    RequestsThisMonth = requestsThisMonth,
                    CompletedProjects = completedProjects,
                    CancelledProjects = cancelledRequests,
                    PendingOffers = pendingOffers,
                    AcceptedOffers = acceptedOffers,
                    RejectedOffers = rejectedOffers,
                    AvgCompletionTimeDays = Math.Round(avgCompletionTime, 1),
                    AvgOfferPrice = Math.Round(avgOfferPrice, 2),
                    AvgAiGenerationTimeSeconds = Math.Round(avgGenTimeSeconds, 1),
                    ConversionRatePercent = Math.Round(conversionRate, 1),
                    MostPopularHairStyle = popHair,
                    MostPopularOutfit = popOutfit,
                    MostPopularAccessories = popAcc
                };

                return Result<AdminCustomStudioDashboardDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminCustomStudioDashboardDto>.Failure($"Failed to fetch dashboard metrics: {ex.Message}");
            }
        }

        // ─── 2. Custom Requests ──────────────────────────────────────────────────
        public async Task<Result<PagedList<AdminCustomRequestDto>>> GetRequestsAsync(
            string? search, string? buyerId, string? sellerId, int? status, 
            int? offerStatus, int? paymentStatus, string? productType, 
            DateTime? startDate, DateTime? endDate, string sortBy, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var requestsQuery = await requestsRepo.GetAllAsNoTracking();

                var query = requestsQuery
                    .Include(r => r.Buyer)
                    .Include(r => r.SelectedSeller)
                    .Include(r => r.SelectedDesign)
                    .Include(r => r.ProjectWorkspace)
                    .Include(r => r.CustomOffers)
                    .AsQueryable();

                // Filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(r => r.Buyer.Name.Contains(search) || 
                                             r.Id.ToString().Contains(search) ||
                                             (r.SelectedSeller != null && r.SelectedSeller.Name.Contains(search)));
                }
                if (!string.IsNullOrWhiteSpace(buyerId))
                {
                    query = query.Where(r => r.BuyerId == buyerId);
                }
                if (!string.IsNullOrWhiteSpace(sellerId))
                {
                    query = query.Where(r => r.SelectedSellerId == Guid.Parse(sellerId));
                }
                if (status.HasValue)
                {
                    query = query.Where(r => (int)r.Status == status.Value);
                }
                if (offerStatus.HasValue)
                {
                    query = query.Where(r => r.CustomOffers.Any(o => (int)o.Status == offerStatus.Value));
                }
                if (paymentStatus.HasValue)
                {
                    query = query.Where(r => r.ProjectWorkspace != null && (int)r.ProjectWorkspace.PaymentStatus == paymentStatus.Value);
                }
                if (startDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt <= endDate.Value);
                }

                // Sorting
                query = sortBy.ToLower() switch
                {
                    "oldest" => query.OrderBy(r => r.CreatedAt),
                    "highestprice" => query.OrderByDescending(r => r.TargetBudget),
                    "lowestprice" => query.OrderBy(r => r.TargetBudget),
                    _ => query.OrderByDescending(r => r.CreatedAt) // default newest
                };

                var count = await query.CountAsync(ct);
                var rawItems = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = rawItems.Select(r => new AdminCustomRequestDto
                {
                    RequestId = r.Id,
                    BuyerName = r.Buyer.Name,
                    BuyerEmail = r.Buyer.Email ?? "",
                    SellerName = r.SelectedSeller != null ? r.SelectedSeller.Name : "Not Matched Yet",
                    Status = (int)r.Status,
                    StatusName = GetRequestStatusLabel(r.Status),
                    CreatedDate = r.CreatedAt,
                    CurrentStep = (int)r.WizardStep,
                    SelectedProduct = r.ProductType.ToString(),
                    SelectedDesignImageUrl = r.SelectedDesign != null ? r.SelectedDesign.ImageUrl : "",
                    OfferStatus = r.CustomOffers.Any() ? r.CustomOffers.OrderByDescending(o => o.CreatedAt).First().Status.ToString() : "No Offers",
                    PaymentStatus = r.ProjectWorkspace != null ? r.ProjectWorkspace.PaymentStatus.ToString() : "N/A",
                    TargetBudget = r.TargetBudget,
                    IsArchived = r.IsDeleted
                }).ToList();

                return Result<PagedList<AdminCustomRequestDto>>.Success(new PagedList<AdminCustomRequestDto>(items, count, pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                return Result<PagedList<AdminCustomRequestDto>>.Failure($"Failed to fetch custom requests: {ex.Message}");
            }
        }

        public async Task<Result<CustomRequest>> GetRequestDetailsAsync(Guid requestId, CancellationToken ct = default)
        {
            try
            {
                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var requestsQuery = await requestsRepo.GetAllAsNoTracking();

                var request = await requestsQuery
                    .Include(r => r.Buyer)
                    .Include(r => r.SelectedSeller)
                    .Include(r => r.SelectedDesign)
                    .Include(r => r.CustomConfiguration)
                    .Include(r => r.GeneratedDesigns)
                    .Include(r => r.SellerRecommendations).ThenInclude(sr => sr.Shop)
                    .Include(r => r.CustomOffers).ThenInclude(o => o.Shop)
                    .Include(r => r.ProjectWorkspace).ThenInclude(w => w.SelectedOffer).ThenInclude(o => o.Shop)
                    .FirstOrDefaultAsync(r => r.Id == requestId, ct);

                if (request == null)
                    return Result<CustomRequest>.Failure("Custom Request not found.");

                return Result<CustomRequest>.Success(request);
            }
            catch (Exception ex)
            {
                return Result<CustomRequest>.Failure($"Failed to retrieve request details: {ex.Message}");
            }
        }

        public async Task<Result> CancelRequestAsync(Guid requestId, string reason, CancellationToken ct = default)
        {
            try
            {
                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var requestsQuery = await requestsRepo.GetAllAsync();

                var request = await requestsQuery
                    .Include(r => r.ProjectWorkspace)
                    .FirstOrDefaultAsync(r => r.Id == requestId, ct);

                if (request == null) return Result.Failure("Request not found");

                request.Status = CustomRequestStatus.Cancelled;
                if (request.ProjectWorkspace != null)
                {
                    request.ProjectWorkspace.Status = ProjectWorkspaceStatus.Refunded;
                    request.ProjectWorkspace.PaymentStatus = PaymentStatus.Refunded;
                }

                await requestsRepo.UpdateAsync(request);
                await _uow.SaveChangesAsync();
                
                await LogActivityAsync(requestId, "Request Cancelled", $"Admin cancelled custom request. Reason: {reason}", request.BuyerId, request.SelectedSeller?.OwnerId, ct);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Cancellation failed: {ex.Message}");
            }
        }

        public async Task<Result> ArchiveRequestAsync(Guid requestId, CancellationToken ct = default)
        {
            try
            {
                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var request = await requestsRepo.GetByIdAsync(requestId);
                if (request == null) return Result.Failure("Request not found");

                request.IsDeleted = true;
                request.DeletedAt = DateTime.UtcNow;
                
                await requestsRepo.UpdateAsync(request);
                await _uow.SaveChangesAsync();
                
                await LogActivityAsync(requestId, "Request Archived", "Admin archived custom request.", request.BuyerId, null, ct);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Archiving failed: {ex.Message}");
            }
        }

        // ─── 3. AI Generations ───────────────────────────────────────────────────
        public async Task<Result<PagedList<AdminAiGenerationDto>>> GetAiGenerationsAsync(
            string? provider, int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var designsRepo = _uow.Repository<GeneratedDesign, Guid>();
                var designsQuery = await designsRepo.GetAllAsNoTracking();

                var query = designsQuery.Include(d => d.CustomRequest).AsQueryable();

                if (!string.IsNullOrWhiteSpace(provider))
                {
                    query = query.Where(d => d.Provider == provider);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d => d.Prompt.Contains(search) || d.CustomRequestId.ToString().Contains(search));
                }

                var count = await query.CountAsync(ct);
                var rawItems = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = rawItems.Select(d => new AdminAiGenerationDto
                {
                    DesignId = d.Id,
                    RequestId = d.CustomRequestId,
                    Provider = d.Provider,
                    GenerationTimeSeconds = d.GenerationTimeMs / 1000.0,
                    PromptLength = d.Prompt != null ? d.Prompt.Length : 0,
                    GenerationStatus = d.IsSelected ? "Selected" : "Not Selected",
                    MatchingScore = d.MatchingScore,
                    GenerationAttempts = d.CustomRequest != null ? d.CustomRequest.GenerationCount : 1,
                    GeneratedImageUrl = d.ImageUrl,
                    CreatedAt = d.CreatedAt
                }).ToList();

                return Result<PagedList<AdminAiGenerationDto>>.Success(new PagedList<AdminAiGenerationDto>(items, count, pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                return Result<PagedList<AdminAiGenerationDto>>.Failure($"Failed to fetch AI generations: {ex.Message}");
            }
        }

        // ─── 4. Artisan Analytics ─────────────────────────────────────────────────
        public async Task<Result<List<AdminArtisanDto>>> GetArtisanAnalyticsAsync(CancellationToken ct = default)
        {
            try
            {
                var shopsRepo = _uow.Repository<Shop, Guid>();
                var shopsQuery = await shopsRepo.GetAllAsNoTracking();

                var shops = await shopsQuery
                    .Include(s => s.Owner)
                    .Where(s => !s.IsDeleted)
                    .ToListAsync(ct);

                var list = new List<AdminArtisanDto>();
                
                var offersRepo = _uow.Repository<CustomOffer, Guid>();
                var offersQuery = await offersRepo.GetAllAsNoTracking();
                
                var recommendationsRepo = _uow.Repository<SellerRecommendation, Guid>();
                var recommendationsQuery = await recommendationsRepo.GetAllAsNoTracking();
                
                var projectsRepo = _uow.Repository<ProjectWorkspace, Guid>();
                var projectsQuery = await projectsRepo.GetAllAsNoTracking();

                foreach (var shop in shops)
                {
                    var offers = await offersQuery
                        .Where(o => o.ShopId == shop.Id)
                        .ToListAsync(ct);

                    var matchedRequestsCount = await recommendationsQuery
                        .CountAsync(sr => sr.ShopId == shop.Id, ct);

                    var acceptedCount = offers.Count(o => o.Status == OfferStatus.Accepted);
                    var totalOffersCount = offers.Count;
                    double acceptanceRate = totalOffersCount > 0 ? (double)acceptedCount / totalOffersCount * 100.0 : 0.0;

                    var workspaces = await projectsQuery
                        .Include(w => w.SelectedOffer)
                        .Where(w => w.SelectedOffer.ShopId == shop.Id)
                        .ToListAsync(ct);

                    var completedCount = workspaces.Count(w => w.Status == ProjectWorkspaceStatus.Completed);
                    var totalProjects = workspaces.Count;
                    double completionRate = totalProjects > 0 ? (double)completedCount / totalProjects * 100.0 : 0.0;

                    double avgDeliveryTime = completedCount > 0
                        ? workspaces.Where(w => w.Status == ProjectWorkspaceStatus.Completed)
                                    .Average(w => ((w.UpdatedAt ?? DateTime.UtcNow) - w.CreatedAt).TotalDays)
                        : 0.0;

                    var totalRevenue = workspaces.Where(w => w.PaymentStatus == PaymentStatus.Paid || w.Status == ProjectWorkspaceStatus.Completed)
                                                 .Sum(w => w.SelectedOffer.Price);

                    list.Add(new AdminArtisanDto
                    {
                        ShopId = shop.Id,
                        ShopName = shop.Name,
                        OwnerName = shop.Owner != null ? shop.Owner.Name : "Seller",
                        RequestsMatchedCount = matchedRequestsCount,
                        OfferAcceptanceRate = Math.Round(acceptanceRate, 1),
                        ProjectCompletionRate = Math.Round(completionRate, 1),
                        AverageRating = shop.Rating,
                        AverageDeliveryTimeDays = Math.Round(avgDeliveryTime, 1),
                        TotalCustomRevenue = totalRevenue,
                        CompletedProjectsCount = completedCount
                    });
                }

                return Result<List<AdminArtisanDto>>.Success(list.OrderByDescending(l => l.TotalCustomRevenue).ToList());
            }
            catch (Exception ex)
            {
                return Result<List<AdminArtisanDto>>.Failure($"Failed to compute artisan metrics: {ex.Message}");
            }
        }

        // ─── 5. Offers ───────────────────────────────────────────────────────────
        public async Task<Result<PagedList<AdminOfferDto>>> GetOffersAsync(
            int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var offersRepo = _uow.Repository<CustomOffer, Guid>();
                var offersQuery = await offersRepo.GetAllAsNoTracking();

                var query = offersQuery
                    .Include(o => o.Shop)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(o => (int)o.Status == status.Value);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(o => o.Shop.Name.Contains(search) || o.CustomRequestId.ToString().Contains(search));
                }

                var count = await query.CountAsync(ct);
                var rawItems = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = rawItems.Select(o => new AdminOfferDto
                {
                    OfferId = o.Id,
                    RequestId = o.CustomRequestId,
                    ShopName = o.Shop.Name,
                    Price = o.Price,
                    DeliveryTimeDays = o.DeliveryTimeDays,
                    RevisionsAllowed = o.RevisionsAllowed,
                    Status = (int)o.Status,
                    StatusName = o.Status.ToString(),
                    CreatedAt = o.CreatedAt
                }).ToList();

                return Result<PagedList<AdminOfferDto>>.Success(new PagedList<AdminOfferDto>(items, count, pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                return Result<PagedList<AdminOfferDto>>.Failure($"Failed to fetch custom offers: {ex.Message}");
            }
        }

        public async Task<Result<AdminOfferMetricsDto>> GetOfferMetricsAsync(CancellationToken ct = default)
        {
            try
            {
                var offersRepo = _uow.Repository<CustomOffer, Guid>();
                var offersQuery = await offersRepo.GetAllAsNoTracking();

                var totalOffers = await offersQuery.ToListAsync(ct);
                var pending = totalOffers.Count(o => o.Status == OfferStatus.Pending);
                var accepted = totalOffers.Count(o => o.Status == OfferStatus.Accepted);
                var rejected = totalOffers.Count(o => o.Status == OfferStatus.Rejected);
                var expired = totalOffers.Count(o => o.Status == OfferStatus.Withdrawn);

                var avgPrice = totalOffers.Any() ? totalOffers.Average(o => o.Price) : 0m;

                var projectsRepo = _uow.Repository<ProjectWorkspace, Guid>();
                var projectsQuery = await projectsRepo.GetAllAsNoTracking();

                var acceptedWorkspaces = await projectsQuery
                    .Include(w => w.SelectedOffer).ThenInclude(o => o.CustomRequest)
                    .ToListAsync(ct);

                double avgNegTimeHours = acceptedWorkspaces.Any()
                    ? acceptedWorkspaces.Average(w => (w.CreatedAt - w.SelectedOffer.CustomRequest.CreatedAt).TotalHours)
                    : 0.0;

                var metrics = new AdminOfferMetricsDto
                {
                    PendingOffers = pending,
                    AcceptedOffers = accepted,
                    RejectedOffers = rejected,
                    ExpiredOffers = expired,
                    AvgOfferPrice = Math.Round(avgPrice, 2),
                    AvgNegotiationTimeHours = Math.Round(avgNegTimeHours, 1)
                };

                return Result<AdminOfferMetricsDto>.Success(metrics);
            }
            catch (Exception ex)
            {
                return Result<AdminOfferMetricsDto>.Failure($"Failed to get offer metrics: {ex.Message}");
            }
        }

        // ─── 6. Projects (Workspaces) ────────────────────────────────────────────
        public async Task<Result<PagedList<AdminProjectDto>>> GetProjectsAsync(
            int? status, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var projectsRepo = _uow.Repository<ProjectWorkspace, Guid>();
                var projectsQuery = await projectsRepo.GetAllAsNoTracking();

                var query = projectsQuery
                    .Include(w => w.CustomRequest).ThenInclude(r => r.Buyer)
                    .Include(w => w.SelectedOffer).ThenInclude(o => o.Shop)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(w => (int)w.Status == status.Value);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(w => w.CustomRequest.Buyer.Name.Contains(search) || 
                                             w.SelectedOffer.Shop.Name.Contains(search));
                }

                var count = await query.CountAsync(ct);
                var rawItems = await query
                    .OrderByDescending(w => w.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = rawItems.Select(w => new AdminProjectDto
                {
                    WorkspaceId = w.Id,
                    RequestId = w.CustomRequestId,
                    Status = (int)w.Status,
                    StatusName = w.Status.ToString(),
                    MilestoneStep = w.MilestoneStep,
                    MilestoneName = w.MilestoneStep switch
                    {
                        0 => "Not Started",
                        1 => "Material Selection",
                        2 => "Crochet Body",
                        3 => "Hair & Face details",
                        4 => "Outfit & Details",
                        5 => "Final Assembly",
                        6 => "Shipped",
                        7 => "Completed",
                        _ => "Processing"
                    },
                    PaymentStatus = w.PaymentStatus.ToString(),
                    SellerName = w.SelectedOffer.Shop.Name,
                    BuyerName = w.CustomRequest.Buyer.Name,
                    EstimatedDeliveryDate = w.CreatedAt.AddDays(w.SelectedOffer.DeliveryTimeDays),
                    CompletionPercentage = w.MilestoneStep * 14.28,
                    ProgressPhotosCount = string.IsNullOrEmpty(w.FinalPhotoUrl) ? 0 : 1,
                    CreatedAt = w.CreatedAt
                }).ToList();

                return Result<PagedList<AdminProjectDto>>.Success(new PagedList<AdminProjectDto>(items, count, pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                return Result<PagedList<AdminProjectDto>>.Failure($"Failed to fetch projects: {ex.Message}");
            }
        }

        // ─── 7. Audit Log ────────────────────────────────────────────────────────
        public async Task<Result<PagedList<CustomStudioAuditLog>>> GetAuditLogsAsync(
            string? eventName, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var auditLogsRepo = _uow.Repository<CustomStudioAuditLog, Guid>();
                var auditLogsQuery = await auditLogsRepo.GetAllAsNoTracking();

                var query = auditLogsQuery.AsQueryable();

                if (!string.IsNullOrWhiteSpace(eventName))
                {
                    query = query.Where(l => l.EventName == eventName);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(l => l.Description.Contains(search) || l.RequestId.ToString().Contains(search));
                }

                var count = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return Result<PagedList<CustomStudioAuditLog>>.Success(new PagedList<CustomStudioAuditLog>(items, count, pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                return Result<PagedList<CustomStudioAuditLog>>.Failure($"Failed to fetch custom studio audit logs: {ex.Message}");
            }
        }

        public async Task LogActivityAsync(
            Guid? requestId, string eventName, string description, string? buyerId = null, string? sellerId = null, CancellationToken ct = default)
        {
            try
            {
                var auditLogsRepo = _uow.Repository<CustomStudioAuditLog, Guid>();
                var log = new CustomStudioAuditLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = requestId,
                    EventName = eventName,
                    Description = description,
                    BuyerId = buyerId,
                    SellerId = sellerId,
                    Timestamp = DateTime.UtcNow
                };

                await auditLogsRepo.AddAsync(log);
                await _uow.SaveChangesAsync();
            }
            catch
            {
                // Silent catch to prevent logging failure from blocking business flow
            }
        }

        // ─── 8. Settings ─────────────────────────────────────────────────────────
        public async Task<Result<CustomStudioSetting>> GetSettingsAsync(CancellationToken ct = default)
        {
            try
            {
                var settingsRepo = _uow.Repository<CustomStudioSetting, Guid>();
                var settingsQuery = await settingsRepo.GetAllAsync();

                var settings = await settingsQuery.FirstOrDefaultAsync(ct);
                if (settings == null)
                {
                    settings = new CustomStudioSetting
                    {
                        Id = Guid.NewGuid(),
                        MaxAiGenerations = 2,
                        MaxReferenceImageSizeMb = 5,
                        AllowedImageTypes = ".jpg,.jpeg,.png",
                        DefaultDeliveryTimeDays = 14,
                        DefaultRevisionCount = 3,
                        ActiveAiProvider = "GoogleAIStudio",
                        PromptBuilderInstructions = "A premium, high-quality, professional studio photo of a handmade amigurumi crochet doll.",
                        IsFeatureEnabled = true
                    };
                    await settingsRepo.AddAsync(settings);
                    await _uow.SaveChangesAsync();
                }
                return Result<CustomStudioSetting>.Success(settings);
            }
            catch (Exception ex)
            {
                return Result<CustomStudioSetting>.Failure($"Failed to retrieve settings: {ex.Message}");
            }
        }

        public async Task<Result<CustomStudioSetting>> UpdateSettingsAsync(CustomStudioSetting settings, CancellationToken ct = default)
        {
            try
            {
                var settingsRepo = _uow.Repository<CustomStudioSetting, Guid>();
                var settingsQuery = await settingsRepo.GetAllAsync();

                var dbSettings = await settingsQuery.FirstOrDefaultAsync(ct);
                if (dbSettings == null)
                {
                    settings.Id = Guid.NewGuid();
                    await settingsRepo.AddAsync(settings);
                }
                else
                {
                    dbSettings.MaxAiGenerations = settings.MaxAiGenerations;
                    dbSettings.MaxReferenceImageSizeMb = settings.MaxReferenceImageSizeMb;
                    dbSettings.AllowedImageTypes = settings.AllowedImageTypes;
                    dbSettings.DefaultDeliveryTimeDays = settings.DefaultDeliveryTimeDays;
                    dbSettings.DefaultRevisionCount = settings.DefaultRevisionCount;
                    dbSettings.ActiveAiProvider = settings.ActiveAiProvider;
                    dbSettings.PromptBuilderInstructions = settings.PromptBuilderInstructions;
                    dbSettings.IsFeatureEnabled = settings.IsFeatureEnabled;
                    dbSettings.UpdatedAt = DateTime.UtcNow;
                    await settingsRepo.UpdateAsync(dbSettings);
                }

                await _uow.SaveChangesAsync();
                await LogActivityAsync(null, "Settings Updated", "Admin updated Custom Studio configuration settings.", null, null, ct);
                
                return Result<CustomStudioSetting>.Success(dbSettings ?? settings);
            }
            catch (Exception ex)
            {
                return Result<CustomStudioSetting>.Failure($"Failed to update settings: {ex.Message}");
            }
        }

        // ─── 9. Exports ──────────────────────────────────────────────────────────
        public async Task<Result<string>> ExportRequestsToCsvAsync(CancellationToken ct = default)
        {
            try
            {
                var requestsRepo = _uow.Repository<CustomRequest, Guid>();
                var requestsQuery = await requestsRepo.GetAllAsNoTracking();

                var requests = await requestsQuery
                    .Include(r => r.Buyer)
                    .Include(r => r.SelectedSeller)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(ct);

                var csv = new StringBuilder();
                csv.AppendLine("RequestID,BuyerName,BuyerEmail,SellerName,Status,TargetBudget,CreatedDate");

                foreach (var r in requests)
                {
                    csv.AppendLine($"\"{r.Id}\",\"{r.Buyer.Name}\",\"{r.Buyer.Email}\",\"{r.SelectedSeller?.Name ?? "Unmatched"}\",\"{r.Status}\",\"{r.TargetBudget}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                return Result<string>.Success(csv.ToString());
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to export requests: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportOffersToCsvAsync(CancellationToken ct = default)
        {
            try
            {
                var offersRepo = _uow.Repository<CustomOffer, Guid>();
                var offersQuery = await offersRepo.GetAllAsNoTracking();

                var offers = await offersQuery
                    .Include(o => o.Shop)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(ct);

                var csv = new StringBuilder();
                csv.AppendLine("OfferID,RequestID,ShopName,Price,DeliveryDays,Revisions,Status,CreatedDate");

                foreach (var o in offers)
                {
                    csv.AppendLine($"\"{o.Id}\",\"{o.CustomRequestId}\",\"{o.Shop.Name}\",\"{o.Price}\",\"{o.DeliveryTimeDays}\",\"{o.RevisionsAllowed}\",\"{o.Status}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                return Result<string>.Success(csv.ToString());
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to export offers: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportProjectsToCsvAsync(CancellationToken ct = default)
        {
            try
            {
                var projectsRepo = _uow.Repository<ProjectWorkspace, Guid>();
                var projectsQuery = await projectsRepo.GetAllAsNoTracking();

                var projects = await projectsQuery
                    .Include(w => w.CustomRequest).ThenInclude(r => r.Buyer)
                    .Include(w => w.SelectedOffer).ThenInclude(o => o.Shop)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync(ct);

                var csv = new StringBuilder();
                csv.AppendLine("WorkspaceID,RequestID,BuyerName,SellerName,Price,Status,MilestoneStep,PaymentStatus,CreatedDate");

                foreach (var w in projects)
                {
                    csv.AppendLine($"\"{w.Id}\",\"{w.CustomRequestId}\",\"{w.CustomRequest.Buyer.Name}\",\"{w.SelectedOffer.Shop.Name}\",\"{w.SelectedOffer.Price}\",\"{w.Status}\",\"{w.MilestoneStep}\",\"{w.PaymentStatus}\",\"{w.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                return Result<string>.Success(csv.ToString());
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to export projects: {ex.Message}");
            }
        }
    }
}
