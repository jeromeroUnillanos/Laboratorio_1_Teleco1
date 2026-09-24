namespace LibraryLoadTester;

public class CliOptions
{
    public string Url { get; set; } = "http://localhost:5018/api/books";
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? JsonBody { get; set; } = null;
    public string ProcessName { get; set; } = "MiPrimerApi";
    public string OutputDir { get; set; } = "results";
    public int CooldownSeconds { get; set; } = 5;
    public int RequestTimeoutSeconds { get; set; } = 10;

    // Default progressive load plan: concurrency:durationSeconds pairs.
    public List<RoundConfig> Rounds { get; set; } = new()
    {
        new RoundConfig { RoundNumber = 1, Concurrency = 5,   DurationSeconds = 10 },
        new RoundConfig { RoundNumber = 2, Concurrency = 20,  DurationSeconds = 10 },
        new RoundConfig { RoundNumber = 3, Concurrency = 50,  DurationSeconds = 10 },
        new RoundConfig { RoundNumber = 4, Concurrency = 100, DurationSeconds = 10 },
        new RoundConfig { RoundNumber = 5, Concurrency = 200, DurationSeconds = 10 },
    };

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--url":
                    options.Url = args[++i];
                    break;
                case "--method":
                    options.Method = new HttpMethod(args[++i].ToUpperInvariant());
                    break;
                case "--body":
                    options.JsonBody = args[++i];
                    break;
                case "--process":
                    options.ProcessName = args[++i];
                    break;
                case "--output":
                    options.OutputDir = args[++i];
                    break;
                case "--cooldown":
                    options.CooldownSeconds = int.Parse(args[++i]);
                    break;
                case "--timeout":
                    options.RequestTimeoutSeconds = int.Parse(args[++i]);
                    break;
                case "--rounds":
                    // Format: "5:10,20:10,50:10,100:10,200:10"  (concurrency:durationSeconds)
                    options.Rounds = ParseRounds(args[++i]);
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    break;
            }
        }

        return options;
    }

    private static List<RoundConfig> ParseRounds(string spec)
    {
        var rounds = new List<RoundConfig>();
        var parts = spec.Split(',', StringSplitOptions.RemoveEmptyEntries);
        int roundNumber = 1;
        foreach (var part in parts)
        {
            var pieces = part.Split(':');
            int concurrency = int.Parse(pieces[0]);
            int duration = pieces.Length > 1 ? int.Parse(pieces[1]) : 10;
            rounds.Add(new RoundConfig { RoundNumber = roundNumber++, Concurrency = concurrency, DurationSeconds = duration });
        }
        return rounds;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        Library API Load Tester

        Options:
          --url <url>          Full URL of the endpoint to test (default: http://localhost:5018/api/books)
          --method <verb>      HTTP method: GET, POST, PUT, DELETE (default: GET)
          --body <json>        JSON body to send (for POST/PUT)
          --process <name>     Process name of the API, for CPU/RAM sampling (default: MiPrimerApi)
          --rounds <spec>      Comma-separated concurrency:durationSeconds pairs, e.g. "5:10,20:10,50:10"
          --cooldown <sec>     Seconds to rest between rounds (default: 5)
          --timeout <sec>      Per-request timeout in seconds (default: 10)
          --output <dir>       Output folder for CSV results (default: results)

        Example:
          dotnet run -- --url http://localhost:5018/api/books --process MiPrimerApi --rounds 5:10,20:10,50:10,100:10,200:10
        """);
    }
}
