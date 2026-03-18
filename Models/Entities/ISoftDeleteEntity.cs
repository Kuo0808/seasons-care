using System;

namespace SeasonsCare.Api.Models.Entities
{
    public interface ISoftDeleteEntity
    {
        DateTime? DeletedAt { get; set; }
    }
}
