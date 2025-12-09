namespace Multifactor.Radius.Adapter.v2.Infrastructure;

public static class RandomWaiter
{
    /// <summary>
    /// Performs waiting task with configured delay values.
    /// </summary>
    /// <returns>Waiting task.</returns>
    public static Task WaitSomeTimeAsync(bool isZeroDelay, int min, int max)
    {
        if (isZeroDelay) return Task.CompletedTask;

        var trueMax = min == max ? max : max + 1;
        var delay = new Random().Next(min, trueMax);

        return Task.Delay(TimeSpan.FromSeconds(delay));
    }
}