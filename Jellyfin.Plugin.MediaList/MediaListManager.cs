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

    public async Task SyncCollection(string name, ICollection<string> urls, IEnumerable<BaseItem> dbItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing {Name}.", name);
        var item = GetBoxSetByName(name);
        if (item is null)
        {
            _logger.LogInformation("{Name} not found, creating.", name);
            item = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = name,
                IsLocked = true,
            });
            item.Tags = new[] { "medialist", "promoted" };
            //item.DisplayOrder = "SortName";
            //item.IsPreSorted = true;
            item.OnMetadataChanged();
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            // clear it
            //item.c
        }
        var item_ids = new List<string>();
        foreach (string url in urls)
        {
            var items = await _mdbClientManager.Request(url.TrimEnd(new Char[] { '/' } ) + "/json");
            
            foreach (MdbItem i in items)
            {
                //Console.WriteLine(i.title);
                if (i.Imdb_id is not null)
                {
                    item_ids.Add(i.Imdb_id);
                }
            }
        }

#pragma warning disable CS8604 // Possible null reference argument.
        var LocalItems = dbItems.Where(i => item_ids.Contains(i.GetProviderId(MetadataProvider.Imdb)));
#pragma warning restore CS8604 // Possible null reference argument.
        // });

        //item.GetProviderId(MetadataProvider.Imdb)
        //Console.WriteLine(LocalItems);

        //GetItemIdsList(InternalItemsQuery query);
        await _collectionManager.AddToCollectionAsync(item.Id, LocalItems.Select(i => i.Id));
    }

    public async Task Sync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing collections");
        //var items = await _mdbClientManager.Request("https://mdblist.com/lists/adamosborne01/trending-shows1/json");
        var lists = MediaListPlugin.Instance!.Configuration.Lists;
        //var grouped = lists.GroupBy(x => new { x.Name, x.Url })
        // .ToList(); 
        var grouped = lists.Where(p => !String.IsNullOrEmpty(p.Url) && !String.IsNullOrEmpty(p.Name)).GroupBy(
    p => p.Name, 
    p => p.Url!,
    (key, g) => new { Name = key!, Urls = g.ToList() }).ToList();
        grouped.ForEach(book => Console.WriteLine(book));
        

        // i have no idea howto query for imdbid at this point so so it the slow way for now.
        var dbItems = _itemRepo.GetItemList(new InternalItemsQuery
        {
            HasImdbId = true
        }).Where(i => !string.IsNullOrEmpty(i.GetProviderId(MetadataProvider.Imdb)));
        
   var options = new ParallelOptions()
    {
        MaxDegreeOfParallelism = 20
    };

    await Parallel.ForEachAsync(grouped, options, async (i, ct) => {
       await SyncCollection(i.Name, i.Urls, dbItems, ct);
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