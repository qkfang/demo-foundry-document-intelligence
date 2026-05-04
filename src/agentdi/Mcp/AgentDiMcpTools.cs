using System.ComponentModel;
using FxAgent.Services;
using ModelContextProtocol.Server;

namespace FxAgent.Mcp;

[McpServerToolType]
public class AgentDiMcpTools
{
    private readonly DocIntelligenceService _docIntelligence;
    private readonly ContentUnderstandingService _contentUnderstanding;
    private readonly NotificationService _notification;

    public AgentDiMcpTools(
        DocIntelligenceService docIntelligence,
        ContentUnderstandingService contentUnderstanding,
        NotificationService notification)
    {
        _docIntelligence = docIntelligence;
        _contentUnderstanding = contentUnderstanding;
        _notification = notification;
    }

    [McpServerTool(Name = "extractDoc_DI"),
     Description("Use Azure AI Document Intelligence (prebuilt-layout) to extract fields and information from a base64-encoded document.")]
    public async Task<string> ExtractDocDI(
        [Description("Base64-encoded binary content of the document to analyze")] string base64Content)
    {
        if (string.IsNullOrWhiteSpace(base64Content))
            return "Error: base64Content is required.";

        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64Content); }
        catch { return "Error: base64Content is not valid base64."; }

        return await _docIntelligence.ExtractTextFromBytesAsync(BinaryData.FromBytes(bytes));
    }

    [McpServerTool(Name = "extractDoc_CU"),
     Description("Use Azure AI Content Understanding to extract fields and information from a base64-encoded document.")]
    public async Task<string> ExtractDocCU(
        [Description("Base64-encoded binary content of the document to analyze")] string base64Content,
        [Description("Content Understanding analyzer id (e.g. prebuilt-documentSearch, prebuilt-invoice, prebuilt-receipt). Defaults to prebuilt-documentSearch.")]
        string analyzerId = "prebuilt-documentSearch")
    {
        if (string.IsNullOrWhiteSpace(base64Content))
            return "Error: base64Content is required.";

        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64Content); }
        catch { return "Error: base64Content is not valid base64."; }

        return await _contentUnderstanding.ExtractFieldsFromBytesAsync(BinaryData.FromBytes(bytes), analyzerId);
    }

    [McpServerTool(Name = "notification"),
     Description("Send a notification email to a recipient.")]
    public async Task<string> Notification(
        [Description("Recipient email address")] string to,
        [Description("Email subject")] string subject,
        [Description("Email body")] string body)
    {
        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(subject))
            return "Error: 'to' and 'subject' are required.";

        return await _notification.SendEmailAsync(to, subject, body ?? string.Empty);
    }
}
