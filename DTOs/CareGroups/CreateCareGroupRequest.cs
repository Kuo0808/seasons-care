namespace SeasonsCare.Api.DTOs.CareGroups
{
    /// <summary>
    /// Request body for creating a care group.
    /// </summary>
    public class CreateCareGroupRequest
    {
        /// <summary>
        /// Care group display name. Maximum length is 100.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Care recipient name. Maximum length is 100.
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// Care recipient gender. Maximum length is 20.
        /// </summary>
        public string? RecipientGender { get; set; }

        /// <summary>
        /// Care recipient birth date.
        /// </summary>
        public DateOnly? RecipientBirthDate { get; set; }
    }
}
