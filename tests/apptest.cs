using System;
using System.Threading.Tasks;
using Moq;
using NetDaemon.Daemon.Fakes;
using Presence;
using Xunit;

/// <summary>
///     Tests the fluent API parts of the daemon
/// </summary>
/// <remarks>
///     Mainly the tests checks if correct underlying call to "CallService"
///     has been made.
/// </remarks>
public class AppTests : DaemonHostTestBase
{
    public AppTests() //: base()
    {
    }

    [Fact]
    public async Task LightsDontTurnOnWhenEventStateNewAndOldIsOff()
    {
        // ARRANGE
        const string presenceEntityId = "binary_sensor.my_motion_sensor";
        const string controlEntityId = "light.my_light";
        await AddAppInstance(new RoomPresence(presenceEntityId, controlEntityId, null));

        // ACT
        await InitializeFakeDaemon().ConfigureAwait(false);
        AddChangedEvent(presenceEntityId, "off", "off");
        await RunFakeDaemonUntilTimeout().ConfigureAwait(false);

        // ASSERT
        VerifyCallServiceTimes("light", Times.Never());
    }

    [Fact]
    public async Task LightsTurnOnWhenMotionTriggered()
    {
        // ARRANGE
        const string presenceEntityId = "binary_sensor.my_motion_sensor";
        const string controlEntityId = "light.my_light";
        await AddAppInstance(new RoomPresence(presenceEntityId, controlEntityId, null));

        // ACT
        await InitializeFakeDaemon().ConfigureAwait(false);
        AddChangedEvent(presenceEntityId, "off", "on");
        await RunFakeDaemonUntilTimeout().ConfigureAwait(false);
        
        // ASSERT
        VerifyCallService("light", "turn_on", controlEntityId);
    }

    [Fact]
    public async Task LightsTurnOnWhenMotionTriggeredOnMoreThanOneSensor()
    {
        // ARRANGE
        string[] presenceEntityIds = { "binary_sensor.my_motion_sensor", "binary_sensor.my_motion_sensor_2" };
        string[] controlEntityIds = { "light.my_light" };
        await AddAppInstance(new RoomPresence(presenceEntityIds, controlEntityIds));

        // ACT
        await InitializeFakeDaemon().ConfigureAwait(false);
        AddChangedEvent("binary_sensor.my_motion_sensor", "off", "on");
        AddChangedEvent("binary_sensor.my_motion_sensor_2", "off", "on");
        await RunFakeDaemonUntilTimeout().ConfigureAwait(false);

        // ASSERT
        VerifyCallService("light", "turn_on", "light.my_light");
    }

    [Fact]
    public async Task LightsTurnOffWhenNoPresenceAfterTimeout()
    {
        // ARRANGE
        string presenceEntityId =  "binary_sensor.my_motion_sensor" ;
        string controlEntityId = "light.my_light";
        await AddAppInstance(new RoomPresence(presenceEntityId, controlEntityId, timeout: TimeSpan.FromMilliseconds(100)));

        // ACT
        await InitializeFakeDaemon(timeout: 1000).ConfigureAwait(false);
        AddChangedEvent(presenceEntityId, "off", "on");
        var runFakeDaemonUntilTimeout = RunFakeDaemonUntilTimeout().ConfigureAwait(false);
        await Task.Delay(200);
        AddChangedEvent(presenceEntityId, "on", "off");
        await runFakeDaemonUntilTimeout;

        // ASSERT
        VerifyCallService("light", "turn_off", controlEntityId);
    }

    [Fact]
    public async Task LightsDontTurnOffWhenKeepAliveEnityIsOn()
    {
        // ARRANGE
        string presenceEntityId = "binary_sensor.my_motion_sensor";
        string controlEntityId = "light.my_light";
        string keepAliveEntityId = "binary_sensor.keep_alive";
        await AddAppInstance(new RoomPresence(presenceEntityId, controlEntityId, keepAliveEntityId));

        // ACT
        await InitializeFakeDaemon(timeout: 1000).ConfigureAwait(false);
        SetEntityState(keepAliveEntityId, "on");
        AddChangedEvent(presenceEntityId, "off", "on");
        var runFakeDaemonUntilTimeout = RunFakeDaemonUntilTimeout().ConfigureAwait(false);
        await Task.Delay(200);
        AddChangedEvent(presenceEntityId, "on", "off");
        await runFakeDaemonUntilTimeout;

        // ASSERT
        VerifyCallServiceTimes("light", Times.Never());
    }
}
