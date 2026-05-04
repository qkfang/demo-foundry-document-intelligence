using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FxAgent.Agents;
using FxAgent.Api;
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
var trackerAgent = new CtAgTracker(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgTracker>());
var notificationAgent = new CtAgNotification(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgNotification>());
var reportingAgent = new CtAgReporting(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgReporting>());
var qualityAgent = new CtAgQuality(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgQuality>());
var correspondenceAgent = new CtAgCorrespondence(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgCorrespondence>());
var reviewAgent = new CtAgReview(aiProjectClient, deploymentName, loggerFactory.CreateLogger<CtAgReview>());
var docService = new DocIntelligenceService(docIntelligenceEndpoint, credential, loggerFactory.CreateLogger<DocIntelligenceService>());

app.MapAllEndpoints(noticeAgent, trackerAgent, notificationAgent, reportingAgent, qualityAgent, correspondenceAgent, reviewAgent, docService, logger);

await app.RunAsync();

static string Sanitize(string value) =>
    value.Replace('\r', ' ').Replace('\n', ' ');
