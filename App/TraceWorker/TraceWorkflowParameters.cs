namespace Trace;

using System.Text.Json;

public class InputTraceWorkflowParameters
{
    // Temporal parameters
    public string WorkerName { get; set; }
    public string WorkflowId { get; set; }
    public string Host { get; set; }
    public string QueueName { get; set; }

    // O365 parameters
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string[] UserPrincipalNames { get; set; }
    public string FolderName { get; set; }

    // Tracing parameters
    public string DateFromStr { get; set; }
    public string DateToStr { get; set; }
    public string[] Searches { get; set; }

    // ADLS parameters
    public string AdlsUri { get; set; }
    public string FileSystemName { get; set; }
    public string SasToken { get; set; }
    public string RootFolder { get; set; }

    public InputTraceWorkflowParameters()
    {
        this.WorkerName = "";
        this.WorkflowId = "";
        this.Host = "";
        this.QueueName = "";
        this.TenantId = "";
        this.ClientId = "";
        this.ClientSecret = "";
        this.UserPrincipalNames = new string[] { };
        this.FolderName = "";
        this.DateFromStr = "";
        this.DateToStr = "";
        this.Searches = new string[] { };
        this.AdlsUri = "";
        this.FileSystemName = "";
        this.SasToken = "";
        this.RootFolder = "";
    }

    override public string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class OutputTraceWorkflowParameters
{
    public List<DayReport> Report { get; set; }

    public OutputTraceWorkflowParameters()
    {
        this.Report = new List<DayReport>();
    }

    override public string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class DayReport
{
    public string DayStr { get; set; }
    public string Status { get; set; }
    public string Preview { get; set; }
    public string AdlsFilePath { get; set; }

    public DayReport()
    {
        this.DayStr = "";
        this.Status = "";
        this.Preview = "";
        this.AdlsFilePath = "";
    }
}