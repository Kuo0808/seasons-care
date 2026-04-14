using System;

namespace SeasonsCare.Api.DTOs.CareGroups
{
    /// <summary>
    /// Summary response for a care group.
    /// </summary>
    public class CareGroupResponse
    {
        /// <summary>
        /// Care group id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Care group display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Care recipient name.
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// Care recipient gender.
        /// </summary>
        public string? RecipientGender { get; set; }

        /// <summary>
        /// Care recipient birth date.
        /// </summary>
        public DateOnly? RecipientBirthDate { get; set; }

        /// <summary>
        /// Optional care group description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional health status note.
        /// </summary>
        public string? HealthStatus { get; set; }

        /// <summary>
        /// Invite code for this care group.
        /// </summary>
        public string InviteCode { get; set; } = string.Empty;

        /// <summary>
        /// Care group creation time in UTC.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Current member count.
        /// </summary>
        public int MemberCount { get; set; }
    }
}
