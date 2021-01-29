using System;
using System.Linq;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

namespace Presence
{
    /// <summary>
    ///     The NetDaemonApp implements System.Reactive API
    ///     currently the default one
    /// </summary>
    public class RoomPresenceImplementation
    {
        private readonly INetDaemonRxApp _app;
        private readonly string[] _controlEntityIds;
        private readonly string[] _keepAliveEntityIds;

        private int LuxLimit
        {
            get
            {
                if (_roomConfig.LuxLimitEntityId == null) return _roomConfig.LuxLimit ?? 40;
                
                if (int.TryParse(_app.State(_roomConfig.LuxLimitEntityId)?.State, out int luxEntityVal))
                    return luxEntityVal;
                
                throw new ArgumentException("Invalid State for given LuxLimitEntityId");
            }
        }

        private readonly string[] _presenceEntityIds;
        private readonly RoomConfig _roomConfig;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private string[] _nightControlEntityIds;

        private string ActiveEntities => string.Join(", ", _presenceEntityIds.Union(_keepAliveEntityIds).Where(entityId => _app.State(entityId)?.State == "on"));
        private bool LuxBelowThreshold => _roomConfig.LuxEntityId == null ? true : int.Parse(_app.State(_roomConfig.LuxEntityId)?.State?.ToString()) <= LuxLimit;
        private bool PresenceActive => !string.IsNullOrEmpty(ActiveEntities);


        private IDisposable? Timer { get; set; }

        public RoomPresenceImplementation(INetDaemonRxApp app, RoomConfig roomConfig)
        {
            _app = app;
            _roomConfig = roomConfig;
            _timeout = TimeSpan.FromSeconds(roomConfig.Timeout);
            _presenceEntityIds = roomConfig.PresenceEntityIds.ToArray();
            _controlEntityIds = roomConfig.ControlEntityIds.ToArray();
            _nightControlEntityIds = roomConfig.NightControlEntityIds?.ToArray() ?? Array.Empty<string>();
            _keepAliveEntityIds = roomConfig.KeepAliveEntityIds.ToArray();
        }

        public void CalcPresence()
        {
            if (PresenceActive)
            {
                if (!LuxBelowThreshold) return;
                TurnOnControlEntities();
                ResetExpiary();
            }
            else
            {
                TurnOffControlEntities();
            }
        }


        public void Initialize()
        {
            LogConfig(_roomConfig);
            foreach (var entityId in _presenceEntityIds)
                _app.Entity(entityId)
                    .StateChanges
                    .Where(e => e.Old?.State == "off" && e.New?.State == "on")
                    .Subscribe(s => { CalcPresence(); });
        }


        private void LogConfig(RoomConfig roomConfig)
        {
            _app.LogInformation($"Config for roomConfig: {roomConfig.Name}"
                                + $"\n  Timeout: {_timeout}"
                                + $"\n  LuxLimit: {LuxLimit}"
                                + $"\n  LuxEntityId: {roomConfig.LuxEntityId}"
                                + $"\n  PresenceEntityIds:\n   - {string.Join("\n   - ", _presenceEntityIds)}"
                                + $"\n  KeepAliveEntityIds:\n   - {string.Join("\n   - ", _keepAliveEntityIds)}"
                                + $"\n  ControlEntityIds:\n   - {string.Join("\n   - ", _controlEntityIds)}\n"
            );
        }

        private void ResetExpiary()
        {
            Timer?.Dispose();
            Timer = _app.RunIn(_timeout, CalcPresence);
            _app.SetState($"room_presence.{_roomConfig.Name.ToLower()}", "on", new
            {
                ActiveEntities,
                KeepAliveEnities = string.Join(", ", _keepAliveEntityIds),
                Expiry = DateTime.Now.AddSeconds(_timeout.TotalSeconds).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }


        private void TurnOffControlEntities()
        {
            foreach (var entityId in _controlEntityIds) _app.Entity(entityId).TurnOff();
            _app.SetState($"room_presence.{_roomConfig.Name.ToLower()}", "off", null);
        }

        private void TurnOnControlEntities()
        {
            var entities = _nightControlEntityIds.Any() && _app.State(_roomConfig.NightTimeEntityId)?.State == "on" ? _nightControlEntityIds : _controlEntityIds;

            foreach (var entityId in entities)
                if (_app.State(entityId)?.State == "off")
                    _app.Entity(entityId).TurnOn();
        }
    }
}