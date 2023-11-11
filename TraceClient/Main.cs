using Trace;

using TemporalHelper;

using Temporalio.Client;

InputTraceWorkflowParameters p = new InputTraceWorkflowParameters
{
    WorkerName = "O365CollectorWorkflow",
    WorkflowId = Protocol.GenerateTemporalGuid("O365CollectorWorkflow"),
    Host = "localhost:7233",
    QueueName = "my-task-queue",
    TenantId = "4780c1c0-5eda-4f9f-96dd-6d02b557770d",
    ClientId = "8186e56a-168b-4f4e-8e73-046ebc9b6196",
    ClientSecret = "y~U8Q~Oyml1ubm5ODR.M-A4UkccwnJwYg.rmacWo",
    UserPrincipalNames = "admin@lertian.onmicrosoft.com,rob.fischer@lertian.onmicrosoft.com,john.doe@lertian.onmicrosoft.com,andyzipper@lertian.onmicrosoft.com,jane.doe@lertian.onmicrosoft.com".Split(","),
    FolderName = "Inbox",
    DateFromStr = "2023-01-01",
    DateToStr = "2023-10-31",
    Searches = "consequat,gubergren,voluptua,erat".Split(","),
    AdlsUri = "https://kkadls1.blob.core.windows.net",
    FileSystemName = "file1",
    SasToken = "?sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupyx&se=2023-12-13T04:37:44Z&st=2023-11-07T20:37:44Z&spr=https&sig=p2zR0hXZhjttv8dl7Wn3gTO4u28pMrkkiIzdU3F7rOY%3D",
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