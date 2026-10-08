namespace Habbo_Downloader.App.Operations;

public enum OperationCategory
{
    HabboOriginal,
    NitroCustom,
    HotelTools,
    Database,
    General
}

public sealed record OperationDefinition(
    string Id,
    OperationCategory Category,
    string Title,
    string Description,
    Func<Task> Action,
    bool RequiresInput = false,
    bool IsDestructive = false);

public sealed record OperationResult(
    bool Succeeded,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    Exception? Error = null);
