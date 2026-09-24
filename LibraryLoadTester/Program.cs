using System.Globalization;
using LibraryLoadTester;

// ============================================================================
// Library Load Tester
// Console app that sends repeated HTTP requests to a local .NET Web API
// (CRUD for library books) and measures how it behaves under increasing load.
//
// Usage examples:
//   dotnet run -- --url http://localhost:5018/api/books --process MiPrimerApi
//   dotnet run -- --url http://localhost:5018/api/books/1 --method GET --rounds 5:10,20:10,50:10,100:10,200:10
//
// This tool must only be pointed at APIs running on your own local machine,
// per the lab's safety rules. Never point it at public/external URLs.
// ============================================================================

var options = CliOptions.Parse(args);

Console.WriteLine("=================================================================");
Console.WriteLine(" Library API - Controlled Load Testing Tool");
Console.WriteLine("=================================================================");
Console.WriteLine($" Target URL     : {options.Url}");
Console.WriteLine($" HTTP method    : {options.Method}");
Console.WriteLine($" Target process : {options.ProcessName} (for CPU/RAM sampling)");
Console.WriteLine($" Rounds         : {string.Join(" | ", options.Rounds.Select(r => $"{r.Concurrency} users / {r.DurationSeconds}s"))}");
Console.WriteLine($" Cooldown       : {options.CooldownSeconds}s between rounds");
Console.WriteLine($" Output folder  : {options.OutputDir}");
Console.WriteLine("=================================================================\n");

Directory.CreateDirectory(options.OutputDir);

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds)
};

var engine = new LoadEngine(httpClient, options.Url, options.Method, options.JsonBody);
var summaryResults = new List<RoundResult>();
var rawRowsCsv = new List<string> { "Round,Concurrency,RequestIndex,LatencyMs,Success,StatusCode" };

foreach (var round in options.Rounds)
{
    Console.WriteLine($">> Starting {round} ...");

    var monitor = new ResourceMonitor(options.ProcessName);
    monitor.Start(TimeSpan.FromMilliseconds(500));

    var swRound = System.Diagnostics.Stopwatch.StartNew();
    var requestResults = await engine.RunRoundAsync(round, CancellationToken.None);
    swRound.Stop();

    var (avgCpu, peakCpu, avgMem, peakMem) = await monitor.StopAsync();

    var latencies = requestResults.Select(r => r.LatencyMs).OrderBy(x => x).ToList();
    int total = requestResults.Count;
    int success = requestResults.Count(r => r.Success);
    int failed = total - success;

    var result = new RoundResult
    {
        Config = round,
        TotalRequests = total,
        SuccessCount = success,
        FailedCount = failed,
        AvgLatencyMs = latencies.Count > 0 ? latencies.Average() : 0,
        MinLatencyMs = latencies.Count > 0 ? latencies.Min() : 0,
        MaxLatencyMs = latencies.Count > 0 ? latencies.Max() : 0,
        P95LatencyMs = Percentile(latencies, 0.95),
        P99LatencyMs = Percentile(latencies, 0.99),
        RequestsPerSecond = swRound.Elapsed.TotalSeconds > 0 ? total / swRound.Elapsed.TotalSeconds : 0,
        AvgCpuPercent = avgCpu,
        PeakCpuPercent = peakCpu,
        AvgMemoryMb = avgMem,
        PeakMemoryMb = peakMem
    };
    summaryResults.Add(result);

    for (int i = 0; i < requestResults.Count; i++)
    {
        var r = requestResults[i];
        rawRowsCsv.Add($"{round.RoundNumber},{round.Concurrency},{i},{r.LatencyMs.ToString(CultureInfo.InvariantCulture)},{r.Success},{r.StatusCode}");
    }

    Console.WriteLine($"   Requests: {total}  Success: {success}  Failed: {failed}  " +
                       $"SuccessRate: {result.SuccessRate:F1}%  RPS: {result.RequestsPerSecond:F1}");
    Console.WriteLine($"   Latency (ms) avg/min/max/p95/p99: " +
                       $"{result.AvgLatencyMs:F1} / {result.MinLatencyMs:F1} / {result.MaxLatencyMs:F1} / " +
                       $"{result.P95LatencyMs:F1} / {result.P99LatencyMs:F1}");
    Console.WriteLine($"   CPU avg/peak: {avgCpu:F1}% / {peakCpu:F1}%   Mem avg/peak: {avgMem:F1}MB / {peakMem:F1}MB\n");

    if (round != options.Rounds[^1] && options.CooldownSeconds > 0)
    {
        Console.WriteLine($"   Cooling down for {options.CooldownSeconds}s before next round...\n");
        await Task.Delay(TimeSpan.FromSeconds(options.CooldownSeconds));
    }
}

WriteSummaryCsv(Path.Combine(options.OutputDir, "results_summary.csv"), summaryResults);
File.WriteAllLines(Path.Combine(options.OutputDir, "raw_latencies.csv"), rawRowsCsv);

Console.WriteLine("=================================================================");
Console.WriteLine(" Done. Files written:");
Console.WriteLine($"   {Path.Combine(options.OutputDir, "results_summary.csv")}");
Console.WriteLine($"   {Path.Combine(options.OutputDir, "raw_latencies.csv")}");
Console.WriteLine(" Use analyze_results.py to generate the required graphs.");
Console.WriteLine("=================================================================");

// ---- helpers ---------------------------------------------------------------

static double Percentile(List<double> sortedValues, double percentile)
{
    if (sortedValues.Count == 0) return 0;
    double rank = percentile * (sortedValues.Count - 1);
    int lower = (int)Math.Floor(rank);
    int upper = (int)Math.Ceiling(rank);
    if (lower == upper) return sortedValues[lower];
    double weight = rank - lower;
    return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
}

static void WriteSummaryCsv(string path, List<RoundResult> results)
{
    var lines = new List<string>
    {
        "Round,Concurrency,DurationSec,TotalRequests,Success,Failed,SuccessRatePct," +
        "AvgLatencyMs,MinLatencyMs,MaxLatencyMs,P95LatencyMs,P99LatencyMs,RequestsPerSec," +
        "AvgCpuPercent,PeakCpuPercent,AvgMemoryMB,PeakMemoryMB"
    };

    foreach (var r in results)
    {
        lines.Add(string.Join(",", new[]
        {
            r.Config.RoundNumber.ToString(),
            r.Config.Concurrency.ToString(),
            r.Config.DurationSeconds.ToString(),
            r.TotalRequests.ToString(),
            r.SuccessCount.ToString(),
            r.FailedCount.ToString(),
            r.SuccessRate.ToString("F2", CultureInfo.InvariantCulture),
            r.AvgLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
            r.MinLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
            r.MaxLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
            r.P95LatencyMs.ToString("F2", CultureInfo.InvariantCulture),
            r.P99LatencyMs.ToString("F2", CultureInfo.InvariantCulture),
            r.RequestsPerSecond.ToString("F2", CultureInfo.InvariantCulture),
            r.AvgCpuPercent.ToString("F2", CultureInfo.InvariantCulture),
            r.PeakCpuPercent.ToString("F2", CultureInfo.InvariantCulture),
            r.AvgMemoryMb.ToString("F2", CultureInfo.InvariantCulture),
            r.PeakMemoryMb.ToString("F2", CultureInfo.InvariantCulture),
        }));
    }

    File.WriteAllLines(path, lines);
}
