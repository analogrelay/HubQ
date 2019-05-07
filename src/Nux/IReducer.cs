using System;
using System.Collections.Generic;
using System.Text;

namespace Nux
{
    /// <summary>
    /// Provides an interface to a Nux Reducer.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    public interface IReducer<TState>
    {
        /// <summary>
        /// Applies the specified action to the current state. Returns a boolean indicating if the action
        /// results in new state, and yields the new state as an out-parameter.
        /// </summary>
        /// <param name="current">The current value of the state.</param>
        /// <param name="action">The action to apply.</param>
        /// <param name="store">The <see cref="IStore{TState}"/> in which the state is stored.</param>
        /// <param name="newState">The new state, or the value of <paramref name="current"/> if no new state is produced.</param>
        /// <returns>A boolean indicating if new state is produced.</returns>
        bool Reduce(TState current, object action, IStore<TState> store, out TState newState);
    }
}
