#pragma warning disable CA2227
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.MediaList.Configuration;



/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        //Console.WriteLine("CALLEEDDDDDP");
        Lists = new []
                { 
                 new ListOption {Name = "Trending", Url = "https://mdblist.com/lists/adamosborne01/hmmmmmmmm/json"},
                 new ListOption {Name = "Trending", Url = "https://mdblist.com/lists/adamosborne01/trending-shows1/json"}
                };
    }

    [SuppressMessage(category: "Performance", checkId: "CA1819", Target = "ArtworkRepos", Justification = "Xml Serializer doesn't support IReadOnlyList")]
    public ListOption[] Lists { get; set; }


}
