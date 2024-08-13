using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaList.Configuration;

/// <summary>
/// The list options.
/// </summary>
public class ListOption
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether default enabled.
    /// </summary>
    public bool DefaultEnabled { get; set; }
}
