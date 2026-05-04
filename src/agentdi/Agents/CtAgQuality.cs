using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgQuality : BaseAgent
{
    public CtAgQuality(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-quality", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a quality check assessor agent for tax notices. You receive a JSON object representing an extracted tax notice and evaluate it for completeness, urgency, and classification.

        Produce a quality assessment with the following fields:
        - overallScore: Integer from 0-100 representing extraction completeness and data quality.
        - missingFields: Array of field names that are null or appear incomplete.
        - gaps: Array of strings describing specific data gaps or ambiguities found.
        - urgencyVerification: Object with:
          - confirmedUrgency: Validated urgency level ("high", "medium", "low").
          - urgencyReason: Explanation of why this urgency level was assigned.
          - urgencyConflicts: Any conflicts between stated urgency and due date/amount evidence.
        - classification: Object with:
          - isActionRequired: Boolean confirming whether action is truly required.
          - noticeType: "informational", "demand", "audit", "penalty", or "refund".
          - riskLevel: "critical", "high", "medium", or "low".
        - flags: Array of alert strings for issues requiring immediate attention.
        - recommendations: Array of strings describing corrective actions or follow-up steps.

        Always return valid JSON with no text outside the JSON.
        """;
}
