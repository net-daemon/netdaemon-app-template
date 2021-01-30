using System;
using System.Collections.Generic;
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
        private readonly string _enabledSwitchEntityId;
        private readonly string[] _keepAliveEntityIds;
        private readonly string[] _nightControlEntityIds;

        private readonly string[] _presenceEntityIds;
        private readonly RoomConfig _roomConfig;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private readonly string _tracePrefix;
        private readonly string _roomPresenceEntityId;
        private string KeepAliveEntities => string.Join(", ", _keepAliveEntityIds);

        private string ActiveEntities => string.Join(", ", _presenceEntityIds.Union(_keepAliveEntityIds).Where(entityId => _app.State(entityId)?.State == "on"));

        private string Expiry => DateTime.Now.AddSeconds(_timeout.TotalSeconds).ToString("yyyy-MM-dd HH:mm:ss");
        private int Lux => _roomConfig.LuxEntityId != null ? Convert.ToInt32( _app.State(_roomConfig.LuxEntityId)?.State) : 0;
        private bool LuxBelowThreshold()
        {
            if (_roomConfig.LuxEntityId == null)
                return true;
            
            LogTrace($"Lux Value: {Lux}");
            return Lux <= LuxLimit();
        }

        private int LuxLimit()
        {

            LogTrace("LuxLimit Get");

            if (string.IsNullOrWhiteSpace(_roomConfig.LuxLimitEntityId))
            {
                var roomConfigLuxLimit = _roomConfig.LuxLimit ?? 40;
                LogTrace($"return LuxLimit (int): {roomConfigLuxLimit}");
                return roomConfigLuxLimit;
            }

            if (int.TryParse(_app.State(_roomConfig.LuxLimitEntityId)?.State?.ToString(), out int luxEntityVal))
            {
                LogTrace($"return LuxLimit (entity): {luxEntityVal}");
                return luxEntityVal;
            }

            LogTrace($"Could Not Parse {_roomConfig.LuxLimitEntityId} state");
            throw new ArgumentException("Invalid State for given LuxLimitEntityId");

        }

        private bool PresenceActive => !string.IsNullOrEmpty(ActiveEntities);


        private IDisposable? Timer { get; set; }

        public RoomPresenceImplementation(INetDaemonRxApp app, RoomConfig roomConfig)
        {
            _app = app;
            _roomConfig = roomConfig;
            _tracePrefix = $"({_roomConfig.Name}) - ";
            _timeout = TimeSpan.FromSeconds(roomConfig.Timeout);
            _presenceEntityIds = roomConfig.PresenceEntityIds.ToArray();
            _controlEntityIds = roomConfig.ControlEntityIds.ToArray();
            _nightControlEntityIds = roomConfig.NightControlEntityIds?.ToArray() ?? Array.Empty<string>();
            _keepAliveEntityIds = roomConfig.KeepAliveEntityIds.ToArray();
            _enabledSwitchEntityId = $"switch.room_presence_enabled_{_roomConfig.Name.ToLower()}";
            _roomPresenceEntityId = $"sensor.room_presence_{_roomConfig.Name.ToLower()}";
           
        }

        public void HandleEvent()
        {
            LogTrace("HandleEvent");
            LogTrace($"{nameof(PresenceActive)}: {PresenceActive}");

            if (IsDisabled()) return;

            if (PresenceActive)
            {
                LogTrace($"{nameof(LuxBelowThreshold)}: {LuxBelowThreshold()}");
                if (!LuxBelowThreshold()) return;
                TurnOnControlEntities();
                ResetExpiry();
            }
            else
            {
                TurnOffControlEntities();
            }
        }


        public void Initialize()
        {
            LogTrace("Initialize");
            LogConfig(_roomConfig);
            IsDisabled();
            SetupSubscriptions();
        }

        private IEnumerable<string> GetControlEntities()
        {
            LogTrace("GetControlEntities");
            if (!_nightControlEntityIds.Any())
            {
                LogTrace("Night Control entities doesnt exists");
                return _controlEntityIds;
            }

            if (_app.State(_roomConfig.NightTimeEntityId)?.State == "on")
            {
                LogTrace("Return Night Control Entities");
                return _nightControlEntityIds;
            }

            LogTrace($"{_roomConfig.NightTimeEntityId} is OFF");
            return _controlEntityIds;
        }

        private bool IsDisabled()
        {
            if (_app.States.FirstOrDefault(e => e.EntityId == _enabledSwitchEntityId)?.State == "off")
            {
                SetRoomState(RoomState.Disabled);
                return true;
            }

            _app.SetState(_enabledSwitchEntityId, "on", null);
            SetRoomState(RoomState.Idle);
            return false;
        }

        private void SetRoomState(RoomState roomState)
        {
            switch (roomState)
            {
                case RoomState.Idle:
                    _app.SetState(_roomPresenceEntityId, "idle", new
                    {
                        PresenceEntityIds = _presenceEntityIds,
                        KeepAliveEntities,
                        ControlEntityIds = _controlEntityIds,
                        NightControlEntityIds = _nightControlEntityIds,
                        LuxLimit = LuxLimit().ToString(),
                        Lux 
                    });
                    break;
                case RoomState.Active:
                    _app.SetState(_roomPresenceEntityId, "active", new
                    {
                        ActiveEntities,
                        PresenceEntityIds = _presenceEntityIds,
                        KeepAliveEntities,
                        ControlEntityIds = _controlEntityIds,
                        NightControlEntityIds = _nightControlEntityIds,
                        Expiry  
                    });
                    break;
                case RoomState.Disabled:
                    _app.SetState(_roomPresenceEntityId, "disabled", null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomState), roomState, null);
            }
            
        }

        private void LogConfig(RoomConfig roomConfig)
        {
            LogTrace("LogConfig");
            LogTrace($"{nameof(roomConfig)} name: {roomConfig.Name}");
            _app.LogInformation($"Config for roomConfig: {roomConfig.Name}"
                                + $"\n  Timeout: {_timeout}"
                                + $"\n  LuxLimit: {LuxLimit()}"
                                + $"\n  LuxEntityId: {roomConfig.LuxEntityId}"
                                + $"\n  LuxLimitEntityId: {roomConfig.LuxLimitEntityId}"
                                + $"\n  NightTimeEntityId: {roomConfig.NightTimeEntityId}"
                                + $"\n  PresenceEntityIds:\n   - {string.Join("\n   - ", _presenceEntityIds)}"
                                + $"\n  KeepAliveEntityIds:\n   - {string.Join("\n   - ", _keepAliveEntityIds)}"
                                + $"\n  ControlEntityIds:\n   - {string.Join("\n   - ", _controlEntityIds)}"
                                + $"\n  NightControlEntityIds:\n   - {string.Join("\n   - ", _nightControlEntityIds)}\n"
            );
        }

        private void LogTrace(string message)
        {
            _app.LogTrace($"{_tracePrefix}{message}");
        }

        private void ResetExpiry()
        {
            LogTrace("ResetExpiry");
            Timer?.Dispose();
            Timer = _app.RunIn(_timeout, HandleEvent);
            SetRoomState(RoomState.Active);
        }

        private void SetupSubscriptions()
        {
            LogTrace("SetupSubscriptions");
            foreach (var entityId in _presenceEntityIds)
                _app.Entity(entityId)
                    .StateChanges
                    .Where(e => e.Old?.State == "off" && e.New?.State == "on")
                    .Subscribe(s => { HandleEvent(); });
        }


        private void TurnOffControlEntities()
        {
            LogTrace("TurnOffControlEntities");
            foreach (var entityId in GetControlEntities()) _app.Entity(entityId).TurnOff();
            SetRoomState(RoomState.Idle);
        }

        private void TurnOnControlEntities()
        {
            LogTrace("TurnOnControlEntities");
            foreach (var entityId in GetControlEntities())
                if (_app.State(entityId)?.State == "off")
                    _app.Entity(entityId).TurnOn();
        }
    }

    internal enum RoomState
    {
        Idle,
        Active,
        Disabled
    }
}