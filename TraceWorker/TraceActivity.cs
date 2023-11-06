namespace Trace;

using Azure.Identity;
using HtmlAgilityPack;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text;
using System.Text.RegularExpressions;
using Temporalio.Activities;

public class O365CollectorActivity
{
    private static string EMPTY_JSON = "{}";
    private static bool isBody = false;

    private static string tenantId = "";
    private static string clientId = "";
    private static string clientSecret = "";
    private static string[] userPrincipalNames = { };
    private static string folderName = "";
    private static string dateStr = "";
    private static string[] searches = { };

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

    public static async Task<string> CollectAllUsers()
    {
        var graphClient = Authenticate(tenantId, clientId, clientSecret);
        var tmp = new StringBuilder();

        tmp.Append("{");
        tmp.Append("\"Date\":\"").Append(dateStr).Append("\",");
        tmp.Append("\"Users\":");
        tmp.Append("[");
        for (int i = 0; i < userPrincipalNames.Length; i++)
        {
            Console.WriteLine($"Exporting User: {userPrincipalNames[i]}");
            tmp.Append(await CollectUser(graphClient, userPrincipalNames[i], folderName, dateStr, searches));
            if (i != userPrincipalNames.Length - 1)
            {
                tmp.Append(",");
            }
            Console.WriteLine($"User: {userPrincipalNames[i]} exported");
        }
        tmp.Append("]");
        tmp.Append("}");
        Console.WriteLine("Export completed!");
        return tmp.ToString();
    }

    public static async Task<string> RunAsync()
    {
        return await CollectAllUsers();
    }

    [Activity]
    public string CollectDay(string tenantIdIn, string clientIdIn, string clientSecretIn, string userPrincipalNamesIn, string folderNameIn, string dateStrIn, string searchesIn)
    {
        tenantId = tenantIdIn;
        clientId = clientIdIn;
        clientSecret = clientSecretIn;
        userPrincipalNames = userPrincipalNamesIn.Split(",");
        folderName = folderNameIn;
        dateStr = dateStrIn;
        searches = searchesIn.Split(",");

        return RunAsync().Result;
    }

    [Activity]
    public string CollectFromToDate(string tenantIdIn, string clientIdIn, string clientSecretIn, string userPrincipalNamesIn, string folderNameIn, string dateFromStrIn, string dateToStrIn, string searchesIn)
    {
        StringBuilder tmp = new StringBuilder();
        DateTime dateFrom = DateTime.Parse(dateFromStrIn);
        DateTime dateTo = DateTime.Parse(dateToStrIn);

        tmp.Append("{");
        tmp.Append("\"Dates\":");
        tmp.Append("[");
        for (DateTime date = dateFrom; date <= dateTo; date = date.AddDays(1))
        {
            Console.WriteLine($"Exporting Date: {date.ToString("yyyy-MM-dd")}");
            tmp.Append(CollectDay(tenantIdIn, clientIdIn, clientSecretIn, userPrincipalNamesIn, folderNameIn, date.ToString("yyyy-MM-dd"), searchesIn));
            if(date != dateTo)
            {
                tmp.Append(",");
            }
            Console.WriteLine($"Date: {date.ToString("yyyy-MM-dd")} exported");
        }
        tmp.Append("]");
        tmp.Append("}");

        return tmp.ToString();
    }
}