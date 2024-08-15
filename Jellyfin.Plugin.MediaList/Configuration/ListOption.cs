using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaList.Configuration;

/// <summary>
/// The list options.
/// </summary>
public class ListOption
{
    [Required]
    public string? Name { get; set; }
    [Required]
    public string? Url { get; set; }
}
