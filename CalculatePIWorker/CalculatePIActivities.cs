namespace SampleWorker;

using System;
using System.Diagnostics;
using Temporalio.Activities;

public class CalculatePIActivity
{
    private const int PROGRESS_INT = 100000000;

    private void Log(string w, string t)
    {
        Console.WriteLine($"WorkflowId: {w} Progress: {t}");
    }

    [Activity]
    public double CalculatePI(string workflowId, string iterationsStr)
    {
        long inCount = 0;
        long maxCount = Convert.ToInt64(iterationsStr);
        var rand = new Random();
        double pi = 0;

        for (long index = 0; index < maxCount; index++)
        {
            var x = rand.NextSingle();
            var y = rand.NextSingle();
            if (Math.Pow(x, 2) + Math.Pow(y, 2) <= 1)
            {
                inCount++;
                pi = (double)4 * inCount / maxCount;
            }

            if (index % PROGRESS_INT == 0)
            {
                var progress = (double)index * 100 / maxCount;
                Log(workflowId, progress.ToString("00") + "%");
            }
        }

        Log(workflowId, "Progress: 100%");
        Log(workflowId, $"PI={pi.ToString()}");
        return pi;
    }
}