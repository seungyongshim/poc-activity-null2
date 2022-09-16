using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using Proto;
using static LanguageExt.Prelude;

namespace Aums.Effect.Actor;

[Typeclass("*")]
public interface HasActorInternal<RT> : HasCancel<RT> where RT : struct, HasActorInternal<RT>
{
    Eff<RT, ActorInternalIO> ActorEff => Eff<RT, ActorInternalIO>(static rt => new ActorInternalIO(rt.Context));
    public IContext Context { get; }
}
