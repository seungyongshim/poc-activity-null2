using ConsoleApp1.Effect;
using LanguageExt;
using Proto;
using Proto.Cluster;
using static LanguageExt.Prelude;

namespace Aums.Effect.Actor;

public readonly record struct ActorClusterIO(Cluster Cluster, ISenderContext SenderContext) 
{
    public async Task<Either<Fail, object>> RequestAsync(ClusterIdentity id, object message, CancellationToken ct)
    {
        var ret = await Cluster.RequestAsync<object?>(id, message, SenderContext, ct);

        return ret switch
        {
            null => Left<Fail, object>(new()),
            Fail m => Left<Fail, object>(m),
            var m => m,
        };
    }
}
