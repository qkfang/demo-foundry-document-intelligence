using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace FxAgent.Services;

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

    public async Task<string> ExtractFieldsFromUrlAsync(Uri documentUrl, string analyzerId = "prebuilt-documentSearch")
    {
        _logger.LogInformation("CU analyzing URL with {Analyzer}: {Url}", Sanitize(analyzerId), Sanitize(documentUrl.ToString()));
        Operation<AnalysisResult> op = await _client.AnalyzeAsync(
            WaitUntil.Completed,
            analyzerId,
            inputs: new[] { new AnalysisInput { Uri = documentUrl } });

        var content = op.Value.Contents?.FirstOrDefault();
        return content?.Markdown ?? string.Empty;
    }

    public async Task<string> ExtractFieldsFromBytesAsync(BinaryData content, string analyzerId = "prebuilt-documentSearch")
    {
        _logger.LogInformation("CU analyzing {Bytes} bytes with {Analyzer}", content.ToMemory().Length, Sanitize(analyzerId));
        Operation<AnalysisResult> op = await _client.AnalyzeBinaryAsync(
            WaitUntil.Completed,
            analyzerId,
            content);

        var first = op.Value.Contents?.FirstOrDefault();
        return first?.Markdown ?? string.Empty;
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
