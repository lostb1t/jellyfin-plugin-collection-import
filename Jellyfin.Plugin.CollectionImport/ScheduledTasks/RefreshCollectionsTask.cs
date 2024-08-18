using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionImport;

/// <summary>
/// The "Refresh Guide" scheduled task.
/// </summary>
public class RefreshCollectionsTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly CollectionImportManager _collectionImportManager;
    private readonly IConfigurationManager _config;

    public RefreshCollectionsTask(
        CollectionImportManager collectionImportManager,
        IConfigurationManager config)
    {
        _collectionImportManager = collectionImportManager;
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
     => _collectionImportManager.Sync(progress, cancellationToken);
       //return Task.CompletedTask;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(8).Ticks
            }
        };
    }
}