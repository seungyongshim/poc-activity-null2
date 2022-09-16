using System;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Proto.Cluster;
using Proto.Mailbox;

namespace Boost.Proto.Actor.Opentelemetry;

public delegate void ActivitySetup(Activity? activity, object message);

public static class OpenTelemetryTracingExtensions
{
    public static TracerProviderBuilder AddProtoActorInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(ProtoTags.ActivitySourceName);

    public static Props WithTracing(
        this Props props,
        ActivitySetup? sendActivitySetup = null,
        ActivitySetup? receiveActivitySetup = null
    )
    {
        sendActivitySetup ??= OpenTelemetryHelpers.DefaultSetupActivity!;
        receiveActivitySetup ??= OpenTelemetryHelpers.DefaultSetupActivity!;


        return props.WithMailbox(() => UnboundedOpenTelemetryMailbox.Create())
               .WithContextDecorator(ctx =>
            {
                return new OpenTelemetryActorContextDecorator(ctx, sendActivitySetup, receiveActivitySetup);
            })
            .WithReceiverMiddleware(OpenTelemetryReceiverMiddleware)
            .WithSenderMiddleware(OpenTelemetrySenderMiddleware);
    }

    private static Receiver OpenTelemetryReceiverMiddleware(Receiver next)
        => async (context, envelope) =>
        {
            await next(context, envelope);
        };

    private static Sender OpenTelemetrySenderMiddleware(Sender next)
        => async (context, target, envelope) => {
            var activity = Activity.Current;

            if (activity != null)
            {
                Console.WriteLine($"6:{Activity.Current?.TraceId}");
                envelope = envelope.WithHeaders(activity.Context.GetPropagationHeaders());
            }

            await next(context, target, envelope);
        };

    public static IRootContext WithTracing(this IRootContext context, ActivitySetup? sendActivitySetup = null)
    {
        sendActivitySetup ??= OpenTelemetryHelpers.DefaultSetupActivity!;

        return new OpenTelemetryRootContextDecorator(context.WithSenderMiddleware(OpenTelemetrySenderMiddleware), sendActivitySetup);
    }
}
