using System;
using System.Collections.Generic;
using System.Linq;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Niemand
{
    public class NotificationEngineImpl
    {
        private readonly INetDaemonRxApp _app;
        private readonly NotificationEngineConfig _config;
        public string InstantMessage { get; private set; }
        public string VoiceMessage { get; private set; }


        public NotificationEngineImpl(INetDaemonRxApp app, NotificationEngineConfig config)
        {
            _app = app;
            _config = config;
        }

        public void CarDoorsAreOpen()
        {
            if (!GetEntityState(_config.CarDoorsEntityId, out var state)) return;
            if (state == "on")
            {
                VoiceMessage = "Your car's doors are open";
                InstantMessage = "Car doors are open";
            }
        }


        public void CarIsLocked()
        {
            if (!GetEntityState(_config.CarLockedEntityId, out var state)) return;
            if (state == "on")
            {
                VoiceMessage = "Your car is not locked";
                InstantMessage = "Car is not locked";
            }
        }

        public void GenerateMessages(List<string>? options)
        {
            foreach (var option in options) _app.LogInformation(option);
        }

        public void Initialize()
        {
            _app.LogInformation(_config.CarLockedEntityId);
        }

        private bool GetEntityState(string entityId, out string? state)
        {
            state = _app.States.FirstOrDefault(e => e.EntityId == entityId)?.State?.ToString();
            return state != null;
        }
    }

    public class NotificationEngine : NetDaemonRxApp
    {
        private NotificationEngineImpl _impl;

        public string CarDoorsEntityId { get; set; }
        public string CarLockedEntityId { get; set; }

        [HomeAssistantServiceCall]
        public void GenerateMessages(dynamic data)
        {
            var options = (data.options as object[] ?? Array.Empty<object>())
                .Select(o => o as string)
                .Where(o => !string.IsNullOrEmpty(o))!
                .ToList<string>();
            _impl.GenerateMessages(options);
        }

        public override void Initialize()
        {
            var config = new NotificationEngineConfig(CarLockedEntityId, CarDoorsEntityId);
            _impl = new NotificationEngineImpl(this, config);
            _impl.Initialize();
        }
    }

    public class NotificationEngineConfig
    {
        public string CarDoorsEntityId { get; }

        public string CarLockedEntityId { get; }

        public NotificationEngineConfig(string? carLockedEntityId = null, string? carDoorsEntityId = null)
        {
            CarLockedEntityId = carLockedEntityId;
            CarDoorsEntityId = carDoorsEntityId;
        }
    }
}