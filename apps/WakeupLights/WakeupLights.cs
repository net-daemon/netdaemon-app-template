using System.Collections.Generic;
using NetDaemon.Common.Reactive;

namespace WakeupSimulator
{
    public class WakeupLights : NetDaemonRxApp
    {
        private readonly WakeupLightsImplementation _app;
        
        public WakeupLights()
        {
            _app = new WakeupLightsImplementation(this);
        }

        public override void Initialize()
        {
            //_app.Initialize();
        }
    }
}