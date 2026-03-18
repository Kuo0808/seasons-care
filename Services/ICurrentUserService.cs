using System;

namespace SeasonsCare.Api.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}
