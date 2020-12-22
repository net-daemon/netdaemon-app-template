using System;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Apps
{
    /// <summary>
    ///     Hello world showcase
    /// </summary>
    public class HelloWorldApp : NetDaemonRxApp
    {
        public override void Initialize()
        {
            Entity("binary_sensor.mypir").StateChanges
                .Where(e => e.New?.State == "on")
                .Subscribe(
                    e =>
                    {
                        Log("My Pir is doing something");
                        Entity("light.mylight").TurnOn();
                    }
                );
            Log("Hello World");
        }
    }
}
