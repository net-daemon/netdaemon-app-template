using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Presence
{
    public interface IDateTime
    {
        DateTime Now();
    }
    public class HouseModeImplementation
    {
        private IScheduler? Scheduler { get; }
        private readonly INetDaemonRxApp _app;
        private IDisposable _timer;
        
        public HouseModeImplementation(INetDaemonRxApp app, IScheduler? scheduler = null)
        {
            Scheduler = scheduler;
            _app = app;
        }

        public DateTime Now => Scheduler?.Now.DateTime ?? DateTime.Now;

        public void Initialize()
        {
            _app.CallService("input_select","select_option",new {
                EntityId = "input_select.house_mode",
                Option= "Sleeping"
            });
            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Where(tuple => tuple.New.State == "Master Motion" )
                .Subscribe(s =>
                {
                    _timer?.Dispose();
                    _timer = _app.RunIn(TimeSpan.FromMinutes(5), () => _app.Entity("input_select.house_mode").SetState("Night"));
                });

            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Where(tuple => tuple.New.State == "Landing Motion")
                .Subscribe(s => 
                {
                    if (Now.Hour >=5 && Now.Hour < 7)
                        _app.Entity("input_select.house_mode").SetState("Morning");
                    if (Now.Hour >= 7 && _app.State("input_select.house_mode")?.State == "Morning")
                        _app.Entity("input_select.house_mode").SetState("Day");
                });
        }
    }
}
