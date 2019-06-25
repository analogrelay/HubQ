using VibrantCode.HubQ.Web.Nux;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NuxServiceCollectionExtensions
    {
        public static INuxBuilder AddNux<TState, TReducer>(this IServiceCollection services)
            where TReducer: class, IReducer<TState>
        {
            services.AddSingleton<StoreFactory<TState>>();
            services.AddSingleton<IReducer<TState>, TReducer>();
            return new NuxBuilder(services);
        }

        public static INuxBuilder AddMiddleware<TMiddle>(this INuxBuilder builder)
            where TMiddle: class, IMiddleware
        {
            builder.Services.AddSingleton<IMiddleware, TMiddle>();
            return builder;
        }

        private class NuxBuilder : INuxBuilder
        {
            public NuxBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
