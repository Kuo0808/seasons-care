namespace SeasonsCare.Api.DTOs.CareGroups
{
    public class CreateCareGroupRequest
    {
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
    }
}
