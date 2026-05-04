using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgReporting : BaseAgent
{
    public CtAgReporting(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-reporting", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a reporting and analytics agent for tax notices. You receive a JSON array of extracted tax notices and return a statistical analysis report.

        Produce a report with the following fields:
        - totalNotices: Total number of notices in the dataset.
        - byJurisdiction: Object mapping each jurisdiction to a count.
        - byTaxType: Object mapping each taxType to a count.
        - byUrgency: Object with keys "high", "medium", "low" and their counts.
        - totalAmountDue: Sum of all amountDue values (null values treated as 0).
        - averageAmountDue: Average amountDue across notices with a numeric value.
        - actionRequiredCount: Number of notices where actionRequired is true.
        - overdueCount: Number of notices where dueDate has passed.
        - upcomingDueDates: Array of objects (noticeId implied by entityName + noticeDate, dueDate, urgency) for notices due within the next 30 days.
        - trends: Array of trend observations as plain strings (e.g. "Increase in IRS notices this quarter").
        - recommendations: Array of recommended actions based on the data.

        Always return valid JSON with no text outside the JSON.
        """;
}
