using Jig.Modules.Notifications.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Jig.Modules.Notifications.Tests;

public class NotificationsOptionsTests
{
    [Fact]
    public void Empty_welcome_format_fails_validation()
    {
        var sp = new ServiceCollection()
            .AddOptions<NotificationsOptions>()
            .Configure(o => o.WelcomeMessageFormat = "")
            .ValidateDataAnnotations()
            .Services
            .BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => sp.GetRequiredService<IOptions<NotificationsOptions>>().Value);
    }
}
