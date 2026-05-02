using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FxAgent.Agents;
using FxAgent.Services;
using OpenTelemetry.Instrumentation.Http;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
    builder.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
    {
        options.FilterHttpRequestMessage = req =>
        {
            var host = req.RequestUri?.Host;
            if (string.IsNullOrEmpty(host)) return true;
            return !host.EndsWith("livediagnostics.monitor.azure.com", StringComparison.OrdinalIgnoreCase);
        };
    });
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/index.html"));

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

var endpoint = app.Configuration["AZURE_AI_PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName = app.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_AI_MODEL_DEPLOYMENT_NAME is not set.");
var docIntelligenceEndpoint = app.Configuration["AZURE_DOC_INTELLIGENCE_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_DOC_INTELLIGENCE_ENDPOINT is not set.");

var tenantId = app.Configuration["AZURE_TENANT_ID"];
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    TenantId = tenantId,
    ExcludeVisualStudioCodeCredential = true,
    ExcludeSharedTokenCacheCredential = true
});

var aiProjectClient = new AIProjectClient(new Uri(endpoint), credential);
var noticeAgent = new CtAgNotice(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgNotice>());
var docService = new DocIntelligenceService(docIntelligenceEndpoint, credential, loggerFactory.CreateLogger<DocIntelligenceService>());

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

await app.RunAsync();

static string Sanitize(string value) =>
    value.Replace('\r', ' ').Replace('\n', ' ');

record NoticeUrlRequest(string Url);
record NoticeTextRequest(string Text);
