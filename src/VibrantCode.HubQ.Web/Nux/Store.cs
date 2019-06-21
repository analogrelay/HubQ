using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VibrantCode.HubQ.Web.Nux
{
    /// <summary>
    /// Stores the current application state and manages state transition via <see cref="Dispatch"/>.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class Store<TState> : IStoreDispatcher
    {
        private TState _state;
        private readonly IReducer<TState> _reducer;
        private readonly Action<Store<TState>, IAction, Action<IAction>> _compiledMiddleware;
        private readonly IEqualityComparer<TState> _equalityComparer;
        private readonly ILogger _logger;

        public event EventHandler? StateChanged;

        public Store(TState initialState, IReducer<TState> reducer, Func<Store<TState>, IAction, IAction> compiledMiddleware, IEqualityComparer<TState> equalityComparer, ILogger logger)
        {
            _state = initialState;
            _reducer = reducer;
            _compiledMiddleware = compiledMiddleware;
            _equalityComparer = equalityComparer;
            _logger = logger;
        }

        public TState CurrentState => _state;

        /// <summary>
        /// Dispatch the provided action. The active <see cref="IReducer{TState}"/> will
        /// be run to derive a new state. If the state has changed, the <see cref="StateChanged"/>
        /// event will be triggered.
        /// </summary>
        /// <param name="action">The action to dispatch.</param>
        public void Dispatch(IAction action)
        {
            _logger.LogDebug("Reducing state based on action: {Action}", action);
            var newState = _reducer.Reduce(_state, action);
            if(_equalityComparer.Equals(newState, _state))
            {
                _logger.LogDebug("State did not change.");
            }
            else
            {
                _logger.LogDebug("State changed, triggering update.");
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Dispatch using an action creator. The provided function will be executed and provided with
        /// a <see cref="IStoreDispatcher"/> which can be used to dispatch actions to update state as the async
        /// operation progresses.
        /// </summary>
        /// <param name="actionCreator">A function that recieves an <see cref="IStoreDispatcher"/>.</param>
        public void Dispatch(Action<IStoreDispatcher> actionCreator) 
            => actionCreator(this);

        /// <summary>
        /// Dispatch using an action creator. The provided function will be executed and provided with
        /// a <see cref="IStoreDispatcher"/> which can be used to dispatch actions to update state as the async
        /// operation progresses.
        /// </summary>
        /// <remarks>
        /// The <see cref="Task"/> returned by the creator is ignored, this overload is just used to allow the use
        /// of `async Task` methods.
        /// </remarks>
        /// <param name="actionCreator">A asynchronous function that recieves an <see cref="IStoreDispatcher"/>.</param>
        public void Dispatch(Func<IStoreDispatcher, Task> actionCreator)
            => _ = actionCreator(this);
    }
}
