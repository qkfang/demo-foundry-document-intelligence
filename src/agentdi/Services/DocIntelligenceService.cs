using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace FxAgent.Services;

public class DocIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<DocIntelligenceService> _logger;

    public DocIntelligenceService(string endpoint, TokenCredential credential, ILogger<DocIntelligenceService> logger)
    {
        _client = new DocumentIntelligenceClient(new Uri(endpoint), credential);
        _logger = logger;
    }

    public async Task<string> ExtractTextFromUrlAsync(Uri documentUrl)
    {
        _logger.LogInformation("Analyzing document from URL: {Url}", Sanitize(documentUrl.ToString()));
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-layout",
            documentUrl);
        return operation.Value.Content ?? string.Empty;
    }

    public async Task<string> ExtractTextFromBytesAsync(BinaryData content)
    {
        _logger.LogInformation("Analyzing document from {Bytes} bytes", content.ToMemory().Length);
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-layout",
            content);
        return operation.Value.Content ?? string.Empty;
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
