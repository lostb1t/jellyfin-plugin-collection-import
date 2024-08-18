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
using Jellyfin.Plugin.MediaList.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace Jellyfin.Plugin.MediaList;

public class MediaListManager
{
    private readonly ILogger<MediaListManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly IFileSystem _fileSystem;
    private readonly IItemRepository _itemRepo;
    private readonly ILibraryManager _libraryManager;
    private readonly MdbClientManager _mdbClientManager;
    private readonly ICollectionManager _collectionManager;

    public MediaListManager(
        ILogger<MediaListManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        MdbClientManager mdbClientManager)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _mdbClientManager = mdbClientManager;
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
            collection.Tags = new[] { "medialist", "promoted", "sf_promoted" };
            collection.DisplayOrder = "Default";
            //item.DisplayOrder = "SortName";
            //item.IsPreSorted = true;
            collection.OnMetadataChanged();
            await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        //Console.WriteLine(test);
        var collectionItems = GetBoxSetChildren(collection.Id);

        //var idSets = Array.Empty<string>();
        //IEnumerable<IEnumerable<Guid>> idSets = new IEnumerable<Guid>[] { };
        IEnumerable<IEnumerable<Guid>> idSets = Array.Empty<IEnumerable<Guid>>();

        foreach (string url in set.Urls)
        {
            //if (url.StartsWith("https://mdblist.com") || url.StartsWith("http://mdblist.com")) {
            var items = await _mdbClientManager.Request(url.TrimEnd(new Char[] { '/' }) + "/json");
            var providerIds = new List<string>();
            //var provider_ids = Dictionary<string, string>;
            //Dictionary<string, string> providerIds = new Dictionary<string, string>();
            //var providerIds = new List<Tuple<string, string>>();

            foreach (MdbItem i in items)
            {
                //Console.WriteLine(i.title);
                if (i.Imdb_id is not null)
                {
                    providerIds.Add(i.Imdb_id);
                    //providerIds.Add("imdb", i.Imdb_id);
                }
            }
            //Console.WriteLine($"providerIds for {url} ----- ");
            //providerIds.ToList().ForEach(x => Console.WriteLine(x.ToString()));
            // var ldbItems = _itemRepo.GetItemList(new InternalItemsQuery
            // {
            //     HasAnyProviderId = providerIds
            // });
#pragma warning disable CS8604 // Possible null reference argument.
            var LocalItems = dbItems
                .Where(i => providerIds.Contains(i.GetProviderId(MetadataProvider.Imdb)))
                .OrderBy(i => providerIds.IndexOf(i.GetProviderId(MetadataProvider.Imdb)));
#pragma warning restore CS8604 // Possible null reference argument.
            idSets = idSets.Append(LocalItems.Select(c => c.Id).ToList());


            //#pragma warning disable CS8604 // Possible null reference argument.
            //var LocalItems = dbItems
            //  .Where(i => itemIds.Contains(i.GetProviderId(MetadataProvider.Imdb)))
            //  .OrderBy(i => itemIds.IndexOf(i.GetProviderId(MetadataProvider.Imdb)));
            //#pragma warning restore CS8604 // Possible null reference argument.

        }
        //Console.WriteLine(ObjectDumper.Dump(idSets));
        var ids = Interleave(idSets);
        //Console.WriteLine("guids  ----- ");
        //ids.ToList().ForEach(x => Console.WriteLine(x.ToString()));
        //Console.WriteLine(ObjectDumper.Dump(ids));
        //));
        //var localItems = new List<BaseItem>;
        // });

        //item.GetProviderId(MetadataProvider.Imdb)
        //Console.WriteLine(LocalItems);

        //GetItemIdsList(InternalItemsQuery query);

        // we need to clear it first, otherwise sorting is not applied.
        await _collectionManager.RemoveFromCollectionAsync(collection.Id, collectionItems.Select(i => i.Id));
        await _collectionManager.AddToCollectionAsync(collection.Id, ids);
        collection.OnMetadataChanged();

        // remove old items
        //item.GetChildren
        //await _collectionManager.AddToCollectionAsync(collection.Id, collectionItems.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToList());
        //collection.OnMetadataChanged();
    }

    public async Task Sync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing collections");
        //var items = await _mdbClientManager.Request("https://mdblist.com/lists/adamosborne01/trending-shows1/json");
        var collections = MediaListPlugin.Instance!.Configuration.ImportSets;

        //    var grouped = lists.Where(p => !String.IsNullOrEmpty(p.Url) && !String.IsNullOrEmpty(p.Name)).GroupBy(
        // p => p.Name, 
        // p => p.Url!,
        // (key, g) => new { Name = key!, Urls = g.ToList() }).ToList();
        //     grouped.ForEach(book => Console.WriteLine(book));


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
        //await grouped.ForEachAsync(async i => await SyncCollection(i.Name, i.Urls, dbItems, cancellationToken));
        //await SyncCollection(
        //    "Trending",
        //    new List<string>
        //        { " https://mdblist.com/lists/adamosborne01/hmmmmmmmm/json",
        //        "https://mdblist.com/lists/adamosborne01/trending-shows1/json"
        //        }
        //, dbItems, cancellationToken);

        //AddToCollectionAsync
        //CreateCollectionAsync

        //var collections = GetAllBoxSetsFromLibrary();
        // var collections = _collectionManager.GetCollections(null).ToList();
        // var trendingBoxSet = GetBoxSet("Trending");
        // if (trendingBoxSet is null) {
        //     trendingBoxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions 
        //     {
        //         Name = "Trending",
        //     });
        // }

        // Console.WriteLine(trendingBoxSet.Name);
        // foreach (BoxSet a in collections.OfType<BoxSet>())
        // {
        //     Console.WriteLine(a.Name);

        //     //await AddMoviesToCollection(movieCollection.Where(m => string.IsNullOrEmpty(m.PrimaryVersionId)).ToList(), tmdbCollectionId, boxSet).ConfigureAwait(false);


        // }

        //return true;
    }

    // private List<CollectionFolder?> GetCollectionsFromLibrary()
    // {
    //     return _libraryManager.GetItemList(new InternalItemsQuery
    //     {
    //         IncludeItemTypes = new[] { BaseItemKind.CollectionFolder },
    //         Recursive = true,
    //     }).Select(b => b as CollectionFolder).Where(i => i is not null).ToList();
    // }

    public BoxSet? GetBoxSetByName(string name)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true,
            Tags = new[] { "medialist" },
            Name = name,
        }).Select(b => b as BoxSet).FirstOrDefault();
    }

    public ICollection<BaseItem> GetBoxSetChildren(Guid id)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            ParentId = id,
        }).ToList();
    }

    public ICollection<BoxSet?> GetBoxSetByIds(Guid[] ids)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true,
            Tags = new[] { "medialist" },
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
    // public List<BaseItem> GetItemsForCollection(BoxSet collection, User, )
    // {
    //     List<BaseItem> children = collection.GetChildren(user, includeLinkedChildren, query);
    //     // return _libraryManager.GetItemList(new InternalItemsQuery
    //     // {
    //     //     IncludeItemTypes = new[] { BaseItemKind.BoxSet },
    //     //     CollapseBoxSetItems = false,
    //     //     Recursive = true,
    //     // }).Select(b => b as BoxSet).ToList();
    // }

}

