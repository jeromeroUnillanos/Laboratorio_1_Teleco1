using System.Diagnostics;

namespace LibraryLoadTester;

/// <summary>
/// Fires repeated HTTP requests at a target endpoint using N concurrent workers
/// for a fixed duration, and collects per-request latency/outcome.
/// </summary>
public class LoadEngine
{
    private readonly HttpClient _client;
    private readonly string _url;
    private readonly HttpMethod _method;
    private readonly string? _jsonBody;

    public LoadEngine(HttpClient client, string url, HttpMethod method, string? jsonBody = null)
    {
        _client = client;
        _url = url;
        _method = method;
        _jsonBody = jsonBody;
    }

    public async Task<List<RequestResult>> RunRoundAsync(RoundConfig config, CancellationToken outerToken)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<RequestResult>();
        using var roundCts = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
        roundCts.CancelAfter(TimeSpan.FromSeconds(config.DurationSeconds));

        var workers = new List<Task>();
        for (int w = 0; w < config.Concurrency; w++)
        {
            workers.Add(Task.Run(async () =>
            {
                while (!roundCts.IsCancellationRequested)
                {
                    var result = await SendOneRequestAsync();
                    results.Add(result);
                }
            }, CancellationToken.None));
        }

        try
        {
            await Task.WhenAll(workers);
        }
        catch
        {
            // Individual worker failures are already captured per-request; ignore here.
        }

        return results.ToList();
    }

    private async Task<RequestResult> SendOneRequestAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(_method, _url);
            if (_jsonBody is not null)
            {
                request.Content = new StringContent(_jsonBody, System.Text.Encoding.UTF8, "application/json");
            }

            using var response = await _client.SendAsync(request);
            sw.Stop();

            bool success = (int)response.StatusCode is >= 200 and < 400;
            return new RequestResult(sw.Elapsed.TotalMilliseconds, success, (int)response.StatusCode);
        }
        catch
        {
            sw.Stop();
            // Connection refused, timeout, reset, etc. -> counted as a failed request (status 0).
            return new RequestResult(sw.Elapsed.TotalMilliseconds, false, 0);
        }
    }
}
