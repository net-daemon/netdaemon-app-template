using System.Threading.Tasks;
using HelloWorld;
using NetDaemon.Daemon.Fakes;
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
    public async Task CallServiceShouldCallCorrectFunction()
    {
        // Add the instance of app that we run tests on
        // This need always need to be first operation
        await AddAppInstance(new HelloWorldApp());

        // Init the fake NetDaemon
        await InitializeFakeDaemon().ConfigureAwait(false);

        // Add change event to simulate update in state
        AddChangedEvent("binary_sensor.mypir", "off", "on");

        // Process events and messages in fake Daemon until default timeout
        await RunFakeDaemonUntilTimeout().ConfigureAwait(false);

        // Verify that light is turned on
        VerifyCallService("light", "turn_on", "light.mylight");
    }
}
