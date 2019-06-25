using Microsoft.Extensions.DependencyInjection;

namespace VibrantCode.HubQ.Web.Nux
{
    /// <summary>
    /// An interface to allow extension methods to add Nux-specific services.
    /// </summary>
    public interface INuxBuilder
    {
        IServiceCollection Services { get; }
    }
}
