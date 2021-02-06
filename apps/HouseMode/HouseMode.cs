using NetDaemon.Common.Reactive;

namespace Presence
{
    public class HouseMode : NetDaemonRxApp
    {
        public override void Initialize()
        {
            LogInformation($"Initialise House Mode");
            var app = new HouseModeImplementation(this);
            app.Initialize();
        }
    }
}