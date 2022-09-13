using LanguageExt;
using LanguageExt.Common;
using Proto;
using Aums.Effect.Actor;
using static LanguageExt.Prelude;

namespace ConsoleApp1.Effect;

public static class ActorEx<RT> where RT : struct, HasActorExternal<RT>
{
    public static Aff<RT, object> RequestAff(PID target, object msg, TimeSpan timeout) =>
        from ct in cancelToken<RT>()
        from actor in default(RT).ActorAff
        from _1 in actor.RequestAsync(target, msg, ct)
                        .ToAsync()
                        .ToAff(e => Error.New(e.Reason ?? ""))
        select _1;


}
