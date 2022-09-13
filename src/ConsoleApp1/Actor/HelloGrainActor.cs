using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aums.Effect.Actor;
using ConsoleApp1.Effect;
using Proto;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using System.Diagnostics;

namespace ConsoleApp1.Actor;

public class HelloGrainActor : IActor
{
    public async Task ReceiveAsync(IContext context)
    {
        Console.WriteLine($"1:{Activity.Current?.TraceId}");

        var q = from ___ in unitEff
                let pid = new PID("nonhost", nameof(EchoActor))
                from __2 in Actor<RT>.MessageMatch<Unit>(msg => msg switch
                {
                    string m => from _1 in unitEff
                                from __ in Eff<RT, Unit>(rt =>
                                {
                                    Console.WriteLine($"2:{Activity.Current?.TraceId}");
                                    return unit;
                                })
                                from _2 in Actor<RT>.RequestAff<string>(pid, m)
                                from _3 in Actor<RT>.RespondEff(_2)
                                select unit,
                    _ => unitAff
                })
                
                select unit;

        using var cts = new CancellationTokenSource();
        await q.Run(new RT(context, cts));
    }

    public readonly record struct RT(IContext Context,
                                     CancellationTokenSource CancellationTokenSource) :
        HasActorInternal<RT>,
        HasDefault<RT>
    {
       
    }

}
