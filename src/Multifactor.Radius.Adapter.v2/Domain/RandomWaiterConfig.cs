namespace Multifactor.Radius.Adapter.v2.Domain;

public class RandomWaiterConfig
{
    public int Min { get; }
    public int Max { get; }
    public bool ZeroDelay => Min == 0 && Max == 0;

    protected RandomWaiterConfig(int min, int max)
    {
        if (min < 0 || max < 0)
            throw new ArgumentException("Delay values cannot be negative");
            
        if (min > max)
            throw new ArgumentException("Min delay cannot be greater than max delay");

        Min = min;
        Max = max;
    }

    public static RandomWaiterConfig Create(string? delaySettings)
    {
        if (string.IsNullOrWhiteSpace(delaySettings))
            return new RandomWaiterConfig(0, 0);

        if (int.TryParse(delaySettings, out var fixedDelay))
            return new RandomWaiterConfig(fixedDelay, fixedDelay);

        var parts = delaySettings.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new ArgumentException("Delay must be a single number or range like 'min-max'");

        if (!int.TryParse(parts[0], out var min) || !int.TryParse(parts[1], out var max))
            throw new ArgumentException("Delay values must be valid integers");

        return new RandomWaiterConfig(min, max);
    }
}