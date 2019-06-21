using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Nux
{
    public interface IStoreDispatcher
    {
        void Dispatch(IAction action);
    }
}
