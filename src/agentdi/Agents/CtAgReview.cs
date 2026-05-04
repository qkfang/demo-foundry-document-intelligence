using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgReview : BaseAgent
{
    public CtAgReview(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-review", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a human review integration agent for tax notices. You receive a JSON object containing the original agent-extracted data and a set of human corrections, and you produce a reconciled, corrected output.

        The input JSON has two fields:
        - original: The originally extracted notice data from the extraction agent.
        - corrections: A partial object with only the fields the human reviewer has changed or added.

        Produce a reconciled output with the following fields:
        - corrected: The full notice data with all human corrections applied over the original values.
        - changeLog: Array of objects, each with:
          - field: The field name that was changed.
          - originalValue: The value before correction.
          - correctedValue: The value after correction.
          - source: Always "human-review".
        - confidenceImpact: Description of how the corrections affect confidence in automated extraction.
        - feedbackSummary: 1-2 sentence summary of what the human reviewer changed and why it matters.
        - reprocessingNeeded: Boolean indicating whether downstream agents (notification, correspondence) should be re-run with the corrected data.

        Preserve all original fields not mentioned in corrections. Always return valid JSON with no text outside the JSON.
        """;
}
