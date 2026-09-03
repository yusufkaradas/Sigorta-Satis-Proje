using Kasko.Business.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Kasko.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Kaynak bulunamadı. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                HttpStatusCode.NotFound,
                "Not Found",
                ex.Message);
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Geçersiz istek. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                HttpStatusCode.BadRequest,
                "Bad Request",
                ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                HttpStatusCode.Conflict,
                "Conflict",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Beklenmeyen hata. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "Beklenmeyen bir hata oluştu.");
        } 
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            context.TraceIdentifier;

        context.Response.StatusCode = (int)statusCode;

        context.Response.ContentType =
            "application/problem+json; charset=utf-8";

        var json = JsonSerializer.Serialize(problemDetails);

        await context.Response.WriteAsync(json);
    }

}