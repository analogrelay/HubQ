using System;
using System.Threading.Tasks;

namespace Nux
{
    /// <summary>
    /// Provides an interface to a Nux Store.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    public interface IStore<TState>
    {
        /// <summary>
        /// Gets the current value of the state
        /// </summary>
        TState Current { get; }

        /// <summary>
        /// Dispatches an action to be handled by the reducer registered to the store.
        /// </summary>
        /// <param name="action">The action object to dispatch</param>
        void Dispatch(object action);

        /// <summary>
        /// Subscribes to changes to the current state.
        /// </summary>
        /// <param name="subscription">A function which receives the new state and returns a <see cref="Task"/> which will be completed when the subscribed action completes.</param>
        /// <returns>An <see cref="IDisposable"/> that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(Func<TState, Task> subscription);
    }

    public static class StoreExtensions
    {
        /// <summary>
        /// Produces an <see cref="Action"/> that will dispatch the specified action when called. Useful for binding to events.
        /// </summary>
        /// <typeparam name="TState">The type of the state object in the store.</typeparam>
        /// <param name="self">The <see cref="IStore{TState}"/> instance on which to dispatch the action.</param>
        /// <param name="actionCreator">A function that produces an action object.</param>
        /// <returns>An <see cref="Action"/> that will dispatch the specified action when called.</returns>
        public static Action Act<TState>(this IStore<TState> self, Func<object> actionCreator) =>
            () => self.Dispatch(actionCreator());

        /// <summary>
        /// Subscribes to changes to the current state.
        /// </summary>
        /// <param name="subscription">A function which receives the new state and returns a <see cref="Task"/> which will be completed when the subscribed action completes.</param>
        /// <returns>An <see cref="IDisposable"/> that can be disposed to unsubscribe.</returns>
        public static IDisposable Subscribe<TState>(this IStore<TState> self, Action<TState> subscription)
        {
            return self.Subscribe((state) =>
            {
                subscription(state);
                return Task.CompletedTask;
            });
        }
    }
}