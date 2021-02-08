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
        private readonly TimeSpan _nightTimeout;
        private readonly TimeSpan _normalTimeout;
        private readonly string[] _presenceEntityIds;
        private readonly RoomConfig _roomConfig;
        private readonly string _roomPresenceEntityId;
        private readonly string _tracePrefix;

        private TimeSpan _timeout => IsNightTime ? _nightTimeout : _normalTimeout;

        private string ActiveEntities => string.Join(", ", _presenceEntityIds.Union( _keepAliveEntityIds).Where(entityId => _app.State(entityId)?.State == "on"));

        private string Expiry => DateTime.Now.AddSeconds(_timeout.TotalSeconds).ToString("yyyy-MM-dd HH:mm:ss");

        private bool IsNightTime
        {
            get
            {
                if (_roomConfig.NightTimeEntityId == null) return false;
                return _app.State(_roomConfig.NightTimeEntityId)?.State?.ToString() == "on";
            }
        }

        private string KeepAliveEntities => string.Join(", ", _keepAliveEntityIds);

        private int Lux
        {
            get
            {
                if (_roomConfig.LuxEntityId == null) return 0;
                string luxState = _app.State(_roomConfig.LuxEntityId)?.State?.ToString() ?? "";
                return string.IsNullOrEmpty(luxState) || luxState == "unknown" ? 0 : Convert.ToInt32(luxState);
            }
        }




        private IDisposable? Timer { get; set; }

        public RoomPresenceImplementation(INetDaemonRxApp app, RoomConfig roomConfig)
        {
            _app = app;

            try
            {
                _roomConfig = roomConfig;
                _tracePrefix = $"({_roomConfig.Name}) - ";
                _normalTimeout = TimeSpan.FromSeconds(roomConfig.Timeout != 0 ? roomConfig.Timeout : 300);
                _nightTimeout = TimeSpan.FromSeconds(roomConfig.NightTimeout != 0 ? roomConfig.NightTimeout : 60);
                _presenceEntityIds = roomConfig.PresenceEntityIds.ToArray();
                _controlEntityIds = roomConfig.ControlEntityIds.ToArray();
                _nightControlEntityIds = roomConfig.NightControlEntityIds?.ToArray() ?? Array.Empty<string>();
                _keepAliveEntityIds = roomConfig.KeepAliveEntityIds.ToArray();
                _enabledSwitchEntityId = $"switch.room_presence_enabled_{_roomConfig.Name.ToLower()}";
                _roomPresenceEntityId = $"sensor.room_presence_{_roomConfig.Name.ToLower()}";
            }
            catch (Exception e)
            {
                _app.LogError(e, "Error in Constructor");
            }
        }

        public void HandleEvent()
        {
            LogTrace("HandleEvent");

            if (IsDisabled() || !LuxBelowThreshold()) return;

            TurnOnControlEntities();
            ResetTimer();
        }

        private void HandleTimer()
        {
            if (ActiveEntities.Any())
                ResetTimer();
            else
                TurnOffControlEntities();
        }


        public void Initialize()
        {
            try
            {
                LogTrace("Initialize");
                VerifyConfig(_roomConfig);
                LogConfig(_roomConfig);
                IsDisabled();
                SetupSubscriptions();
            }
            catch (Exception e)
            {
                _app.LogError(e, "Error in Initialize");
            }
        }

        private IEnumerable<string> GetControlEntities()
        {
            LogTrace("GetControlEntities");
            if (!_nightControlEntityIds.Any())
            {
                LogTrace("Night Control entities doesnt exists");
                return _controlEntityIds;
            }


            if (IsNightTime)
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

        private void LogConfig(RoomConfig roomConfig)
        {
            LogTrace("LogConfig");
            LogTrace($"{nameof(roomConfig)} name: {roomConfig.Name}");
            _app.LogInformation($"Config for roomConfig: {roomConfig.Name}"
                                + $"\n  Timeout: {_normalTimeout}"
                                + $"\n  NightTimeout: {_nightTimeout}"
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
            return 1000;
        }

        private void ResetTimer()
        {
            LogTrace("ResetTimer");
            Timer?.Dispose();
            Timer = _app.RunIn(_timeout, HandleTimer);
            SetRoomState(RoomState.Active);
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

        private void VerifyConfig(RoomConfig roomConfig)
        {
            var entities = _roomConfig.ControlEntityIds
                .Union(_roomConfig.PresenceEntityIds)
                .Union(_roomConfig.KeepAliveEntityIds)
                .Union(_roomConfig.NightControlEntityIds)
                .Union(new List<string> { _roomConfig.LuxEntityId ?? "", _roomConfig.LuxLimitEntityId ?? "", _roomConfig.NightTimeEntityId ?? "" })
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();


            var invalidEntities = entities.Except(_app.States.Select(s => s.EntityId)).ToList();

            if (invalidEntities.Any())
                _app.LogError($"{_roomConfig.Name} contains the following invalid EntityIds:\n{string.Join("\n  - ", invalidEntities)}");
        }
    }

    internal enum RoomState
    {
        Idle,
        Active,
        Disabled
    }
}