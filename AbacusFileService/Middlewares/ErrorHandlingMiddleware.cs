using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AbacusFileService.Exceptions;
using AbacusFileService.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AbacusFileService.Middlewares;

    /// <summary>
    /// Middleware for handling errors and returning appropriate HTTP responses.
    /// In case of an unhandled exception, it logs the error with a correlation ID and returns a JSON response with the error details.
    /// </summary>
    /// <param name="next">RequestDelegate</param>
    /// <param name="logger">Default logging implementation</param>
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
                var correlationId = CorrelationIdProvider.GetOrCreate(context);
                logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}", 
                    correlationId, context.Request.Path);
            
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }
        
        /// <summary>
        /// Handles the exception and writes an appropriate response.
        /// </summary>
        /// <param name="context">HttpContext to write to</param>
        /// <param name="exception">The handled exception</param>
        /// <param name="correlationId">The correlationId for logging and tracing</param>
        /// <returns>No value returned, awaitable</returns>
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
