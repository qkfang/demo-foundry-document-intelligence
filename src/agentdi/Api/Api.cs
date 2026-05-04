using FxAgent.Agents;
using FxAgent.Services;

namespace FxAgent.Api;

record NoticeUrlRequest(string Url);
record NoticeTextRequest(string Text);
record JsonRequest(string Json);
record ApproveRequest(string RunId, bool Approved);
record Base64Request(string Base64Content);

public static class Endpoints
{
    public static void MapAllEndpoints(this WebApplication app,
        CtAgNotification notificationAgent, CtAgQuality qualityAgent,
        CtAgCorrespondence correspondenceAgent,
        CtAgExtractDI extractDiAgent, CtAgExtractCU extractCuAgent,
        DocIntelligenceService docService, ContentUnderstandingService cuService,
        PendingApprovalStore approvalStore, ILogger logger)
    {
        app.MapPost("/extract/di/upload", async (Base64Request request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Base64Content))
                return Results.BadRequest(new { error = "base64Content is required" });

            byte[] bytes;
            try { bytes = Convert.FromBase64String(request.Base64Content); }
            catch { return Results.BadRequest(new { error = "base64Content is not valid base64" }); }

            logger.LogInformation("Extract DI upload: {Size} bytes", bytes.Length);
            var text = await docService.ExtractTextFromBytesAsync(BinaryData.FromBytes(bytes));
            var response = await extractDiAgent.RunAsync(text);
            return Results.Ok(new { extractedText = text, response });
        });

        app.MapPost("/extract/cu/upload", async (Base64Request request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Base64Content))
                return Results.BadRequest(new { error = "base64Content is required" });

            byte[] bytes;
            try { bytes = Convert.FromBase64String(request.Base64Content); }
            catch { return Results.BadRequest(new { error = "base64Content is not valid base64" }); }

            logger.LogInformation("Extract CU upload: {Size} bytes", bytes.Length);
            var text = await cuService.ExtractFieldsFromBytesAsync(BinaryData.FromBytes(bytes));
            var response = await extractCuAgent.RunAsync(text);
            return Results.Ok(new { extractedText = text, response });
        });

        app.MapPost("/notification/generate", async (JsonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return Results.BadRequest(new { error = "json is required" });

            logger.LogInformation("Notification request ({Length} chars)", request.Json.Length);
            var response = await notificationAgent.RunAsync(request.Json);
            return Results.Ok(new { response });
        });

        app.MapPost("/notification/upload", async (HttpRequest http) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { error = "multipart/form-data required" });

            var form = await http.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "file is required" });

            logger.LogInformation("Notification upload: {FileName} ({Size} bytes)", Sanitize(file.FileName), file.Length);
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var extractedText = await docService.ExtractTextFromBytesAsync(BinaryData.FromBytes(ms.ToArray()));
            var response = await notificationAgent.RunAsync(extractedText);
            return Results.Ok(new { extractedText, response });
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
            var (result, pending) = await correspondenceAgent.StartRunAsync(request.Json);

            if (pending is not null)
            {
                var runId = approvalStore.Add(pending.ResponseId, pending.ApprovalItemId, pending.ServerLabel);
                return Results.Ok(new { status = "pending_approval", runId, toolCall = new { serverLabel = pending.ServerLabel } });
            }

            return Results.Ok(new { status = "complete", response = result });
        });

        app.MapPost("/correspondence/approve", async (ApproveRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.RunId))
                return Results.BadRequest(new { error = "runId is required" });

            var state = approvalStore.Get(request.RunId);
            if (state is null)
                return Results.NotFound(new { error = "run not found or already completed" });

            approvalStore.Remove(request.RunId);
            logger.LogInformation("Correspondence approval: runId={RunId} approved={Approved}", Sanitize(request.RunId), request.Approved);

            var (result, pending) = await correspondenceAgent.ContinueRunAsync(state.PreviousResponseId, state.ApprovalItemId, request.Approved);

            if (pending is not null)
            {
                var nextRunId = approvalStore.Add(pending.ResponseId, pending.ApprovalItemId, pending.ServerLabel);
                return Results.Ok(new { status = "pending_approval", runId = nextRunId, toolCall = new { serverLabel = pending.ServerLabel } });
            }

            return Results.Ok(new { status = "complete", response = result });
        });

    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
