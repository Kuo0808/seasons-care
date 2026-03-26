namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class UpdateCareGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientGender { get; set; }
        public DateOnly? RecipientBirthDate { get; set; }
        public string? Description { get; set; }
        public string? HealthStatus { get; set; }
    }
}
