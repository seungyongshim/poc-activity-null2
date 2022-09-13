using Aums.Effect.Actor;
using LanguageExt;
using LanguageExt.Common;
using Proto;
using Proto.Cluster;
using static LanguageExt.Prelude;

namespace ConsoleApp1.Effect;

public static class ActorCluster<RT> where RT : struct, HasActorCluster<RT>
{
    public static Aff<RT, object> RequestAff(string identity, string kind, object msg, TimeSpan timeout) =>
        from ct in cancelToken<RT>()
        from actor in default(RT).ActorEff
        let target = ClusterIdentity.Create(identity, kind)
        from _1 in actor.RequestAsync(target, msg, ct)
                        .ToAsync()
                        .ToAff(e => Error.New(e.Reason ?? ""))
        select _1;

    public static Aff<RT, T> RequestAff<T>(string identity, string kind, object msg, TimeSpan timeout) =>
        from _1 in RequestAff(identity, kind, msg, timeout)
        select (T)_1;
}
