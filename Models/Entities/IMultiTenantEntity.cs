using System;

namespace SeasonsCare.Api.Models.Entities
{
    public interface IMultiTenantEntity
    {
        Guid CareGroupId { get; set; }
    }
}
