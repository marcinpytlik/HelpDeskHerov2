using System.Diagnostics;
using System.Net;
using HelpDeskHero.Application.Common;
using HelpDeskHero.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
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
        catch (AppValidationException ex)
        {
            await WriteAsync(
                context,
                HttpStatusCode.BadRequest,
                new ApiErrorResponse
                {
                    Code = "validation_error",
                    Message = ex.Message,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                    ValidationErrors = ex.Errors
                });
        }
        catch (BusinessRuleException ex)
        {
            await WriteAsync(
                context,
                HttpStatusCode.BadRequest,
                new ApiErrorResponse
                {
                    Code = ex.Code,
                    Message = ex.Message,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteAsync(
                context,
                HttpStatusCode.Conflict,
                new ApiErrorResponse
                {
                    Code = "concurrency_conflict",
                    Message = "The record was modified by another user.",
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");

            await WriteAsync(
                context,
                HttpStatusCode.InternalServerError,
                new ApiErrorResponse
                {
                    Code = "internal_server_error",
                    Message = "Unexpected server error.",
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        ApiErrorResponse response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(response);
    }
}