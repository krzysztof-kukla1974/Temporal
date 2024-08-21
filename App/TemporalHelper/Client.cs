namespace TemporalHelper;

using System.Text;
using Azure;
using Azure.Storage.Files.DataLake;

public class Protocol
{
    public static string GenerateTemporalGuid(string classStr)
    {
        return classStr + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }
}

public class Adls
{
    private static DataLakeFileSystemClient GetFileSystemClient(string adlsUri, string sasToken, string fileSystemName)
    {
        DataLakeServiceClient client = new DataLakeServiceClient(new Uri(adlsUri), new AzureSasCredential(sasToken));
        return client.GetFileSystemClient(fileSystemName);
    }

    private static async Task<DataLakeDirectoryClient> CreateDirectory(DataLakeFileSystemClient fileSystemClient, string directoryName)
    {
        return await fileSystemClient.CreateDirectoryAsync(directoryName);
    }

    private static async Task<Response<Azure.Storage.Files.DataLake.Models.PathInfo>> UploadFile(DataLakeDirectoryClient directoryClient, string fileName, string fileContent)
    {
        DataLakeFileClient fileClient = directoryClient.GetFileClient(fileName);
        byte[] fileContentInBytes = Encoding.UTF8.GetBytes(fileContent);
        MemoryStream memoryStream = new MemoryStream(fileContentInBytes);
        return await fileClient.UploadAsync(content: memoryStream, overwrite: true);
    }

    public static async void UploadFileToFolder(string adlsUri, string sasToken, string fileSystemName, string directoryName, string fileName, string fileContent)
    {
        Console.WriteLine("##### adlsUri: " + adlsUri);
        Console.WriteLine("##### sasToken: " + sasToken);
        Console.WriteLine("##### fileSystemName: " + fileSystemName);
        Console.WriteLine("##### directoryName: " + directoryName);
        Console.WriteLine("##### fileName: " + fileName);
        DataLakeFileSystemClient fileSystemClient = GetFileSystemClient(adlsUri, sasToken, fileSystemName);
        Console.WriteLine("##### Got File System Client");
        DataLakeDirectoryClient directoryClient = await CreateDirectory(fileSystemClient, directoryName);
        Console.WriteLine("##### Created Directory");
        await Adls.UploadFile(directoryClient, fileName, fileContent);
    }
}