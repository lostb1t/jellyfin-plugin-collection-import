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
    private readonly IUserManager _userManager;

    private readonly User _adminUser;

    public CollectionImportManager(
        ILogger<CollectionImportManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        IUserManager userManager,
        MdbClientManager mdbClientManager)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _userManager = userManager;
        _mdbClientManager = mdbClientManager;
        _adminUser = _userManager.Users
            .Where(i => i.HasPermission(PermissionKind.IsAdministrator))
            .First();
    }

    public async Task SyncCollection(ImportSet set, IEnumerable<BaseItem> dbItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing {Name}.", set.Name);
        if (set.Urls.Length == 0)
        {
            return;
        }
        var collection = GetBoxSetByName(set.Name);
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
        }

        collection.DisplayOrder = "Default";
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
#pragma warning restore CS8604 // Possible null reference argument.
            idSets = idSets.Append(LocalItems.Select(c => c.Id).ToList());

        }
        var ids = Interleave(idSets);
        var children = collection.GetChildren(_adminUser, false);

        // we need to clear it first, otherwise sorting is not applied.
        await _collectionManager.RemoveFromCollectionAsync(collection.Id, children.Select(i => i.Id));
        await _collectionManager.AddToCollectionAsync(collection.Id, ids);

        collection.OnMetadataChanged();
        await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task Sync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing collections");
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
            await SyncCollection(i, dbItems, ct);
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

