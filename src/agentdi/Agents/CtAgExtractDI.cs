using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgExtractDI : BaseAgent
{
    public CtAgExtractDI(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-extract-di", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a document extraction agent. You receive raw text extracted from a document by Azure Document Intelligence.

        Extract exactly these fields and return them as a single JSON object. Do not include any text outside the JSON.

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
