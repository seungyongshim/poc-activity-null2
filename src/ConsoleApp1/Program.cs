using System.Diagnostics;
using Boost.Proto.Actor.DependencyInjection;
using Boost.Proto.Actor.Hosting.Cluster;
using Boost.Proto.Actor.Hosting.OpenTelemetry;
using Boost.Proto.Actor.Opentelemetry;
using ConsoleApp1.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using Proto;
using Proto.Cluster;

using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStarted = {},
    ActivityStopped = {}
    //ActivityStarted = activity => Console.WriteLine($"{activity.Id} - Start"),
    //ActivityStopped = activity => Console.WriteLine($"{activity.Id} - Stop")
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
                   option.FuncClusterConfig = config => config
                           .WithGossipInterval(TimeSpan.FromDays(1))
                           .WithTimeout(TimeSpan.FromDays(1));

                   option.ClusterKinds.Add(new
                   (
                       nameof(HelloGrainActor),
                       sp.GetRequiredService<IPropsFactory<HelloGrainActor>>().Create()
                   ));

                   option.FuncActorSystemStartAsync = async (root, next) =>
                   {
                       root.SpawnNamed(sp.GetRequiredService<IPropsFactory<EchoActor>>().Create(),
                                       nameof(EchoActor));
                       await next(root);
                   };
               })
               .UseProtoActorOpenTelemetry()
               .Build();

await host.StartAsync();

_ = OpenTelemetry.Sdk.SuppressInstrumentation;

var root = host.Services.GetRequiredService<IRootContext>();



using var cts = new CancellationTokenSource();
using var activitySource = new ActivitySource(ProtoTags.ActivitySourceName);
using var trace = activitySource.StartActivity();
var x = root.System.Cluster().RequestAsync<string>("1", nameof(HelloGrainActor), "Hello", root, cts.Token);


await host.WaitForShutdownAsync();

