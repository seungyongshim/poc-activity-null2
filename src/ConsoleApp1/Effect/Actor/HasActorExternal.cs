using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using Proto;
using static LanguageExt.Prelude;

namespace Aums.Effect.Actor;

[Typeclass("*")]
public interface HasActorExternal<RT> : HasCancel<RT> where RT : struct, HasActorExternal<RT>
{
    Eff<RT, ActorExternalIO> ActorAff => Eff<RT, ActorExternalIO>(static rt => new(rt.Root));
    IRootContext Root { get; }
}
