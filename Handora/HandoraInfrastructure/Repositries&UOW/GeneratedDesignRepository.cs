using HandoraDomain.Interfaces;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraInfrastructure.Data;
using System;

namespace HandoraInfrastructure.Repositries
{
    public class GeneratedDesignRepository(AppDbContext context)
        : GenericRepository<GeneratedDesign, Guid>(context), IGeneratedDesignRepository
    {
    }
}
