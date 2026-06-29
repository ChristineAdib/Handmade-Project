using HandoraDomain.Models.CustomStudioEntities;
using System;

namespace HandoraDomain.Interfaces
{
    public interface ICustomOfferRepository : IGenericRepository<CustomOffer, Guid>
    {
    }
}
