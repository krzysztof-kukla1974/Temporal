namespace SampleWorker;

using Temporalio.Workflows;

[Workflow]
public class CalculatePIWorkflow
{
    private const int DEFAULT_TIMEOUT = 900;

    [WorkflowRun]
    public async Task<string> RunAsync(string iterations)
    {
        var pi = Workflow.ExecuteActivityAsync(
            (CalculatePIActivity act) => act.CalculatePI(Workflow.Info.WorkflowId.ToString(), iterations),
            new() { ScheduleToCloseTimeout = TimeSpan.FromMinutes(DEFAULT_TIMEOUT) });

        await pi;

        return $"PI={pi.Result.ToString()}";
    }

    [WorkflowQuery]
    public string CurrentStatus() => "STILL ALIVE!!!.";
}