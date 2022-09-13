using System.Diagnostics;
using Boost.Proto.Actor.DependencyInjection;
using Boost.Proto.Actor.Hosting.Cluster;
using Boost.Proto.Actor.Hosting.OpenTelemetry;
using ConsoleApp1.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Start"),
    ActivityStopped = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Stop")
};

ActivitySource.AddActivityListener(listener);

var host = Host.CreateDefaultBuilder()
               .ConfigureServices(services =>
               {

               })
               .UseProtoActorCluster((option, sp) =>
               {
                   option.Name = "poc";
                   option.SystemShutdownDelaySec = 0;
                   option.ClusterKinds.Add(new
                   (
                       nameof(HelloGrainActor),
                       sp.GetRequiredService<IPropsFactory<HelloGrainActor>>().Create()
                   ));

                   option.FuncActorSystemStart = root =>
                   {
                       root.SpawnNamed(sp.GetRequiredService<IPropsFactory<EchoActor>>().Create(),
                                       nameof(EchoActor));
                       return root;
                   };
               })
               .UseProtoActorOpenTelemetry()
               .Build();

await host.StartAsync();

var root = host.Services.GetRequiredService<IRootContext>();

using var cts = new CancellationTokenSource();
var x = root.System.Cluster().RequestAsync<string>("1", nameof(HelloGrainActor), "Hello", root, cts.Token);


await host.WaitForShutdownAsync();

