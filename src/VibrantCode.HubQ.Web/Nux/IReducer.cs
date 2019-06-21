namespace VibrantCode.HubQ.Web.Nux
{
    public interface IReducer<TState>
    {
        TState Reduce(TState initialState, IAction action, IStoreDispatcher dispatcher);
    }
}
