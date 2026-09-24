namespace LibraryLoadTester;

/// <summary>One single HTTP request outcome.</summary>
public readonly record struct RequestResult(
    double LatencyMs,
    bool Success,
    int StatusCode);

/// <summary>One sample of the target process' resource usage.</summary>
public readonly record struct ResourceSample(
    double CpuPercent,
    double MemoryMb);

/// <summary>Aggregated results for one completed round.</summary>
public class RoundResult
{
    public required RoundConfig Config { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public double AvgLatencyMs { get; init; }
    public double MinLatencyMs { get; init; }
    public double MaxLatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public double P99LatencyMs { get; init; }
    public double RequestsPerSecond { get; init; }
    public double AvgCpuPercent { get; init; }
    public double PeakCpuPercent { get; init; }
    public double AvgMemoryMb { get; init; }
    public double PeakMemoryMb { get; init; }

    public double SuccessRate => TotalRequests == 0 ? 0 : (double)SuccessCount / TotalRequests * 100.0;
}
