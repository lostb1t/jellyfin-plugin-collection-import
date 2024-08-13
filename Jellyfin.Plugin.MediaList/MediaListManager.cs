#pragma warning disable CA2007

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaList;

public class MediaListManager
{
    private readonly ILogger<MediaListManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly IFileSystem _fileSystem;
    private readonly IItemRepository _itemRepo;
    private readonly ILibraryManager _libraryManager;
  
    public MediaListManager(
        ILogger<MediaListManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
    }

    public async Task Sync(CancellationToken cancellationToken) 
    {
      _logger.LogDebug("Refreshing collections");
      await Task.CompletedTask;
      //return true;
    }
}