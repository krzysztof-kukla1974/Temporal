﻿using Trace;

using Temporalio.Client;
using Temporalio.Worker;

string workerName = "O365CollectorWorkflow";
string host = "localhost:7233";
string queueName = "my-task-queue";

try
{
    var client = await TemporalClient.ConnectAsync(new(host));

    using var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        tokenSource.Cancel();
        eventArgs.Cancel = true;
    };

    var activities = new O365CollectorActivity();

    using var worker = new TemporalWorker
    (
        client: client,
        options: new TemporalWorkerOptions(queueName).AddActivity(activities.CollectFromToDate).AddWorkflow<O365CollectorWorkflow>()
    );

    Console.WriteLine($"Starting worker: {workerName}");
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