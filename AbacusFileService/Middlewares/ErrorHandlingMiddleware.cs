using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AbacusFileService.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AbacusFileService.Middlewares;

    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}", 
                    correlationId, context.Request.Path);
            
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }
        
        private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            var statusCode = exception switch
            {
                FileNotFoundException => HttpStatusCode.NotFound,
                FileExistsException => HttpStatusCode.Conflict,
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                _ => HttpStatusCode.InternalServerError
            };

            var response = new
            {
                StatusCode = (int)statusCode,
                exception.Message,
                CorrelationId = correlationId
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
