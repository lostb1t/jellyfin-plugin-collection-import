#pragma warning disable SA1611
#pragma warning disable SA1591
#pragma warning disable SA1615

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.MediaList.Configuration;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaList.Api;

/// <summary>
/// MediaListController.
/// </summary>
[ApiController]
[Authorize(Policy = "DefaultAuthorization")]
[Route("medialists")]
[Produces(MediaTypeNames.Application.Json)]
public class MediaListController : ControllerBase
{
    private readonly ILogger<MediaListController> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _config;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaListController"/> class.
    /// </summary>
    public MediaListController(
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IServerConfigurationManager config,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MediaListController>();
        _fileSystem = fileSystem;
        _config = config;
        _userManager = userManager;
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
    [HttpGet("home")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // public ActionResult<Dictionary<string, string>> Home()
    public ActionResult<QueryResult<BaseItemDto>> Home([FromQuery] Guid? userId)
    {
        var isApiKey = User.GetIsApiKey();
        // if api key is used (auth.IsApiKey == true), then `user` will be null throughout this method
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        // beyond this point, we're either using an api key or we have a valid user
        if (!isApiKey && user is null)
        {
            return BadRequest("userId is required");
        }
        var query = new InternalItemsQuery(user) { };
        result = folder.GetItems(query);
        return new QueryResult<BaseItemDto>(
            startIndex,
            result.TotalRecordCount,
            _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user));
    }
}
