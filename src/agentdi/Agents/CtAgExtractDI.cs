using Azure.AI.Projects;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace FxAgent.Agents;

public class CtAgExtractDI : BaseAgent
{
    public CtAgExtractDI(AIProjectClient aiProjectClient, string deploymentName, IList<ResponseTool>? tools = null, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-extract-di", deploymentName, GetInstructions(), tools, logger)
    {
    }

    private static string GetInstructions() => """
        You are a document extraction agent. You receive a base64-encoded document.

        Step 1: Call the extractDoc_DI tool with the base64 content to extract the raw text from the document.
        Step 2: From the extracted text, identify and extract exactly these fields and return them as a single JSON object. Do not include any text outside the JSON.

        Fields:
        - entityName: Name of the entity (taxpayer, business, or individual) the notice is addressed to.
        - jurisdiction: The authority sending the notice (e.g. "IRS", "New York Department of Taxation").
        - noticeDate: Date the notice was issued, in YYYY-MM-DD format if possible.
        - dueDate: Due date for response or payment, in YYYY-MM-DD format if possible. Null if none.
        - amountDue: Numeric amount of payment due (no currency symbols). Null if none.
        - taxType: One of "income", "sales", "payroll", "property", "franchise", "other". Use lowercase.

        If a field cannot be determined, use null. Always return valid JSON.
        """;
}
