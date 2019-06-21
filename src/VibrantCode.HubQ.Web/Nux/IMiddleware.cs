using System;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Nux
{
    /// <summary>
    /// Provides an interface to Nux middleware.
    /// </summary>
    /// <remarks>
    /// Middleware run on every dispatch, before the action is received by the reducer.
    /// </remarks>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public interface IMiddleware<TState>
    {
        void Invoke(Store<TState> store, IAction action, Action<IAction> next);
    }

    /// <summary>
    /// Base class for middleware that allows the action to continue but triggers an asychronous
    /// side-effect that can dispatch more actions.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public abstract class SideEffectMiddleware<TState> : IMiddleware<TState>
    {
        public void Invoke(Store<TState> store, IAction action, Action<IAction> next)
        {
            // Launch the side-effect
            _ = ExecuteSideEffectAsync(action, store);

            next(action);
        }

        protected abstract Task ExecuteSideEffectAsync(IAction action, IStoreDispatcher dispatcher);
    }
}
