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
        ImportSets = new[]
                {
                 new ImportSet {
                   Name = "Trending",
                   Urls = new [] {"https://mdblist.com/lists/adamosborne01/hmmmmmmmm","https://mdblist.com/lists/adamosborne01/trending-shows1"} },
                };
    }

    [SuppressMessage(category: "Performance", checkId: "CA1819", Target = "ArtworkRepos", Justification = "Xml Serializer doesn't support IReadOnlyList")]
    public ImportSet[] ImportSets { get; set ; }

        // public ImportSet[] ImportSets { 
        // get;
        // set
        // {
        //     if (!String.IsNullOrEmpty(value))
        //         _myValue = value;
        //     }
        // }
}
