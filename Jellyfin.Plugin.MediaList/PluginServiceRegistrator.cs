using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.MediaList
{
    /// <summary>
    /// Register MediaList services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
           serviceCollection.AddSingleton<MediaListManager>();
           serviceCollection.AddSingleton<MdbClientManager>();
        }
    }
}