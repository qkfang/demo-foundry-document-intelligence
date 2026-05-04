using Azure.AI.Projects;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FxAgent.Agents;
using FxAgent.Api;
using FxAgent.Mcp;
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

var endpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var docIntelligenceEndpoint = builder.Configuration["AZURE_DOC_INTELLIGENCE_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_DOC_INTELLIGENCE_ENDPOINT is not set.");
var foundryEndpoint = builder.Configuration["AZURE_AI_FOUNDRY_ENDPOINT"]
    ?? new Uri(endpoint).GetLeftPart(UriPartial.Authority);

var tenantId = builder.Configuration["AZURE_TENANT_ID"];
var credential = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
{
    TenantId = tenantId,
    ExcludeVisualStudioCodeCredential = true,
    ExcludeSharedTokenCacheCredential = true
});

builder.Services.AddSingleton(sp => new DocIntelligenceService(
    docIntelligenceEndpoint, credential, sp.GetRequiredService<ILogger<DocIntelligenceService>>()));
builder.Services.AddSingleton(sp => new ContentUnderstandingService(
    foundryEndpoint, credential, sp.GetRequiredService<ILogger<ContentUnderstandingService>>()));
builder.Services.AddSingleton<NotificationService>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => { options.Stateless = true; })
    .WithTools<AgentDiMcpTools>();

builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Normalize Accept header for MCP requests from Foundry agent server
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp"))
    {
        var accept = context.Request.Headers.Accept.ToString();
        if (string.IsNullOrEmpty(accept) || !accept.Contains("text/event-stream"))
        {
            context.Request.Headers.Accept = "application/json, text/event-stream";
        }
    }
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/index.html"));

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

var deploymentName = app.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_AI_MODEL_DEPLOYMENT_NAME is not set.");

var aiProjectClient = new AIProjectClient(new Uri(endpoint), credential);
var noticeAgent = new CtAgNotice(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgNotice>());
var trackerAgent = new CtAgTracker(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgTracker>());
var notificationAgent = new CtAgNotification(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgNotification>());
var reportingAgent = new CtAgReporting(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgReporting>());
var qualityAgent = new CtAgQuality(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgQuality>());
var correspondenceAgent = new CtAgCorrespondence(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgCorrespondence>());
var extractDiAgent = new CtAgExtractDI(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgExtractDI>());
var extractCuAgent = new CtAgExtractCU(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgExtractCU>());
var docService = app.Services.GetRequiredService<DocIntelligenceService>();

app.MapAllEndpoints(noticeAgent, trackerAgent, notificationAgent, reportingAgent, qualityAgent, correspondenceAgent, extractDiAgent, extractCuAgent, docService, logger);

await app.RunAsync();
