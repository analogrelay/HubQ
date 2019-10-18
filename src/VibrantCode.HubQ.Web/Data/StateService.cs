using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace VibrantCode.HubQ.Web.Data
{
    public class StateService
    {
        private readonly ILocalStorageService _localStorage;

        public StateService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<AppState> GetStateAsync()
        {
            var state = await _localStorage.GetItemAsync<AppState>("state");
            if(state == null)
            {
                state = new AppState();
                await SaveStateAsync(state);
            }
            return state;
        }

        public async Task SaveStateAsync(AppState state)
        {
            await _localStorage.SetItemAsync("state", state);
        }
    }
}
