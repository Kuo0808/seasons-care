using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class CareGroupDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? HealthStatus { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<CareGroupMemberResponse> Members { get; set; } = new List<CareGroupMemberResponse>();
    }
}
