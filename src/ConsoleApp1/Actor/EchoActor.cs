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
    public async Task ReceiveAsync(IContext context) 
    {
        Console.WriteLine($"9:{Activity.Current?.TraceId}");
    }
}
