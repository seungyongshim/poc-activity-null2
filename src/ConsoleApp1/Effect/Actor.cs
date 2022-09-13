using System.Diagnostics;
using Aums.Effect.Actor;
using LanguageExt;
using LanguageExt.Common;
using Proto;
using static LanguageExt.Prelude;

namespace ConsoleApp1.Effect;

public static class Actor<RT> where RT : struct, HasActorInternal<RT>
{
    public static Aff<RT, T> MessageMatch<T>(Func<object?, Aff<RT, T>> match) =>
        from actor in default(RT).ActorEff
        from _1 in match(actor.Context.Message)
        select _1;

    public static Eff<RT, Unit> RespondEff(object msg) =>
        default(RT).ActorEff.Map(x => x.Respond(msg));

    public static Aff<RT, T> RespondIfFailAff<T>(Aff<RT, T> aff) =>
        aff | @catch(err =>
            from __ in unitAff
            from _1 in RespondEff(new Fail(err.ToException().ToString()))
            from _2 in FailEff<T>(err)
            select _2);

    public static Eff<RT, Unit> PoisonSelfEff() =>
        from actor in default(RT).ActorEff
        let _1 = actor.PoisonSelf()
        select _1;

    public static Aff<RT, T> RespondResultOrFailAff<T>(Aff<RT, T> aff) =>
        from _1 in RespondIfFailAff(aff)
        from _2 in RespondEff(_1)
        select _1;

    public static Aff<RT, T> RequestAff<T>(PID target, object msg) =>
        from obj in RequestAff(target, msg)
        select (T)obj;

    public static Aff<RT, object> RequestAff(PID target, object msg) =>
        from ct in cancelToken<RT>()
        from actor in default(RT).ActorEff
        from _1 in actor.RequestAsync(target, msg, ct)
                        .ToAsync()
                        .ToAff(e => Error.New(e.Reason ?? ""))
        select _1;
}
