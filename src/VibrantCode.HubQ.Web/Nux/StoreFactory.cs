using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace VibrantCode.HubQ.Web.Nux
{
    /// <summary>
    /// A singleton service that can be used to construct a <see cref="Store"/>. Designed to simplify
    /// creating a store from a reducer in the DI container.
    /// </summary>
    /// <remarks>
    /// The lifetime of the store depends heavily on if client- or server-side Blazor are in use. In
    /// client-side Blazor, the store can be a singleton. However, in server-side Blazor the store must
    /// be created and retained by the component instances themselves so that they don't interfere with
    /// each other.
    /// </remarks>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class StoreFactory<TState>
    {
        private readonly IReducer<TState> _reducer;
        private readonly ILoggerFactory _loggerFactory;

        public StoreFactory(IReducer<TState> reducer, ILoggerFactory loggerFactory)
        {
            _reducer = reducer;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Creates a new <see cref="Store{TState, TAction}"/> that checks state equality based on
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </summary>
        /// <param name="initialState">The initial state for the application.</param>
        /// <returns>A new <see cref="Store{TState, TAction}"/></returns>
        public Store<TState> CreateStore(TState initialState)
            => CreateStore(initialState, EqualityComparer<TState>.Default);

        /// <summary>
        /// Creates a new <see cref="Store{TState, TAction}"/> that checks state equality based on
        /// the provided <paramref name="equalityComparer" />
        /// </summary>
        /// <param name="initialState">The initial state for the application.</param>
        /// <returns>A new <see cref="Store{TState, TAction}"/></returns>
        public Store<TState> CreateStore(TState initialState, IEqualityComparer<TState> equalityComparer)
        {
            return new Store<TState>(initialState, _reducer, equalityComparer, _loggerFactory.CreateLogger<Store<TState>>());
        }
    }
}
