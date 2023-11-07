using Trace;

using Temporalio.Client;
using TemporalHelper;
using Azure.Storage.Files.DataLake;
using Azure;

// Tracing parameters
string workerName = "O365CollectorWorkflow";
string workflowId = Protocol.GenerateTemporalGuid(workerName);
string host = "localhost:7233";
string queueName = "my-task-queue";
string tenantId = "4780c1c0-5eda-4f9f-96dd-6d02b557770d";
string clientId = "8186e56a-168b-4f4e-8e73-046ebc9b6196";
string clientSecret = "y~U8Q~Oyml1ubm5ODR.M-A4UkccwnJwYg.rmacWo";
string userPrincipalNames = "admin@lertian.onmicrosoft.com,rob.fischer@lertian.onmicrosoft.com,john.doe@lertian.onmicrosoft.com,andyzipper@lertian.onmicrosoft.com,jane.doe@lertian.onmicrosoft.com";
string folderName = "Inbox";
string dateFromStr = "2023-05-01";
string dateToStr = "2023-05-05";
string searches = "consequat,gubergren,voluptua,erat";

// ADLS parameters
string adlsUri = "https://kkadls1.blob.core.windows.net";
string fileSystemName = "file1";
string sasToken = "?sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupyx&se=2023-12-13T04:37:44Z&st=2023-11-07T20:37:44Z&spr=https&sig=p2zR0hXZhjttv8dl7Wn3gTO4u28pMrkkiIzdU3F7rOY%3D";
string rootFolder = "/TRACE";
string directoryPath = Path.Combine(rootFolder, tenantId, clientId);
string fileName = dateFromStr + "_" + dateToStr + ".json";

Console.WriteLine($"Starting {workerName} client");

var client = await TemporalClient.ConnectAsync(new(host));

Console.WriteLine($"Running Workflow {workflowId}...");

var handle = await client.StartWorkflowAsync(
    (O365CollectorWorkflow wf) => wf.RunAsync(tenantId, clientId, clientSecret, userPrincipalNames, folderName, dateFromStr, dateToStr, searches),
    new WorkflowOptions(workflowId, queueName));

var result = await handle.GetResultAsync();

Console.WriteLine($"Workflow {workflowId} completed");
Console.WriteLine($"Storing results in {adlsUri}{directoryPath}...");

DataLakeFileSystemClient fileSystemClient = Adls.GetFileSystemClient(adlsUri, sasToken, fileSystemName);
DataLakeDirectoryClient directoryClient = await Adls.CreateDirectory(fileSystemClient, directoryPath);
Response<Azure.Storage.Files.DataLake.Models.PathInfo> response = await Adls.UploadFile(directoryClient, fileName, result);

Console.WriteLine($"Results file {fileName} uploaded");