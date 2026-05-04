using Azure.AI.Projects;
using Microsoft.Extensions.Logging;

namespace FxAgent.Agents;

public class CtAgNotification : BaseAgent
{
    public CtAgNotification(AIProjectClient aiProjectClient, string deploymentName, ILogger? logger = null)
        : base(aiProjectClient, "ct-ag-notification", deploymentName, GetInstructions(), null, logger)
    {
    }

    private static string GetInstructions() => """
        You are a notification and assignment agent for tax notices. You receive a JSON object representing an extracted tax notice and return an action plan.

        Produce a notification plan with the following fields:
        - assignedTo: Role or team responsible (e.g. "tax-team", "compliance-officer", "senior-accountant") based on taxType and amountDue.
        - assignmentReason: Brief explanation of why this role was assigned.
        - emailSubject: Subject line for the notification email.
        - emailBody: Full notification email body addressed to the assigned role, summarizing the notice and required actions.
        - reminders: Array of reminder objects, each with:
          - daysBefore: Number of days before dueDate to send the reminder.
          - message: Short reminder message.
        - escalationRule: Condition under which to escalate (e.g. "if no response 5 days before due date").

        Assignment rules:
        - amountDue > 50000 or urgency "high": assign to "senior-accountant"
        - taxType "payroll" or "sales": assign to "compliance-officer"
        - All others: assign to "tax-team"
        - If actionRequired is false: assign to "tax-team" for filing only.

        Return a single JSON object with no text outside the JSON.
        """;
}
