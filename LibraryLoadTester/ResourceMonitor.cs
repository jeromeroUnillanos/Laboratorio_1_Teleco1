using System.Diagnostics;

namespace LibraryLoadTester;

/// <summary>
/// Samples CPU% and working-set memory of the local API process while a round runs.
/// CPU% is computed from the delta in TotalProcessorTime between samples,
/// normalized by elapsed time and core count (works on Windows and Linux).
/// </summary>
public class ResourceMonitor
{
    private readonly string _processName;
    private readonly List<ResourceSample> _samples = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public ResourceMonitor(string processName)
    {
        _processName = processName;
    }

    public void Start(TimeSpan interval)
    {
        _samples.Clear();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _loopTask = Task.Run(async () =>
        {
            Process? proc = FindProcess();
            TimeSpan lastCpuTime = proc?.TotalProcessorTime ?? TimeSpan.Zero;
            DateTime lastSampleTime = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                proc ??= FindProcess();
                if (proc is null || proc.HasExited)
                {
                    proc = FindProcess();
                    if (proc is null) continue;
                    lastCpuTime = proc.TotalProcessorTime;
                    lastSampleTime = DateTime.UtcNow;
                    continue;
                }

                try
                {
                    proc.Refresh();
                    var now = DateTime.UtcNow;
                    var currentCpuTime = proc.TotalProcessorTime;

                    var cpuDelta = (currentCpuTime - lastCpuTime).TotalMilliseconds;
                    var wallDelta = (now - lastSampleTime).TotalMilliseconds;
                    double cpuPercent = 0;
                    if (wallDelta > 0)
                    {
                        cpuPercent = (cpuDelta / (wallDelta * Environment.ProcessorCount)) * 100.0;
                        cpuPercent = Math.Clamp(cpuPercent, 0, 100 * Environment.ProcessorCount);
                    }

                    double memoryMb = proc.WorkingSet64 / (1024.0 * 1024.0);

                    _samples.Add(new ResourceSample(cpuPercent, memoryMb));

                    lastCpuTime = currentCpuTime;
                    lastSampleTime = now;
                }
                catch
                {
                    // Process may have exited mid-sample; ignore and retry next tick.
                }
            }
        }, token);
    }

    public async Task<(double avgCpu, double peakCpu, double avgMem, double peakMem)> StopAsync()
    {
        _cts?.Cancel();
        if (_loopTask is not null)
        {
            try { await _loopTask; } catch { /* ignore */ }
        }

        if (_samples.Count == 0)
            return (0, 0, 0, 0);

        return (
            _samples.Average(s => s.CpuPercent),
            _samples.Max(s => s.CpuPercent),
            _samples.Average(s => s.MemoryMb),
            _samples.Max(s => s.MemoryMb)
        );
    }

    private Process? FindProcess()
    {
        // Try an exact-name match first (e.g. "MiPrimerApi"), then fall back to
        // any process whose name contains it (useful when running via `dotnet run`,
        // where the actual process is called "dotnet").
        var candidates = Process.GetProcesses()
            .Where(p =>
            {
                try { return p.ProcessName.Contains(_processName, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            })
            .ToList();

        return candidates.FirstOrDefault();
    }
}
