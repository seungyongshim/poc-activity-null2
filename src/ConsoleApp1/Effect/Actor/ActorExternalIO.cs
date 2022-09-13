using ConsoleApp1.Effect;
using LanguageExt;
using Proto;
using static LanguageExt.Prelude;


namespace Aums.Effect.Actor;

public readonly record struct ActorExternalIO(IRootContext Context) 
{
    public async Task<Either<Fail, object>> RequestAsync(PID target, object message, CancellationToken ct)
    {
        var ret = await Context.RequestAsync<object?>(target, message, ct);

        return ret switch
        {
            null => Left<Fail, object>(new Fail("RequestTimeout")),
            Fail m => Left<Fail, object>(m),
            object m => m,
        };
    }
}
