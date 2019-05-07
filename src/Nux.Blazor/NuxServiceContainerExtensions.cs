using Nux;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NuxServiceContainerExtensions
    {
        public static void AddNux<TState, TReducer>(this IServiceCollection services)
            where TReducer: class, IReducer<TState>
            where TState: class, new() => AddNux<TState, TReducer>(services, new TState());

        public static void AddNux<TState, TReducer>(this IServiceCollection services, TState initialState)
            where TReducer: class, IReducer<TState>
        {
            // In Server-Side Blazor we probably need to use Scoped to keep this per-connection...
            // Not sure how to detect that though.
            services.AddSingleton<IReducer<TState>, TReducer>();
            services.AddSingleton<IStore<TState>>((services) =>
            {
                var reducer = services.GetRequiredService<IReducer<TState>>();
                return new Store<TState>(reducer, initialState);
            });
        }
    }
}
