namespace Multifactor.Radius.Adapter.v2.Tests;

/// <summary>
/// Wraps 
/// </summary>
internal class TestEnvironmentVariables
{
    private readonly HashSet<string> _names;

    private TestEnvironmentVariables(HashSet<string> names)
    {
        _names = names;
    }

    public static void With(Action<TestEnvironmentVariables> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var names = new HashSet<string>();

        action(new TestEnvironmentVariables(names));

        foreach (var name in names)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    public TestEnvironmentVariables SetEnvironmentVariable(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
        }

        _names.Add(name);
        Environment.SetEnvironmentVariable(name, value);

        return this;
    }
}
