using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;

namespace ConsoleApp1.Actor;

public class EchoActor : IActor
{
    public Task ReceiveAsync(IContext context) => throw new NotImplementedException();
}
