namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Http;

public class ActivityContext
{
    private static readonly AsyncLocal<ActivityContext> Value = new();

    /// <summary>
    /// Current context activity id.
    /// </summary>
    public string ActivityId { get; private set; }

    /// <summary>
    /// Returns current ActivityContext or creates new if null.
    /// </summary>
    public static ActivityContext Current
    {
        get => Value.Value ??= new ActivityContext();
        private set => Value.Value = value;
    }

    private ActivityContext()
    {
        ActivityId = Guid.NewGuid().ToString();
        Current = this;
    }

    private ActivityContext(string activityId)
    {
        ActivityId = activityId;
        Current = this;
    }

    /// <summary>
    /// Creates and sets current ActivityContext then returns it.
    /// </summary>
    /// <param name="activityId">Specified activity id.</param>
    /// <returns>Current ActivityContext.</returns>
    public static ActivityContext Create(string activityId) => new(activityId);

    /// <summary>
    /// Sets activity id to the current ActivityContext.
    /// </summary>
    /// <param name="activityId">New activity id.</param>
    public void SetActivityId(string activityId) => ActivityId = activityId;
}