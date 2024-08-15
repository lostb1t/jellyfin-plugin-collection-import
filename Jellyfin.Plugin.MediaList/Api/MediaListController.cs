#pragma warning disable SA1611
#pragma warning disable SA1591
#pragma warning disable SA1615
#pragma warning disable CS0165

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// using Jellyfin.Api.Attributes;
// using Jellyfin.Api.Extensions;
// using Jellyfin.Api.Helpers;
// using Jellyfin.Api.ModelBinders;
// using Jellyfin.Api.Models.LibraryDtos;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaList.Api;

/// <summary>
/// MediaListController.
/// </summary>
[ApiController]
//[Authorize(Policy = "DefaultAuthorization")]
[Authorize]
[Route("medialist")]
// [Produces(MediaTypeNames.Application.Json)]
public class MediaListController : ControllerBase
{
    private readonly ILogger<MediaListController> _logger;
    private readonly ILoggerFactory _loggerFactory;
    // private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _config;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly MediaListManager _mediaListManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaListController"/> class.
    /// </summary>
    public MediaListController(
        ILoggerFactory loggerFactory,
        // IFileSystem fileSystem,
        IDtoService dtoService,
        IServerConfigurationManager config,
        IUserManager userManager,
        MediaListManager mediaListManager,
        ILibraryManager libraryManager)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MediaListController>();
        // _fileSystem = fileSystem;
        _dtoService = dtoService;
        _config = config;
        _userManager = userManager;
        _mediaListManager = mediaListManager;
        _libraryManager = libraryManager;

        _logger.LogInformation("MediaListController Loaded");
    }

    // public class Data
    // {
    //     public string test { get; set; } = "";
    // }

    /// <summary>
    /// Returns home lists.
    /// </summary>
    [HttpGet("items")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // public ActionResult<Dictionary<string, string>> Home()
    public ActionResult<QueryResult<BaseItemDto>> Home(
        [FromQuery] Guid userId,
        [FromQuery] Guid[] collectionIds,
        [FromQuery] int? limit,
        [FromQuery] BaseItemKind[] includeItemTypes
    //[FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
    //[FromQuery] Guid collectionId
    )
    {
        //var isApiKey = User.GetIsApiKey();
        //var user = HttpContext.User;
        var user = _userManager.GetUserById(userId);
        //var user = null;
        //var user = User.GetUserId();
        //User user = _userManager.GetUserByName(username);
        //var userId = UserManager.GetUserId();

        // var dtoOptions = new DtoOptions { Fields = fields }
        //     .AddClientFields(User);
        //var dtoOptions = null;
        var boxsets = _mediaListManager.GetBoxSetByIds(collectionIds);
        var boxset = boxsets.First();

        QueryResult<BaseItem> result;
        Console.WriteLine(includeItemTypes);
        // if (includeItemTypes.Length == 0
        //     && includeItemTypes[0] == BaseItemKind.BoxSet)
        // {
        //     parentId = null;
        // }

        if (boxset is not null)
        {
            Console.WriteLine("Boxset is not null");
            var items = boxset.GetChildren(user, true, new InternalItemsQuery
            {
                Limit = limit,
                //IncludeItemTypes = includeItemTypes
            });
            result = new QueryResult<BaseItem>(items);
            Console.WriteLine(result.TotalRecordCount);
            //return result;
        }
        else
        {
            Console.WriteLine("Boxset is null");
            return new QueryResult<BaseItemDto>();
        }
        //result = new QueryResult<BaseItem>(itemsArray);
        var startIndex = 0;
        var dtoOptions = new DtoOptions();
        return new QueryResult<BaseItemDto>(
            startIndex,
            result.TotalRecordCount,
            _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user));
        //_dtoService.GetBaseItemDtos(result.Items, null, null));
        // }
        // // if api key is used (auth.IsApiKey == true), then `user` will be null throughout this method
        // userId = RequestHelpers.GetUserId(User, userId);
        // var user = userId.IsNullOrEmpty()
        //     ? null
        //     : _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        // // beyond this point, we're either using an api key or we have a valid user
        // if (!isApiKey && user is null)
        // {
        //     return BadRequest("userId is required");
        // }
        // var query = new InternalItemsQuery(user) { };
        // result = folder.GetItems(query);
        // return new QueryResult<BaseItemDto>(
        //     startIndex,
        //     result.TotalRecordCount,
        //     _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user));
    }
}
