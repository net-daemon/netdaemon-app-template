using System;
using System.Reactive.Concurrency;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace HouseModeApp
{
    public class HouseModeImplementation
    {
        private readonly INetDaemonRxApp _app;
        private IDisposable _timer;

        public string LastMotion => _app.State("sensor.template_last_motion")?.State?.ToString() ?? "";

        public DateTime Now => Scheduler?.Now.DateTime ?? DateTime.Now;
        private string HouseModeState => _app.State("input_select.house_mode")?.State?.ToString().ToLower() ?? "";
        private IScheduler? Scheduler { get; }

        public HouseModeImplementation(INetDaemonRxApp app, IScheduler? scheduler = null)
        {
            Scheduler = scheduler;
            _app = app;
        }

        public void Initialize()
        {
            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Subscribe(s => { CalcHouseMode(); });
        }

        private void CalcHouseMode()
        {
            switch (Now.Hour)
            {
                case < 7 or > 19:
                    SetHouseMode(HouseModeEnum.Night);
                    if (LastMotion == "Master Motion")
                    {
                        _timer?.Dispose();
                        _timer = _app.RunIn(TimeSpan.FromMinutes(5), () => SetHouseMode(HouseModeEnum.Sleeping));
                    }
                    break;
                case < 8:
                    SetHouseMode(HouseModeEnum.Morning);
                    break;
                case < 18:
                    SetHouseMode(HouseModeEnum.Day);
                    break;
            }
        }

        private void SetHouseMode(HouseModeEnum mode)
        {
            if (Scheduler == null)
                _app.CallService("input_select", "select_option", new {entity_id = "input_select.house_mode", option = mode.ToString().ToLower()});
            else
                _app.SetState("input_select.house_mode", mode.ToString().ToLower(), null);
        }
    }

    internal enum HouseModeEnum
    {
        Morning,
        Day,
        Night,
        Sleeping
    }
}