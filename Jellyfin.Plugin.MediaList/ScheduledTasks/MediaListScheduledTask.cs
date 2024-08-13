using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.MediaList;

/// <summary>
/// The "Refresh Guide" scheduled task.
/// </summary>
public class MediaListScheduledTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly MediaListManager _mediaListManager;
    private readonly IConfigurationManager _config;

    public MediaListScheduledTask(
        MediaListManager mediaListManager,
        IConfigurationManager config)
    {
        _mediaListManager = mediaListManager;
        _config = config;
    }

    /// <inheritdoc />
    public string Name => "Super collections";

    /// <inheritdoc />
    public string Description => "";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public string Key => "RefreshCollections";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
     => _mediaListManager.Sync(cancellationToken);
       //return Task.CompletedTask;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        };
    }
}