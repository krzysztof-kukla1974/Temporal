using Trace;

using Temporalio.Client;
using TemporalHelper;

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
string directoryPath = Path.Combine("/Users/krzysztof.kukla/Downloads", tenantId, clientId);
string fileName = dateFromStr + "_" + dateToStr + ".json";
string filePath = Path.Combine(directoryPath, fileName);

Console.WriteLine($"Starting {workerName} client");

var client = await TemporalClient.ConnectAsync(new(host));

Console.WriteLine($"Starting Workflow {workflowId}");

var handle = await client.StartWorkflowAsync(
    (O365CollectorWorkflow wf) => wf.RunAsync(tenantId, clientId, clientSecret, userPrincipalNames, folderName, dateFromStr, dateToStr, searches),
    new WorkflowOptions(workflowId, queueName));

var result = await handle.GetResultAsync();
Directory.CreateDirectory(directoryPath);
File.WriteAllText(filePath, result);

Console.WriteLine($"Workflow {workflowId} completed.\nResults stored in {filePath} file.");