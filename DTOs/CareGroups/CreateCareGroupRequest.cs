namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class CreateCareGroupRequest
    {
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientGender { get; set; }
        public DateOnly? RecipientBirthDate { get; set; }
    }
}
