using System.Diagnostics;
using ConsoleApp1.Effect;
using LanguageExt;
using Proto;
using static LanguageExt.Prelude;

namespace Aums.Effect.Actor;

public readonly record struct ActorInternalIO(IContext Context) 
{
    public Unit Respond(object msg)
    {
        Context.Respond(msg);
        return Unit.Default;
    }

    public Unit PoisonSelf()
    {
        Context.Send(Context.Self, PoisonPill.Instance);
        return Unit.Default;
    }

    public Unit SetTimeout(TimeSpan time)
    {
        Context.SetReceiveTimeout(time);
        return Unit.Default;
    }

    public async Task<Either<Fail, object>> RequestAsync(PID target, object message, CancellationToken ct)
    {
        Console.WriteLine($"4:{Activity.Current?.TraceId}");
        var ret = await Context.RequestAsync<object?>(target, message, ct);

        Console.WriteLine($"6:{Activity.Current?.TraceId}");

        return ret switch
        {
            null => Left<Fail, object>(new Fail("RequestTimeout")),
            Fail m => Left<Fail, object>(m),
            object m => m,
        };
    }
}
