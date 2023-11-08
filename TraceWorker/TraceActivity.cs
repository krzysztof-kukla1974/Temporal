namespace Trace;

using Azure.Identity;
using HtmlAgilityPack;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text;
using System.Text.RegularExpressions;
using Temporalio.Activities;
using TemporalHelper;
using System.Text.Json;

public class O365CollectorActivity
{
    private static string EMPTY_JSON = "{}";
    private static bool isBody = false;
    private static InputTraceWorkflowParameters? p;

    private static string HTMLToText(string s)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(s);

        var chunks = new List<string>();

        foreach (var item in doc.DocumentNode.DescendantsAndSelf())
        {
            if (item.NodeType == HtmlNodeType.Text)
            {
                if (item.InnerText.Trim() != "")
                {
                    chunks.Add(item.InnerText.Trim());
                }
            }
        }
        s = String.Join(" ", chunks);

        return HTMLToTextExt(s);
    }

    private static string HTMLToTextExt(string s)
    {
        s = s.Replace("\u200C", " ");
        s = s.Replace("\r", " ");
        s = s.Replace("\n", " ");
        s = s.Replace("\"", "\'");
        s = s.Replace("\t", " ");
        StringBuilder sb = new StringBuilder(s);
        string[] OldWords = { "&nbsp;", "&amp;", "&quot;", "&lt;", "&gt;", "&reg;", "&copy;", "&bull;", "&trade;", "&#39;" };
        string[] NewWords = { " ", "&", "\'", "<", ">", "Â®", "Â©", "â€¢", "â„¢", "\'" };
        for (int i = 0; i < OldWords.Length; i++)
        {
            sb.Replace(OldWords[i], NewWords[i]);
        }
        s = Regex.Replace(sb.ToString(), "<[^>]*>", "");
        s = Regex.Replace(s, "\\s+", " ");
        return s;
    }

    private static string ConvertRecipientsToEmailAddress(List<Recipient> recipients)
    {
        var tmp = new StringBuilder();
        foreach (Recipient recipient in recipients)
        {
            tmp.Append(recipient.EmailAddress).Append(",");
        }
        return tmp.ToString();
    }

    private static GraphServiceClient Authenticate(string tenantId, string clientId, string clientSecret)
    {
        var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credentials, new string[] { "https://graph.microsoft.com/.default" });
    }

    private static async Task<string> CollectUser(GraphServiceClient graphClient, string userPrincipalName, string folderName, string dateStr, string[] searches)
    {
        string dateToStr = DateTime.Parse(dateStr).AddDays(1).ToString("yyyy-MM-dd");

        var users = await graphClient.Users.GetAsync(rConfig =>
        {
            rConfig.QueryParameters.Select = new string[] { "Id" };
            rConfig.QueryParameters.Filter = "UserPrincipalName eq '" + userPrincipalName + "'";
        });

        if (users == null)
        {
            return EMPTY_JSON;
        }

        var user = users?.Value?[0];

        if (user == null)
        {
            return EMPTY_JSON;
        }

        var tmp = new StringBuilder();

        tmp.Append("{\n");
        tmp.Append("\"UserPrincipalName\":\"").Append(userPrincipalName).Append("\",\n");
        tmp.Append("\"Emails\":\n");
        tmp.Append("[\n");

        var messages = await graphClient.Users["{" + user?.Id?.ToString() + "}"].MailFolders[folderName].Messages.GetAsync(rConfig =>
        {
            rConfig.QueryParameters.Select = new string[] { "SentDateTime", "Subject", "From", "ToRecipients", "BodyPreview", "Body" };
            rConfig.QueryParameters.Filter = "receivedDateTime ge " + dateStr + " and receivedDateTime lt " + dateToStr;
        });

        if (messages == null)
        {
            return EMPTY_JSON;
        }

        for (int i = 0; i < messages?.Value?.Count; i++)
        {
            tmp.Append("{\n");
            tmp.Append("\"Sent\":\"").Append(DateTime.Parse(messages?.Value?[i].SentDateTime?.ToString() ?? "").ToString("yyyy-MM-dd HH:mm:ss")).Append("\",\n");
            tmp.Append("\"Subject\":\"").Append(messages?.Value?[i].Subject).Append("\",\n");
            tmp.Append("\"From\":\"").Append(messages?.Value?[i].From?.EmailAddress?.Address).Append("\",\n");
            tmp.Append("\"To\":\"").Append(ConvertRecipientsToEmailAddress(messages?.Value?[i].ToRecipients ?? new List<Recipient>())).Append("\",\n");
            tmp.Append("\"BodyPreview\":\"").Append(HTMLToText(messages?.Value[i].BodyPreview ?? "")).Append("\",\n");
            string body = messages?.Value?[i].Body?.Content ?? "";
            if(isBody)
            {
                tmp.Append("\"Body\":\"").Append(HTMLToText(body)).Append("\",\n");
            }
            var hits = new StringBuilder();
            for (int j = 0; j < searches.Length; j++)
            {
                var matches = Regex.Matches(body, searches[j]);
                hits.Append("{");
                hits.Append("\"Key\":\"").Append(searches[j]).Append("\",");
                hits.Append("\"Count\":\"").Append(matches.Count).Append("\"");
                hits.Append("}");
                if (j < searches.Length - 1)
                {
                    hits.Append(",");
                }
                hits.Append("\n");
            }
            tmp.Append("\"Hits\":[\n").Append(hits.ToString()).Append("]\n");
            tmp.Append("}\n");
            if (i != messages?.Value?.Count - 1)
            {
                tmp.Append(",\n");
            }
        }

        tmp.Append("]\n");
        tmp.Append("}\n");

        return tmp.ToString();
    }

    public static async Task<string> CollectAllUsers(string dateStr)
    {
        if (p == null)
        {
            return "";
        }
        var graphClient = Authenticate(p.TenantId, p.ClientId ?? "", p.ClientSecret ?? "");
        var tmp = new StringBuilder();

        tmp.Append("{");
        tmp.Append("\"Date\":\"").Append(dateStr).Append("\",");
        tmp.Append("\"Users\":");
        tmp.Append("[");
        for (int i = 0; i < p.UserPrincipalNames.Length; i++)
        {
            Console.WriteLine($"Exporting User: {p.UserPrincipalNames[i]}");
            tmp.Append(await CollectUser(graphClient, p.UserPrincipalNames[i], p.FolderName, dateStr, p.Searches));
            if (i != p.UserPrincipalNames.Length - 1)
            {
                tmp.Append(",");
            }
            Console.WriteLine($"User: {p.UserPrincipalNames[i]} exported");
        }
        tmp.Append("]");
        tmp.Append("}");
        Console.WriteLine("Export completed!");
        return tmp.ToString();
    }

    public static async Task<string> RunAsync(string dateStr)
    {
        return await CollectAllUsers(dateStr);
    }

    [Activity]
    public string CollectFromToDate(string parameters)
    {
        p = JsonSerializer.Deserialize<InputTraceWorkflowParameters>(parameters);
        if (p == null)
        {
            return "";
        }
        string directoryName = Path.Combine(p.RootFolder, p.TenantId, p.ClientId);

        StringBuilder resultsCom = new StringBuilder();
        DateTime dateFrom = DateTime.Parse(p.DateFromStr);
        DateTime dateTo = DateTime.Parse(p.DateToStr);

        for (DateTime date = dateFrom; date <= dateTo; date = date.AddDays(1))
        {
            Console.WriteLine($"Exporting Date: {date.ToString("yyyy-MM-dd")}");
            string dateStr = date.ToString("yyyy-MM-dd");
            string fileName = dateStr + ".json";
            string results = RunAsync(dateStr).Result;
            Console.WriteLine($"Storing results...");
            Adls.UploadFileToFolder(p.AdlsUri, p.SasToken, p.FileSystemName, directoryName, fileName, results);
            Console.WriteLine($"Results file {fileName} uploaded onto {p.AdlsUri}{directoryName}");
            resultsCom.Append(p.AdlsUri).Append(directoryName).Append(fileName);
            if (date != dateTo)
            {
                resultsCom.Append("\n");
            }
        }
        return resultsCom.ToString();
    }
}