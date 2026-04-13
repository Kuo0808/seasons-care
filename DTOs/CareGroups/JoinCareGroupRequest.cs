namespace SeasonsCare.Api.DTOs.CareGroups
{
    /// <summary>
    /// Request body for joining a care group.
    /// </summary>
    public class JoinCareGroupRequest
    {
        /// <summary>
        /// Invite code used to join a care group.
        /// </summary>
        public string? InviteCode { get; set; }
    }
}
