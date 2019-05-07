namespace Nux
{
    public abstract class Reducer<TState> : IReducer<TState> where TState : class
    {
        public bool Reduce(TState current, object action, IStore<TState> store, out TState newState)
        {
            newState = Reduce(current, action, store);
            return !ReferenceEquals(newState, current);
        }

        protected abstract TState Reduce(TState current, object action, IStore<TState> store);
    }
}
