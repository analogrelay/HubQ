using Microsoft.Extensions.DependencyInjection;
using VibrantCode.HubQ.Web.Nux;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NuxServiceCollectionExtensions
    {
        public static IServiceCollection AddNuxStore<TState, TReducer>(this IServiceCollection services)
            where TReducer: class, IReducer<TState>
        {
            services.AddSingleton<StoreFactory<TState>>();
            services.AddSingleton<IReducer<TState>, TReducer>();
            return services;
        }
    }
}
