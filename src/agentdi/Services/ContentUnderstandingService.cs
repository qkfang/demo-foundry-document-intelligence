using System.Text.Json;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace FxAgent.Services;

public record ContentUnderstandingResult(string Markdown, string Json);

public class ContentUnderstandingService
{
    private readonly ContentUnderstandingClient _client;
    private readonly ILogger<ContentUnderstandingService> _logger;
    private readonly Dictionary<string, string> _modelDeployments;

    public ContentUnderstandingService(string endpoint, TokenCredential credential,
        string gpt41Deployment, string gpt41MiniDeployment, string embeddingDeployment,
        ILogger<ContentUnderstandingService> logger)
    {
        _client = new ContentUnderstandingClient(new Uri(endpoint), credential);
        _logger = logger;
        _modelDeployments = new Dictionary<string, string>
        {
            ["gpt-4.1"] = gpt41Deployment,
            ["gpt-4.1-mini"] = gpt41MiniDeployment,
            ["text-embedding-3-large"] = embeddingDeployment
        };
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Configuring CU model deployment defaults");
        await _client.UpdateDefaultsAsync(_modelDeployments);
    }

    public async Task<ContentUnderstandingResult> AnalyzeFromUrlAsync(Uri documentUrl, string analyzerId = "prebuilt-documentSearch")
    {
        _logger.LogInformation("CU analyzing URL with {Analyzer}: {Url}", Sanitize(analyzerId), Sanitize(documentUrl.ToString()));
        Operation<AnalysisResult> op = await _client.AnalyzeAsync(
            WaitUntil.Completed,
            analyzerId,
            inputs: new[] { new AnalysisInput { Uri = documentUrl } });

        return BuildResult(op);
    }

    public async Task<ContentUnderstandingResult> AnalyzeFromBytesAsync(BinaryData content, string analyzerId = "prebuilt-documentSearch")
    {
        _logger.LogInformation("CU analyzing {Bytes} bytes with {Analyzer}", content.ToMemory().Length, Sanitize(analyzerId));
        Operation<AnalysisResult> op = await _client.AnalyzeBinaryAsync(
            WaitUntil.Completed,
            analyzerId,
            content);

        return BuildResult(op);
    }

    private static ContentUnderstandingResult BuildResult(Operation<AnalysisResult> op)
    {
        var markdown = op.Value.Contents?.FirstOrDefault()?.Markdown ?? string.Empty;
        var rawJson = op.GetRawResponse().Content.ToString();
        return new ContentUnderstandingResult(markdown, PrettyJson(rawJson));
    }

    private static string PrettyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
