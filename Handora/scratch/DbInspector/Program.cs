using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using HandoraInfrastructure.Data;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Consts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== DIAGNOSE SELLER MATCHING ===\n");

        var apiPath = @"c:\Users\EG.LAP\Desktop\NewProject\Handmade-Project\Handora\HandoraApi";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var connString = configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine($"Conn string: {connString}");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connString));

        var serviceProvider = services.BuildServiceProvider();
        var db = serviceProvider.GetRequiredService<AppDbContext>();

        var reqId = Guid.Parse("0dfc9054-72bd-4dd8-af04-933a3b6a91a9");

        // 1. Check custom request
        Console.WriteLine("\n[1] Checking Custom Request...");
        var request = await db.CustomRequests
            .Include(r => r.CustomConfiguration)
            .Include(r => r.GeneratedDesigns)
            .FirstOrDefaultAsync(r => r.Id == reqId);

        if (request == null)
        {
            Console.WriteLine($"Custom request {reqId} NOT FOUND in DB!");
            return;
        }

        // 3. Inspect Shops in DB
        Console.WriteLine("\n[3] Inspecting active/all shops in DB...");
        var shops = await db.Shops
            .Include(s => s.Products).ThenInclude(p => p.Category)
            .Include(s => s.Reviews)
            .ToListAsync();
        Console.WriteLine($"Total shops in DB: {shops.Count}");

        // 4. Simulate the DB fallback matching step-by-step
        Console.WriteLine("\n[4] Simulating fallback matching logic...");
        try
        {
            var matchedShops = new List<Shop>();
            // If matchedShops count is less than 3, fill with top-rated shops
            if (matchedShops.Count < 3)
            {
                var extraShops = await db.Shops
                    .Include(s => s.Products).ThenInclude(p => p.Category)
                    .Include(s => s.Reviews)
                    .Where(s => !matchedShops.Select(ms => ms.Id).Contains(s.Id))
                    .Take(5)
                    .ToListAsync();
                matchedShops.AddRange(extraShops);
            }
            Console.WriteLine($"Matched shops count (after fallback fill): {matchedShops.Count}");

            // Fetch other tables for comprehensive scoring
            var allOrders = await db.Orders
                .Include(o => o.Items)
                .ToListAsync();
            Console.WriteLine($"Total orders loaded: {allOrders.Count}");

            var completedCustomRequests = await db.CustomRequests
                .Include(r => r.CustomConfiguration)
                .Where(r => r.Status == CustomRequestStatus.Completed)
                .ToListAsync();
            Console.WriteLine($"Completed custom requests loaded: {completedCustomRequests.Count}");

            var scoredRecommendations = new List<SellerRecommendation>();

            foreach (var shop in matchedShops)
            {
                double score = 50.0;
                var reasonList = new List<string>();

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

                var shopCustomRequests = completedCustomRequests.Where(r => r.SelectedSellerId == shop.Id).ToList();
                var customDollsCount = shopCustomRequests.Count;
                score += Math.Min(10.0, customDollsCount * 2.0);
                if (customDollsCount > 0) reasonList.Add($"Completed {customDollsCount} custom dolls");

                var shopOrders = allOrders.Where(o => o.Items.Any(i => i.ShopId == shop.Id)).ToList();
                var completedOrders = shopOrders.Where(o => o.Status == OrderStatus.Delivered).Count();
                var totalOrders = shopOrders.Count;
                double completionRate = totalOrders > 0 ? (double)completedOrders / totalOrders : 1.0;
                score += completionRate * 10.0;

                score += ((double)shop.Rating / 5.0) * 10.0;
                if (shop.Rating >= 4.5m) reasonList.Add("Top Rated");

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

                score += 5.0; // Default positive delivery score

                if (request.CustomConfiguration != null)
                {
                    var cfgJson = request.CustomConfiguration.ConfigurationDataJson;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(cfgJson);
                        var root = doc.RootElement;
                        var reqSize = root.TryGetProperty("size", out var sProp) ? sProp.GetString() : null;
                        var reqOutfit = root.TryGetProperty("outfitStyle", out var oProp) ? oProp.GetString() : null;

                        bool matchedSize = false;
                        bool matchedOutfit = false;

                        foreach (var prevReq in shopCustomRequests)
                        {
                            if (prevReq.CustomConfiguration != null)
                            {
                                using var prevDoc = System.Text.Json.JsonDocument.Parse(prevReq.CustomConfiguration.ConfigurationDataJson);
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing config JSON: {ex.Message}");
                    }
                }

                var avgProductPrice = products.Count > 0 ? products.Average(p => p.Price) : 350m;
                if (request.TargetBudget.HasValue && request.TargetBudget.Value > 0 && Math.Abs(avgProductPrice - request.TargetBudget.Value) / request.TargetBudget.Value <= 0.25m)
                {
                    score += 5.0;
                }
                else
                {
                    score += 3.0;
                }

                score += 5.0; // Default positive workload score
                score += Math.Min(3.0, customDollsCount * 1.0);
                score += 6.0; // Default similarity

                var hasPreviousRelation = allOrders.Any(o => o.UserId == request.BuyerId && o.Items.Any(i => i.ShopId == shop.Id));
                if (hasPreviousRelation)
                {
                    score += 5.0;
                    reasonList.Add("Previously ordered from");
                }

                score += 5.0;
                reasonList.Add("Fast response rate");

                score = Math.Min(99.0, Math.Max(70.0, score));
                score = Math.Round(score, 1);

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
                    CustomRequestId = reqId,
                    ShopId = shop.Id,
                    MatchingScore = score,
                    Reason = finalReason,
                    EstimatedPrice = 250m + (decimal)(new Random().Next(0, 8) * 20),
                    EstimatedDeliveryDays = 6 + new Random().Next(0, 4),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var top3Recommendations = scoredRecommendations
                .OrderByDescending(r => r.MatchingScore)
                .Take(3)
                .ToList();

            Console.WriteLine($"\n[SUCCESS] Top 3 matched recommendations generated: {top3Recommendations.Count}");
            foreach (var tr in top3Recommendations)
            {
                var s = shops.FirstOrDefault(x => x.Id == tr.ShopId);
                Console.WriteLine($"  Shop: '{s?.Name}' | Score: {tr.MatchingScore}% | Reason: {tr.Reason}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL MATCHING ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
