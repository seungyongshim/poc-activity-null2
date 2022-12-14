using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Boost.Proto.Actor.DependencyInjection;

public record PropsFactory<T>(IServiceProvider ServiceProvider)
    : IPropsFactory<T> where T : IActor
{
    public Props Create(params object[] args) =>
        Props.FromProducer(() => ActivatorUtilities.CreateInstance<T>(ServiceProvider, args));
}
