using System.Threading.Tasks;
using NetDaemon.Common;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace HelloWorld
{
    /// <summary>
    ///     The NetDaemonApp implements async model API
    ///     This API are deprecated please use the Rx one!
    /// </summary>
    public class HelloWorldApp : NetDaemonApp
    {
        public override async Task InitializeAsync()
        {
            Entity("binary_sensor.mypir")
                .WhenStateChange("on")
                .Call(async (entityId, to, from) =>
                {
                    Log("My Pir is doing something");
                    await Entity("light.mylight").TurnOn().ExecuteAsync();
                });

            Log("Hello World!");
        }
    }
}