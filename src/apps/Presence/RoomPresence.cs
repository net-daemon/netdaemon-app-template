using System;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Octopus
{
    /// <summary>
    ///     The NetDaemonApp implements System.Reactive API
    ///     currently the default one
    /// </summary>
    public class RoomPresence : NetDaemonRxApp
    {
        public override void Initialize()
        {
            LogInformation($"Starting RoomPresence");

            Entity("binary_sensor.my_motion_sensor")
                .StateChanges
                .Where(e => e.Old?.State != "off" && e.New?.State == "off")
                .NDSameStateFor(TimeSpan.FromMinutes(10))
                .Subscribe(s => Entity("light.light1").TurnOff());

        }
    }
}
