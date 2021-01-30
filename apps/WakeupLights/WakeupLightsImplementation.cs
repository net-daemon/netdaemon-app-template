using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NetDaemon.Common.Reactive;

namespace WakeupSimulator
{
    /// <summary>
    ///     The NetDaemonApp implements System.Reactive API
    ///     currently the default one
    /// </summary>
    public class WakeupLightsImplementation
    {
        private INetDaemonRxApp _app;
        private List<string> Lights = new() { "light.master_1", "light.master_2", "light.master_4", "light.master_3" };
        private decimal transition = 300m;
        private string startTime = "07:00:00";

        public WakeupLightsImplementation(INetDaemonRxApp app)
        {
            _app = app;
        }

        public void Initialize()
        {
            _app.LogInformation($"Starting WakeupLights");
            _app.RunDaily(startTime, Sequence);
            //Sequence();
        }

        private void Sequence()
        {
            for (var i = 1; i <= 4; i++)
            {
                _app.Entity($"light.master_{i}").TurnOn(new { brightness_pct = 1 });
                _app.Delay(TimeSpan.FromSeconds(1));
                _app.Entity($"light.master_{i}").TurnOn(new { kelvin = 2200 });
                _app.Delay(TimeSpan.FromSeconds(15));
            }

            for (var i = 1; i <= transition; i++)
            {
                var cfg_brightness_pct = 100m;
                var cfg_kelvin = 6000m;

                var now_brightness_pct = Convert.ToInt32( 0 + (cfg_brightness_pct / transition) * i);
                var now_kelvin = Convert.ToInt32(2200 + ((cfg_kelvin-2200) / transition) * i);


                _app.LogInformation($"Brightness {now_brightness_pct} - Kelvin {now_kelvin}");
                _app.Entity("light.master").TurnOn(new {brightness_pct = now_brightness_pct });
                _app.Delay(TimeSpan.FromMilliseconds(1000));
                _app.Entity("light.master").TurnOn(new {kelvin = now_kelvin });
                _app.Delay(TimeSpan.FromMilliseconds(1000));
            }
        }
    }

}