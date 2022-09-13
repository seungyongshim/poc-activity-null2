using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aums.Effect.Actor;
using LanguageExt.Effects.Traits;

namespace ConsoleApp1.Effect
{
    public interface HasDefault<RT> : HasCancel<RT>
        where RT : struct, HasDefault<RT>
    {
        RT HasCancel<RT>.LocalCancel => default;
        CancellationToken HasCancel<RT>.CancellationToken => CancellationTokenSource.Token;
    }
}
