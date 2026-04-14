using System;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class CareGroupMemberResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvatarKey { get; set; } = string.Empty;
        public CareGroupRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
    }
}
