using HandoraDomain.Interfaces;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraInfrastructure.Data;
using System;

namespace HandoraInfrastructure.Repositries
{
    public class ProjectWorkspaceRepository(AppDbContext context)
        : GenericRepository<ProjectWorkspace, Guid>(context), IProjectWorkspaceRepository
    {
    }
}
