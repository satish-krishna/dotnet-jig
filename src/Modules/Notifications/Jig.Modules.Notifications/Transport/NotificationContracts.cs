namespace Jig.Modules.Notifications.Transport;

internal sealed record UserNotificationsResponse(string UserName, IReadOnlyList<string> Messages);
