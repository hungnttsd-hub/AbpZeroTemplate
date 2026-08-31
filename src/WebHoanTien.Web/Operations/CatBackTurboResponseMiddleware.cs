using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebHoanTien.Web.Operations;

/// <summary>
/// Adapts regular Razor Page POST responses to the response contract expected by Turbo Drive.
/// Requests not initiated by CatBack's Turbo bootstrap are left untouched.
/// </summary>
public sealed class CatBackTurboResponseMiddleware
{
    public const string RequestHeaderName = "X-CatBack-Turbo";

    private readonly RequestDelegate _next;

    public CatBackTurboResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsTurboRequest(context.Request))
        {
            var isUnsafeRequest = IsUnsafeRequest(context.Request);
            context.Response.OnStarting(() =>
            {
                var response = context.Response;

                if (IsHtmlResponse(response.ContentType))
                {
                    response.Headers.CacheControl = "no-store, no-cache";
                    response.Headers.Pragma = "no-cache";
                }

                if (isUnsafeRequest &&
                    (response.StatusCode == StatusCodes.Status301MovedPermanently ||
                     response.StatusCode == StatusCodes.Status302Found) &&
                    response.Headers.ContainsKey("Location"))
                {
                    response.StatusCode = StatusCodes.Status303SeeOther;
                }
                else if (isUnsafeRequest &&
                         response.StatusCode == StatusCodes.Status200OK &&
                         IsHtmlResponse(response.ContentType))
                {
                    // A rendered HTML response after POST represents validation/business errors.
                    response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                }

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private static bool IsTurboRequest(HttpRequest request) =>
        string.Equals(request.Headers[RequestHeaderName], "1", StringComparison.Ordinal);

    private static bool IsUnsafeRequest(HttpRequest request) =>
        !HttpMethods.IsGet(request.Method) &&
        !HttpMethods.IsHead(request.Method) &&
        !HttpMethods.IsOptions(request.Method);

    private static bool IsHtmlResponse(string? contentType) =>
        contentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
}
