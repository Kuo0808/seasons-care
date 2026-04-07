using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SeasonsCare.Api.Exceptions;

namespace SeasonsCare.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = 500;

            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}, Method: {Method}, Path: {Path}, QueryString: {QueryString}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.Value);

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
            else if (exception is DbUpdateConcurrencyException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response = new
                {
                    type = "https://api.seasons-care.com/errors/concurrency-conflict",
                    title = GetTitleByStatusCode(StatusCodes.Status409Conflict),
                    status = StatusCodes.Status409Conflict,
                    detail = "資料已被其他人更新，請重新整理後再試。",
                    errorCode = "CONCURRENCY_CONFLICT",
                    traceId = context.TraceIdentifier,
                    errors = (IDictionary<string, string[]>?)null
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
