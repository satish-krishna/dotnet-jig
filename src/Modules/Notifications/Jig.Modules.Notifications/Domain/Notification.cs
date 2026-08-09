using Jig.SharedKernel;

namespace Jig.Modules.Notifications.Domain;

internal record Notification(PseudoKey Id, Guid UserId, string Message);
