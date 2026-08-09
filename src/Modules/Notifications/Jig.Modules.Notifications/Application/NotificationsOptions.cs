using System.ComponentModel.DataAnnotations;

namespace Jig.Modules.Notifications.Application;

internal sealed class NotificationsOptions
{
    [Required(AllowEmptyStrings = false)]
    public string WelcomeMessageFormat { get; set; } = "";
}
