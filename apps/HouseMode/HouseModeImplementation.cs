using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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

            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Subscribe(s =>
                {
                    switch (Now.Hour)
                    {
                        case >= 18 and < 19:
                            SetHouseMode(HouseModeEnum.Night);
                            break;
                    }
                });

            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Where(tuple => tuple.New.State == "Master Motion")
                .Subscribe(s =>
                {
                    _timer?.Dispose();
                    _timer = _app.RunIn(TimeSpan.FromMinutes(5), () => SetHouseMode(HouseModeEnum.Sleeping));
                });

            _app.Entity("sensor.template_last_motion")
                .StateChanges
                .Where(tuple => tuple.New.State == "Landing Motion")
                .Subscribe(s =>
                {
                    switch (Now.Hour)
                    {
                        case >= 5 and < 7:
                            SetHouseMode(HouseModeEnum.Morning);
                            break;
                        case >= 7 when _app.State("input_select.house_mode")?.State.ToString().ToLower() == "morning":
                            SetHouseMode(HouseModeEnum.Day);
                            break;
                    }
                });
        }

        private void SetHouseMode(HouseModeEnum mode)
        {
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
