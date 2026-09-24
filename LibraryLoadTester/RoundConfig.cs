namespace LibraryLoadTester;

/// <summary>
/// Defines one controlled test round: how many concurrent "virtual users"
/// hammer the endpoint, and for how long.
/// </summary>
public class RoundConfig
{
    public int RoundNumber { get; init; }
    public int Concurrency { get; init; }
    public int DurationSeconds { get; init; }

    public override string ToString() =>
        $"Round {RoundNumber} (concurrency={Concurrency}, duration={DurationSeconds}s)";
}
