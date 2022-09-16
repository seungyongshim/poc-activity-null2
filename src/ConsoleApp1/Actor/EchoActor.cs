using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;

namespace ConsoleApp1.Actor;

public class EchoActor : IActor
{
    public Task ReceiveAsync(IContext context) => context.Message switch
    {
        string m => Task.Run(() =>
        {
            context.Respond("world");
            Console.WriteLine($"9:{Activity.Current?.TraceId}");
            return Task.CompletedTask;
        }),
        _ => Task.CompletedTask,
    };
}
