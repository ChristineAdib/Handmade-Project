using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HandoraDomain.Consts;
using HandoraDomain.Models.CustomStudioEntities;

namespace HandoraApplication.Specifications
{
    public static class CustomRequestSpecifications
    {
        public static IQueryable<CustomRequest> WithDetails(this IQueryable<CustomRequest> query)
        {
            return query
                .Include(r => r.Buyer)
                .Include(r => r.SelectedSeller)
                .Include(r => r.SelectedDesign)
                .Include(r => r.CustomConfiguration)
                .Include(r => r.GeneratedDesigns)
                .Include(r => r.SellerRecommendations)
                    .ThenInclude(sr => sr.Shop)
                .Include(r => r.CustomOffers)
                    .ThenInclude(co => co.Shop)
                .Include(r => r.CustomService)
                .Include(r => r.ProjectWorkspace)
                    .ThenInclude(w => w.TimelineEntries);
        }

        public static IQueryable<CustomRequest> ByBuyer(this IQueryable<CustomRequest> query, string buyerId)
        {
            return query.Where(r => r.BuyerId == buyerId);
        }

        public static IQueryable<CustomRequest> BySellerShop(this IQueryable<CustomRequest> query, Guid shopId)
        {
            // E.g. request is matched or has offers from this seller shop
            return query.Where(r => r.SelectedSellerId == shopId || 
                                    r.CustomOffers.Any(o => o.ShopId == shopId) ||
                                    r.SellerRecommendations.Any(rec => rec.ShopId == shopId));
        }

        public static IQueryable<CustomRequest> Pending(this IQueryable<CustomRequest> query)
        {
            return query.Where(r => r.Status != CustomRequestStatus.Completed && 
                                    r.Status != CustomRequestStatus.Cancelled && 
                                    r.Status != CustomRequestStatus.Rejected);
        }
    }
}
