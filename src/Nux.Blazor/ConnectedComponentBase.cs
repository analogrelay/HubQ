using System;
using Microsoft.AspNetCore.Components;

namespace Nux.Blazor
{
    public abstract class ConnectedComponentBase<TState> : ComponentBase
    {
        [Inject]
        protected IStore<TState> Store { get; set; } = default!;

        protected TState State => Store.Current;

        protected void Dispatch(object action) => Store.Dispatch(action);

        protected Action Act(Func<object> actionCreator) => Store.Act(actionCreator);

        protected override void OnInit()
        {
            Store.Subscribe((state) => StateHasChanged());
            base.OnInit();
        }
    }
}
