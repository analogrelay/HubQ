using System;
using Microsoft.AspNetCore.Components;

namespace VibrantCode.HubQ.Web.Nux
{
    public abstract class NuxComponentBase<TState> : ComponentBase, IDisposable
    {
        [CascadingParameter]
        protected Store<TState> Store { get; set; } = default!;

        protected TState State => Store.CurrentState;

        protected override void OnInit()
        {
            base.OnInit();

            if(Store == null)
            {
                throw new InvalidOperationException("Unable to find a Store instance. The root of the app must be contained in a 'Provider' component.");
            }

            Store.StateChanged += Store_StateChanged;
        }

        protected void Dispatch(IAction action)
        {
            Store.Dispatch(action);
        }

        public void Dispose()
        {
            // Unhook our event
            Store.StateChanged -= Store_StateChanged;
        }

        private void Store_StateChanged(object sender, EventArgs e)
        {
            // Re-render the component.
            StateHasChanged();
        }
    }
}
