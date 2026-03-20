using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SeasonsCare.Api.Data;

namespace SeasonsCare.Api.Middleware
{
    public class CareGroupContextMiddleware
    {
        private readonly RequestDelegate _next;

        public CareGroupContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            dbContext.CurrentCareGroupId = null;

            if (context.Request.RouteValues.TryGetValue("careGroupId", out var routeValue) &&
                routeValue is not null &&
                Guid.TryParse(routeValue.ToString(), out var careGroupId))
            {
                dbContext.CurrentCareGroupId = careGroupId;
            }

            await _next(context);
        }
    }
}
