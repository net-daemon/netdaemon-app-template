using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace HelloWorld
{
    /// <summary>
    ///     The NetDaemonApp implements async model API
    ///     currently the default one
    /// </summary>
    public class HelloWorldApp : NetDaemonApp
    {
        public async override Task InitializeAsync()
        {

            Entity("binary_sensor.mypir")
                .WhenStateChange(to: "on")
                .Call(async (entityId, to, from) =>
                {
                    Log("My Pir is doing something");
                    await Entity("light.mylight").TurnOn().ExecuteAsync();
                });

            Log("Hello World!");
        }
    }
}

