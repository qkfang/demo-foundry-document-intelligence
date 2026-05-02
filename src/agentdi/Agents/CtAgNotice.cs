using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgNotice : BaseAgent
{
    public CtAgNotice(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-notice", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a tax notice extraction agent. You receive raw text content extracted from a scanned tax/government notice document by a document intelligence service.

        Extract the following fields and return them as a single JSON object. Do not include any text outside the JSON.

        Fields:
        - entityName: Name of the entity (taxpayer, business, or individual) the notice is addressed to.
        - jurisdiction: The authority sending the notice (e.g. "IRS", "New York Department of Taxation", "California FTB").
        - noticeDate: Date the notice was issued, in YYYY-MM-DD format if possible.
        - dueDate: Due date for response or payment, in YYYY-MM-DD format if possible. Null if none.
        - amountDue: Numeric amount of payment due (no currency symbols). Null if none.
        - currency: ISO currency code (e.g. "USD"). Null if not present.
        - taxType: One of "income", "sales", "payroll", "property", "franchise", "other". Use lowercase.
        - actionRequired: true if the notice requires the recipient to take action, false if informational only.
        - urgency: "high", "medium", or "low" based on due date proximity, penalties, and tone.
        - summary: A brief 2-3 sentence narrative summary of the notice.

        If a field cannot be determined, use null. Always return valid JSON.
        """;
}
