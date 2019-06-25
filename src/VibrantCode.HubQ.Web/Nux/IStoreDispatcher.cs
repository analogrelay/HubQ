namespace VibrantCode.HubQ.Web.Nux
{
    public interface IStoreDispatcher
    {
        void Dispatch(IAction action);
    }
}
