#pragma warning disable CA1051
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.MediaList.Configuration;

/// <summary>
/// The list options.
/// </summary>
public class ImportSet
{
  public ImportSet()
  {
    Urls = Array.Empty<string>();
  }
  [Required]
  public string Name { get; set; } = default!;
  [Required]
  [SuppressMessage(category: "Performance", checkId: "CA1819", Target = "ArtworkRepos", Justification = "Xml Serializer doesn't support IReadOnlyList")]
  public string[] Urls { get; set; }
}
