using Trace;

using TemporalHelper;

using Temporalio.Client;

InputTraceWorkflowParameters p = new InputTraceWorkflowParameters
{
    WorkerName = "O365CollectorWorkflow",
    WorkflowId = Protocol.GenerateTemporalGuid("O365CollectorWorkflow"),
    Host = args[0], // "localhost:7233"
    QueueName = "trace-task-queue",
    TenantId = "4780c1c0-5eda-4f9f-96dd-6d02b557770d",
    ClientId = "8186e56a-168b-4f4e-8e73-046ebc9b6196",
    ClientSecret = "y~U8Q~Oyml1ubm5ODR.M-A4UkccwnJwYg.rmacWo",
    UserPrincipalNames = "admin@lertian.onmicrosoft.com,rob.fischer@lertian.onmicrosoft.com,john.doe@lertian.onmicrosoft.com,andyzipper@lertian.onmicrosoft.com,jane.doe@lertian.onmicrosoft.com".Split(","),
    FolderName = "Inbox",
    DateFromStr = args[1], //"2023-01-01"
    DateToStr = args[2], //"2023-01-05"
    Searches = "consequat,gubergren,voluptua,erat".Split(","),
    AdlsUri = "https://kkafs1.blob.core.windows.net",
    FileSystemName = "blob1",
    SasToken = "sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupyx&se=2024-10-17T03:22:54Z&st=2024-08-21T19:22:54Z&spr=https&sig=j0vjjH7lkHcuCgLxzXld5o8JZ%2BYIguh50mQFuxBG0wI%3D",
    RootFolder = "/TRACE"
};

Console.WriteLine($"Starting {p.WorkerName} client");

var client = await TemporalClient.ConnectAsync(new(p.Host));

Console.WriteLine($"Running Workflow {p.WorkflowId}...");

var handle = await client.StartWorkflowAsync(
    (O365CollectorWorkflow wf) => wf.RunAsync(p.ToString()),
    new WorkflowOptions(p.WorkflowId, p.QueueName));

var results = await handle.GetResultAsync();

Console.WriteLine($"Results exported to the following blobs:\n{results}");
Console.WriteLine($"Workflow {p.WorkflowId} completed");