using SampleWorker;

using Temporalio.Client;
using Temporalio.Worker;

try
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    using var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        tokenSource.Cancel();
        eventArgs.Cancel = true;
    };

    var activities = new CalculatePIActivity();

    using var worker = new TemporalWorker
    (
        client: client,
        options: new TemporalWorkerOptions("my-task-queue").
            AddActivity(activities.CalculatePI).
            AddWorkflow<CalculatePIWorkflow>()
    );

    Console.WriteLine("Running worker");
    await worker.ExecuteAsync(tokenSource.Token);
}
catch (InvalidOperationException ioEx)
{
    Console.WriteLine("Temporal error: " + ioEx.Message);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Worker cancelled");
}