using System.Diagnostics;

using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Start"),
    ActivityStopped = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Stop")
};

ActivitySource.AddActivityListener(listener);
var activitySource = new ActivitySource("what?!");

await SomeAsyncOperation(activitySource);

Console.WriteLine(Activity.Current?.ToString() ?? "Activity Current is null!!");

static async Task<Activity?> SomeAsyncOperation(ActivitySource src)
{
    await Task.Delay(1).ConfigureAwait(true);
    return src.StartActivity("example");
}
