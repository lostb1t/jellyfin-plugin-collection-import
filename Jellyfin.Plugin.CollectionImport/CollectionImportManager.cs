#pragma warning disable CA2007
#pragma warning disable CA1861

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.CollectionImport.Configuration;
using System.Security.Cryptography.X509Certificates;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.CollectionImport;

public class CollectionImportManager
{
    private readonly ILogger<CollectionImportManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly IFileSystem _fileSystem;
    private readonly IItemRepository _itemRepo;
    private readonly ILibraryManager _libraryManager;
    private readonly MdbClientManager _mdbClientManager;
    private readonly ICollectionManager _collectionManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly IUserManager _userManager;

    private readonly User _adminUser;

    public CollectionImportManager(
        ILogger<CollectionImportManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        IPlaylistManager playlistManager,
        IUserManager userManager,
        MdbClientManager mdbClientManager)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _playlistManager = playlistManager;
        _userManager = userManager;
        _mdbClientManager = mdbClientManager;
        _adminUser = _userManager.Users
            .Where(i => i.HasPermission(PermissionKind.IsAdministrator))
            .First();
    }

    private async Task<IEnumerable<Guid>> GetItemIdsFromMdb(ImportSet set, IEnumerable<BaseItem> dbItems)
    {
        IEnumerable<IEnumerable<Guid>> idSets = Array.Empty<IEnumerable<Guid>>();

        foreach (string url in set.Urls)
        {
            var items = await _mdbClientManager.Request(url.TrimEnd(new Char[] { '/' }) + "/json");
            var providerIds = new List<string>();

            foreach (MdbItem i in items)
            {
                if (i.Imdb_id is not null)
                {
                    providerIds.Add(i.Imdb_id);
                }
            }

#pragma warning disable CS8604 // Possible null reference argument.
            var LocalItems = dbItems
                .Where(i => providerIds.Contains(i.GetProviderId(MetadataProvider.Imdb)))
                .OrderBy(i => providerIds.IndexOf(i.GetProviderId(MetadataProvider.Imdb)));
            idSets = idSets.Append(LocalItems.GroupBy(i => providerIds.IndexOf(i.GetProviderId(MetadataProvider.Imdb))).Select(group=>group.First().Id).ToList());
#pragma warning restore CS8604 // Possible null reference argument.

        }
        return Interleave(idSets);
    }

    private async Task SetPhotoForCollection(BoxSet collection)
    {
        try
        {
            var query = new InternalItemsQuery
            {
                Recursive = true
            };

            var items = collection.GetItems(query)
                .Items
                .Where(item => item is Movie || item is Series)
                .ToList();

            _logger.LogDebug("Found {Count} items in collection {CollectionName}",
                items.Count, collection.Name);

            var firstItemWithImage = items
                .FirstOrDefault(item =>
                    item.ImageInfos != null &&
                    item.ImageInfos.Any(i => i.Type == ImageType.Primary));

            if (firstItemWithImage != null)
            {
                var imageInfo = firstItemWithImage.ImageInfos
                    .First(i => i.Type == ImageType.Primary);

                // Simply set the image path directly
                collection.SetImage(new ItemImageInfo
                {
                    Path = imageInfo.Path,
                    Type = ImageType.Primary
                }, 0);

                await _libraryManager.UpdateItemAsync(
                    collection,
                    collection.GetParent(),
                    ItemUpdateType.ImageUpdate,
                    CancellationToken.None);
                _logger.LogInformation("Successfully set image for collection {CollectionName} from {ItemName}",
                    collection.Name, firstItemWithImage.Name);
            }
            else
            {
                _logger.LogWarning("No items with images found in collection {CollectionName}. Items: {Items}",
                    collection.Name,
                    string.Join(", ", items.Select(i => i.Name)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting image for collection {CollectionName}",
                collection.Name);
        }
    }

    public async Task SyncCollection(ImportSet set, IEnumerable<BaseItem> dbItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing {Name}", set.Name);
        if (set.Urls.Length == 0)
        {
            return;
        }
        var collection = GetBoxSetByName(set.Name);
        var created = false;
        if (collection is null)
        {
            _logger.LogInformation("{Name} not found, creating.", set.Name);
            collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = set.Name,
                IsLocked = true
            });
            collection.Tags = new[] { "collectionimport" };
            collection.DisplayOrder = "Default";
            created = true;
        }

        collection.DisplayOrder = "Default";

        var filteredDbItems = dbItems.Where(item => !set.ExcludedLibraries.Contains(item.Path));
        var allowedItemKind = new[] { BaseItemKind.TvProgram, BaseItemKind.Movie };
        filteredDbItems = filteredDbItems.Where(item => allowedItemKind.Contains(item.GetBaseItemKind()));

        var ids = await GetItemIdsFromMdb(set, filteredDbItems);

        // we need to clear it first, otherwise sorting is not applied.
        var children = collection.GetChildren(_adminUser, true);
        await _collectionManager.RemoveFromCollectionAsync(collection.Id, children.Select(i => i.Id)).ConfigureAwait(true);

        await _collectionManager.AddToCollectionAsync(collection.Id, ids).ConfigureAwait(true);
        if (created) {
            await SetPhotoForCollection(collection);
        }
    }

    public async Task SyncPlaylist(ImportSet set, IEnumerable<BaseItem> dbItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing {Name}", set.Name);
        if (set.Urls.Length == 0)
        {
            return;
        }
        var playlist = GetPlaylistByName(set.Name);
        if (playlist is null)
        {
            _logger.LogInformation("{Name} not found, creating.", set.Name);

            var playlistId = (await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = set.Name,
                Public = true,
                UserId = _adminUser.Id
            })).Id;

            if (Guid.TryParse(playlistId, out Guid playlistGuid))
            {
                playlist = _playlistManager.GetPlaylistForUser(playlistGuid, _adminUser.Id);
                playlist.Tags = new[] { "collectionimport" };
            }
        }

        var filteredDbItems = dbItems.Where(item => !set.ExcludedLibraries.Contains(item.Path));
        var ids = await GetItemIdsFromMdb(set, filteredDbItems);

        // we need to clear it first, otherwise sorting is not applied.
        if (playlist is not null)
        {
            var children = playlist.GetChildren(_adminUser, true);
            await _playlistManager.RemoveItemFromPlaylistAsync(playlist.Id.ToString(), children.Select(i => i.Id.ToString())).ConfigureAwait(true);

            await _playlistManager.AddItemToPlaylistAsync(playlist.Id, ids.ToList(), _adminUser.Id).ConfigureAwait(true);
        }
    }

    public async Task Sync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing lists");
        var collections = CollectionImportPlugin.Instance!.Configuration.ImportSets;

        // i have no idea howto query for imdbid at this point so so it the slow way for now.
        var dbItems = _itemRepo.GetItemList(new InternalItemsQuery
        {
            HasImdbId = true
        }).Where(i => !string.IsNullOrEmpty(i.GetProviderId(MetadataProvider.Imdb)));

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 20
        };

        //double percent = 100;
        var done = 0;
        await Parallel.ForEachAsync(collections, options, async (i, ct) =>
        {
            if (i.UsePlaylistsOverCollections)
            {
                await SyncPlaylist(i, dbItems, ct);
            }
            else
            {
                await SyncCollection(i, dbItems, ct);
            }
            done++;
            progress.Report(Convert.ToDouble(100 / collections.Length * done));
        });
    }

    public BoxSet? GetBoxSetByName(string name)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true,
            Tags = new[] { "collectionimport" },
            Name = name,
        }).Select(b => b as BoxSet).FirstOrDefault();
    }

    public Playlist? GetPlaylistByName(string name)
    {
        return _playlistManager.GetPlaylists(_adminUser.Id)
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public ICollection<BoxSet?> GetBoxSetByIds(Guid[] ids)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true,
            Tags = new[] { "collectionimport" },
            ItemIds = ids,
        }).Select(b => b as BoxSet).ToList();
    }

    public ICollection<BoxSet?> GetAllBoxSetsFromLibrary()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true,
        }).Select(b => b as BoxSet).ToList();
    }
    private static IEnumerable<T> Interleave<T>(IEnumerable<IEnumerable<T>> source)
    {
        var queues = source.Select(x => new Queue<T>(x)).ToList();
        while (queues.Any(x => x.Count != 0))
        {
            foreach (var queue in queues.Where(x => x.Count != 0))
            {
                yield return queue.Dequeue();
            }
        }
    }
}

