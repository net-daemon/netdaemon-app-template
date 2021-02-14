using System;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Niemand
{
    /// <summary>
    ///     Hello world showcase
    /// </summary>
    public class Lights : NetDaemonRxApp
    {
        public override void Initialize()
        {
            Entity("input_select.house_mode")
                .StateChanges
                .Where(e => e.New?.State?.ToLower() == "sleeping")
                .Subscribe(_ =>
                {
                    Entity("light.downstairs").TurnOff();
                    Entity("light.upstairs").TurnOff();
                });
        }
    }
}
