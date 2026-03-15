using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SeasonsCare.Api.Exceptions;

namespace SeasonsCare.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            var response = new
            {
                type = "https://api.seasons-care.com/errors/internal-server-error",
                title = "Internal Server Error",
                status = 500,
                detail = "系統發生未預期的錯誤",
                errorCode = "INTERNAL_SERVER_ERROR",
                traceId = context.TraceIdentifier,
                errors = (IDictionary<string, string[]>?)null
            };

            if (exception is DomainException domainEx)
            {
                context.Response.StatusCode = domainEx.StatusCode;
                response = new
                {
                    type = $"https://api.seasons-care.com/errors/{domainEx.ErrorCode.ToLowerInvariant().Replace("_", "-")}",
                    title = GetTitleByStatusCode(domainEx.StatusCode),
                    status = domainEx.StatusCode,
                    detail = domainEx.Message,
                    errorCode = domainEx.ErrorCode,
                    traceId = context.TraceIdentifier,
                    errors = domainEx.Errors
                };
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(result);
        }

        private static string GetTitleByStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                500 => "Internal Server Error",
                _ => "Internal Server Error"
            };
        }
    }
}
