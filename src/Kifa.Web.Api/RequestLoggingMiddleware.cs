using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NLog;

namespace Kifa.Web.Api;

public class RequestLoggingMiddleware {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    readonly RequestDelegate next;

    public RequestLoggingMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        var request = context.Request;
        var user = UserFilter.GetUser(request);
        var requestInfo = $"{request.Method} {request.Path}{request.QueryString}";

        var body = await ReadRequestBodyAsync(request);
        if (!string.IsNullOrEmpty(body)) {
            Logger.Info($"Handling {requestInfo} for user '{user}':\n{body}");
        } else {
            Logger.Info($"Handling {requestInfo} for user '{user}'...");
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            await next(context);
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error : statusCode >= 400 ? LogLevel.Warn : LogLevel.Info;
            Logger.Log(level,
                $"Finished {requestInfo} for user '{user}': {statusCode} ({stopwatch.ElapsedMilliseconds} ms)");
        } catch (Exception ex) {
            stopwatch.Stop();
            Logger.Error(ex,
                $"Failed {requestInfo} for user '{user}': Exception after {stopwatch.ElapsedMilliseconds} ms");
            throw;
        }
    }

    static async Task<string?> ReadRequestBodyAsync(HttpRequest request) {
        if (request.ContentLength is null or 0) {
            return null;
        }

        if (request.ContentType != null &&
            !request.ContentType.StartsWith("application/json") &&
            !request.ContentType.StartsWith("application/x-yaml") &&
            !request.ContentType.StartsWith("text/")) {
            return null;
        }

        if (request.ContentLength > 100 * 1024) {
            return null;
        }

        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}
