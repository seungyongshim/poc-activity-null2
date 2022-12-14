using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Boost.Proto.Actor.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Proto.Cluster;
using Proto.Cluster.Gossip;

namespace Boost.Proto.Actor.Opentelemetry
{
    static class OpenTelemetryHelpers
    {
        private static readonly ActivitySource ActivitySource = new(ProtoTags.ActivitySourceName);

        public static void DefaultSetupActivity(Activity _, object __)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Activity? BuildStartedActivity(
            ActivityContext parent,
            string verb,
            object message,
            ActivitySetup activitySetup,
            ActivityKind activityKind = ActivityKind.Internal
        )
        {
            if (parent.TraceId.ToString() == "00000000000000000000000000000000")
            {
                return null;
            }

            var messageType = message?.GetType().Name ?? "Unknown";

            var name = $"{verb}{messageType}";
            var tags = new[] { new KeyValuePair<string, object?>(ProtoTags.MessageType, messageType) };
            var activity = ActivitySource.StartActivity(name, activityKind, parent, tags);

            if (activity is not null)
            {
                activitySetup(activity, message!);
            }

            return activity;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetPropagationHeaders(this ActivityContext activityContext)
        {
            var context = new List<KeyValuePair<string, string>>();
            Propagators.DefaultTextMapPropagator.Inject(new(activityContext, Baggage.Current), context, AddHeader);
            return context;
        }

        public static PropagationContext ExtractPropagationContext(this MessageHeader headers)
            => Propagators.DefaultTextMapPropagator.Extract(default, headers.ToDictionary(),
                (dictionary, key) => dictionary.TryGetValue(key, out var value) ? new[] { value } : default
            );

        private static void AddHeader(List<KeyValuePair<string, string>> list, string key, string value)
            => list.Add(new(key, value));
    }
}
