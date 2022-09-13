using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using Proto;
using Proto.Cluster;
using static LanguageExt.Prelude;

namespace Aums.Effect.Actor;

[Typeclass("*")]
public interface HasActorCluster<RT> : HasCancel<RT> where RT : struct, HasActorCluster<RT>
{
    Eff<RT, ActorClusterIO> ActorEff => Eff<RT, ActorClusterIO>(static rt => new (rt.Root.System.Cluster(), rt.Root));
    IRootContext Root { get; }
}
