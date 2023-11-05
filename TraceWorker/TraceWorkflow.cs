namespace Trace;

using Temporalio.Workflows;

[Workflow]
public class O365CollectorWorkflow
{
    private const int DEFAULT_TIMEOUT = 900;

    [WorkflowRun]
    public async Task<string> RunAsync(string tenantId, string clientId, string clientSecret, string userPrincipalNames, string folderName, string dateFromStr, string dateToStr, string searches)
    {
        string workflowId = Workflow.Info.WorkflowId.ToString();
        var w = Workflow.ExecuteActivityAsync(
            (O365CollectorActivity act) => act.CollectFromToDate(tenantId, clientId, clientSecret, userPrincipalNames, folderName, dateFromStr, dateToStr, searches),
            new() { ScheduleToCloseTimeout = TimeSpan.FromMinutes(DEFAULT_TIMEOUT) });
        Console.WriteLine($"Starting Workflow {workflowId}");
        await w;
        Console.WriteLine($"Workflow {workflowId} completed");
        return w.Result;
    }
}