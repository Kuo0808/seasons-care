using System;

namespace SeasonsCare.Api.DTOs.Auth
{
    public class UpdateLastViewedCareGroupRequest
    {
        /// <summary>
        /// 必填。最後查看的照護群組 ID。
        /// </summary>
        public Guid CareGroupId { get; set; }
    }
}
