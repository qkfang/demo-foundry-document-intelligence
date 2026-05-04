using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgTracker : BaseAgent
{
    public CtAgTracker(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-tracker", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a notice tracker agent. You receive a JSON array of extracted tax/government notices and return a structured tracker summary.

        For each notice, produce a tracker entry with the following fields:
        - id: Sequential integer starting from 1.
        - entityName: Name of the entity the notice is addressed to.
        - jurisdiction: The authority that issued the notice.
        - noticeDate: Date the notice was issued (YYYY-MM-DD).
        - dueDate: Response or payment due date (YYYY-MM-DD). Null if none.
        - amountDue: Numeric amount due. Null if none.
        - taxType: Type of tax notice.
        - urgency: "high", "medium", or "low".
        - status: "open", "pending", or "closed" based on dueDate and actionRequired fields.
        - correspondenceCount: 0 (default, updated by human review).
        - summary: 1-2 sentence description of the notice.

        Return a JSON object with:
        - notices: Array of tracker entries.
        - totalCount: Total number of notices.
        - highUrgencyCount: Number of high-urgency notices.
        - openCount: Number of open notices.

        Always return valid JSON with no text outside the JSON.
        """;
}
