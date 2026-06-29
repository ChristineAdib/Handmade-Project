using HandoraDomain.Models.CustomStudioEntities;
using System;

namespace HandoraDomain.Interfaces
{
    public interface ICustomRequestRepository : IGenericRepository<CustomRequest, Guid>
    {
    }
}
