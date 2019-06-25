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
    public interface IMiddleware
    {
        void Invoke(IStoreDispatcher dispatcher, IAction action, Action<IAction> next);
    }
}
