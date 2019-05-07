using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nux
{
    /// <summary>
    /// A store that uses the specified kind of action objects.
    /// </summary>
    /// <typeparam name="TState">The type of the state value</typeparam>
    public class Store<TState> : IStore<TState>
    {
        private readonly IReducer<TState> _reducer;
        private List<Func<TState, Task>> _subscriptions = new List<Func<TState, Task>>();

        public TState Current { get; private set; }

        public Store(IReducer<TState> reducer, TState initialState = default)
        {
            _reducer = reducer;
            Current = initialState;
        }

        public IDisposable Subscribe(Func<TState, Task> subscription)
        {
            _subscriptions.Add(subscription);

            return Disposable.Create(() => _subscriptions.Remove(subscription));
        }

        public void Dispatch(object action)
        {
            if (!_reducer.Reduce(Current, action, this, out var newState))
            {
                // Nothing changed!
                return;
            }

            Current = newState;

            foreach (var subscription in _subscriptions)
            {
                subscription?.Invoke(newState);
            }
        }
    }
}
