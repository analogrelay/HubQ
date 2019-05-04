using System;
using System.Collections.Generic;
using System.Text;

namespace VibrantCode.HubQ.Web.Models
{
    public class PagedResponse<T>
    {
        public Hyperlinks Links { get; set; } = default!;
        public IReadOnlyList<T> Data { get; set; } = default!;
    }
}
