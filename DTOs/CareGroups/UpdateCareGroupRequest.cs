namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class UpdateCareGroupRequest
    {
        /// <summary>
        /// 必填。群組名稱，最長 100 字。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 必填。被照護者姓名，最長 100 字。
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// 必填。被照護者性別，最長 20 字。
        /// </summary>
        public string? RecipientGender { get; set; }

        /// <summary>
        /// 必填。被照護者生日。
        /// </summary>
        public DateOnly? RecipientBirthDate { get; set; }

        /// <summary>
        /// 選填。群組描述，最長 500 字。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 選填。健康狀況描述，最長 1000 字。
        /// </summary>
        public string? HealthStatus { get; set; }
    }
}
