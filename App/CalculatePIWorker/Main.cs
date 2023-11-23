using SampleWorker;

using Temporalio.Client;
using Temporalio.Worker;

string workerName = "O365CollectorWorkflow";
string host = "127.0.0.1:7233";
if (args.Length > 0)
{
    host = args[0];
}
string queueName = "trace-task-queue";

try
{
    Console.WriteLine($"Connecting to {host}");
    var client = await TemporalClient.ConnectAsync(new(host));
    Console.WriteLine("Connected");

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
        options: new TemporalWorkerOptions(queueName).
            AddActivity(activities.CalculatePI).
            AddWorkflow<CalculatePIWorkflow>()
    );

    Console.WriteLine($"Starting workflow: {workerName}");
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