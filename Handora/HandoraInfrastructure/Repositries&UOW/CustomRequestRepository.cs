using HandoraDomain.Interfaces;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraInfrastructure.Data;
using System;

namespace HandoraInfrastructure.Repositries
{
    public class CustomRequestRepository(AppDbContext context)
        : GenericRepository<CustomRequest, Guid>(context), ICustomRequestRepository
    {
    }
}
