using FxAgent.Agents;
using FxAgent.Services;

namespace FxAgent.Api;

record NoticeUrlRequest(string Url);
record NoticeTextRequest(string Text);
record JsonRequest(string Json);

public static class Endpoints
{
    public static void MapAllEndpoints(this WebApplication app, CtAgNotice noticeAgent, CtAgTracker trackerAgent,
        CtAgNotification notificationAgent, CtAgReporting reportingAgent, CtAgQuality qualityAgent,
        CtAgCorrespondence correspondenceAgent, CtAgReview reviewAgent, DocIntelligenceService docService, ILogger logger)
    {
        app.MapPost("/notice/url", async (NoticeUrlRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return Results.BadRequest(new { error = "url is required" });

            logger.LogInformation("Notice URL request: {Url}", Sanitize(request.Url));
            var text = await docService.ExtractTextFromUrlAsync(new Uri(request.Url));
            var response = await noticeAgent.RunAsync(text);
            return Results.Ok(new { extractedText = text, response });
        });

        app.MapPost("/notice/upload", async (HttpRequest http) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { error = "multipart/form-data required" });

            var form = await http.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "file is required" });

            logger.LogInformation("Notice upload: {FileName} ({Size} bytes)", Sanitize(file.FileName), file.Length);
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var text = await docService.ExtractTextFromBytesAsync(BinaryData.FromBytes(ms.ToArray()));
            var response = await noticeAgent.RunAsync(text);
            return Results.Ok(new { extractedText = text, response });
        });

        app.MapPost("/notice/text", async (NoticeTextRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new { error = "text is required" });

            logger.LogInformation("Notice text request ({Length} chars)", request.Text.Length);
            var response = await noticeAgent.RunAsync(request.Text);
            return Results.Ok(new { response });
        });

        app.MapPost("/tracker/notices", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Tracker request ({Length} chars)", request.Json.Length);
            var response = await trackerAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/notification/generate", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Notification request ({Length} chars)", request.Json.Length);
            var response = await notificationAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/reporting/analyze", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Reporting request ({Length} chars)", request.Json.Length);
            var response = await reportingAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/quality/check", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Quality check request ({Length} chars)", request.Json.Length);
            var response = await qualityAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/correspondence/draft", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Correspondence request ({Length} chars)", request.Json.Length);
            var response = await correspondenceAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/review/corrections", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Review corrections request ({Length} chars)", request.Json.Length);
            var response = await reviewAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
