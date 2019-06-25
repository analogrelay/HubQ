using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace VibrantCode.HubQ.Web.Nux
{
    /// <summary>
    /// A singleton service that can be used to construct a <see cref="Store"/>. Designed to simplify
    /// creating a store from a reducer in the DI container.
    /// </summary>
    /// <remarks>
    /// The lifetime of the store depends on if client- or server-side Blazor are in use. In
    /// client-side Blazor, the store can be a singleton. However, in server-side Blazor the store must
    /// be created and retained by the component instances themselves so that they don't interfere with
    /// each other.
    /// </remarks>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class StoreFactory<TState>
    {
        private readonly IReducer<TState> _reducer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Action<IStoreDispatcher, IAction, Action<IAction>> _compiledMiddleware;

        public StoreFactory(IReducer<TState> reducer, ILoggerFactory loggerFactory, IEnumerable<IMiddleware> middlewares)
        {
            _reducer = reducer;
            _loggerFactory = loggerFactory;

            var middlewareList = middlewares.ToList();
            middlewareList.Reverse();
            _compiledMiddleware = CompileMiddleware(middlewareList);
        }

        /// <summary>
        /// Creates a new <see cref="Store{TState}"/> that checks state equality based on
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </summary>
        /// <param name="initialState">The initial state for the application.</param>
        /// <returns>A new <see cref="Store{TState}"/></returns>
        public Store<TState> CreateStore(TState initialState)
            => CreateStore(initialState, EqualityComparer<TState>.Default);

        /// <summary>
        /// Creates a new <see cref="Store{TState}"/> that checks state equality based on
        /// the provided <paramref name="equalityComparer" />
        /// </summary>
        /// <param name="initialState">The initial state for the application.</param>
        /// <returns>A new <see cref="Store{TState}"/></returns>
        public Store<TState> CreateStore(TState initialState, IEqualityComparer<TState> equalityComparer)
        {
            return new Store<TState>(initialState, _reducer, _compiledMiddleware, equalityComparer, _loggerFactory.CreateLogger<Store<TState>>());
        }

        private Action<IStoreDispatcher, IAction, Action<IAction>> CompileMiddleware(IEnumerable<IMiddleware> middlewares)
        {
            Action<IStoreDispatcher, IAction, Action<IAction>> current = (dispatcher, action, next) => next(action);
            foreach (var middleware in middlewares)
            {
                var nextMiddleware = current;
                current = (dispatcher, action, next) =>
                {
                    middleware.Invoke(dispatcher, action, (a) => nextMiddleware(dispatcher, a, next));
                };
            }
            return current;
        }
    }
}
