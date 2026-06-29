using HandoraDomain.Interfaces;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraInfrastructure.Data;
using System;

namespace HandoraInfrastructure.Repositries
{
    public class CustomOfferRepository(AppDbContext context)
        : GenericRepository<CustomOffer, Guid>(context), ICustomOfferRepository
    {
    }
}
