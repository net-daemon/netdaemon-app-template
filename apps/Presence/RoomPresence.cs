using System;
using System.Linq;
using System.Reactive.Linq;
using System.Timers;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Presence
{
    /// <summary>
    ///     The NetDaemonApp implements System.Reactive API
    ///     currently the default one
    /// </summary>
    public class RoomPresence : NetDaemonRxApp
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private readonly string[] _presenceEntityIds = { "binary_sensor.office_motion", "binary_sensor.study" };
        private readonly string[] _controlEntityIds = { "light.study" };
        private readonly string[] _keepAliveEntityIds = { "binary_sensor.eugene_laptop_in_use" };

        public RoomPresence()
        {

        }

        public RoomPresence(string[] presenceEntityIds, string[]? controlEntityIds, string[]? keepAliveEntityIds = null, TimeSpan? timeout = null)
        {
            if (timeout != null) _timeout = (TimeSpan)timeout;
            _presenceEntityIds = presenceEntityIds;
            _controlEntityIds = controlEntityIds ?? Array.Empty<string>();
            _keepAliveEntityIds = keepAliveEntityIds ?? Array.Empty<string>();
        }

        public RoomPresence(string presenceEntityId, string controlEntityId, string? keepAliveEntityId = null, TimeSpan? timeout = null)
        {
            if (timeout != null) _timeout = (TimeSpan)timeout;
            _presenceEntityIds = new[] { presenceEntityId };
            _controlEntityIds = new[] { controlEntityId };
            _keepAliveEntityIds = keepAliveEntityId != null ? new[] {keepAliveEntityId} : Array.Empty<string>();
        }

        public override void Initialize()
        {
            LogInformation($"Starting RoomPresence");
            
            Entities(_presenceEntityIds)
                .StateAllChanges
                .Where(e => e.Old?.State == "off" && e.New?.State == "on")
                .Subscribe(s =>
                {
                    CalcPresence();
                });
        }

        private void CalcPresence()
        {
            var activeEntities = string.Join(", ", _presenceEntityIds.Union(_keepAliveEntityIds).Where(entityId => State(entityId)?.State == "on"));

            if (string.IsNullOrEmpty(activeEntities))
            {
                Entities(_controlEntityIds).TurnOff();
            }
            else
            {
                Entities(_controlEntityIds).TurnOn();
                SetState("room_presence.study", "on", new
                {
                    ActiveEntities = activeEntities, 
                    KeepAliveEnities = string.Join(", ", _keepAliveEntityIds),
                    Expiry = DateTime.Now.AddSeconds(_timeout.TotalSeconds)
                });
                Timer = RunIn(_timeout, CalcPresence);
            }
        }

        private IDisposable? Timer { get; set; }
    }
}
