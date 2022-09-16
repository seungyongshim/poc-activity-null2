using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Boost.Proto.Actor.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AddProtoActor(this IServiceCollection services)
    {
        services.AddSingleton<FuncProps>(sp => _ => _);
        services.AddSingleton<FuncActorSystem>(sp => _ => _);
        services.AddSingleton<FuncActorSystemConfig>(sp => _ => _);
        services.AddSingleton<FuncIRootContext>(sp => _ => _);

        services.AddSingleton(sp =>
        {
            Log.SetLoggerFactory(sp.GetRequiredService<ILoggerFactory>());

            var funcConfig = sp.GetServices<FuncActorSystemConfig>()
                               .Aggregate((x, y) => z => y(x(z)));
            var funcSystem = sp.GetServices<FuncActorSystem>()
                               .Aggregate((x, y) => z => y(x(z)));

            var funcIRootContext = sp.GetServices<FuncIRootContext>()
                                     .Aggregate((x, y) => z => x(y(z)));



            return funcSystem(new(funcConfig(ActorSystemConfig.Setup()
                .WithConfigureRootContext(root => funcIRootContext(root))
#if false
                .WithConfigureProps(props =>
                {
                    var funcProps = sp.GetRequiredService<IEnumerable<FuncProps>>();
                    var func = funcProps.Aggregate((x, y) => z => y(x(z)));
                    return func(props);
                })
#endif
                .WithConfigureSystemProps((name, props) =>
                {
                    var funcProps = sp.GetRequiredService<IEnumerable<FuncProps>>();
                    var func = funcProps.Aggregate((x, y) => z => y(x(z)));
                    return func(props);
                }))));
        });

        services.AddSingleton(typeof(IPropsFactory<>), typeof(PropsFactory<>));
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Root);
        return services;
    }
}
