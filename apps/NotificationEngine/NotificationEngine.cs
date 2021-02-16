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
        private readonly Dictionary<string, Action> _messageBuilders = new();
        public string InstantMessage { get; private set; }
        public string VoiceMessage { get; private set; }

        public NotificationEngineImpl(INetDaemonRxApp app, NotificationEngineConfig config)
        {
            _app = app;
            _config = config;
            _messageBuilders.Add("CarLocked", CarIsLocked);
            _messageBuilders.Add("CarOpen", CarDoorsAreOpen);
        }

        public void Initialize()
        {
            _app.LogInformation(_config.CarLockedEntityId);
        }

        public void Notify(List<string> options)
        {
            ClearMessages();

            foreach (var option in options) _messageBuilders[option].Invoke();

            _app.LogInformation($"VoiceMessage: {VoiceMessage}");
            _app.LogInformation($"InstantMessage: {InstantMessage}");

            SendNotifications();
        }

        private void BuildMessages((string voiceMessage, string instantMessage) caller)
        {
            VoiceMessage += "<s>" + caller.voiceMessage + "</s>";
            InstantMessage += caller.instantMessage + Environment.NewLine;
        }

        private void CarDoorsAreOpen()
        {
            BuildMessages(GetStateOnMessage(_config.CarDoorsEntityId, "Your car's doors are open", "Car doors are open"));
        }

        private void CarIsLocked()
        {
            BuildMessages(GetStateOnMessage(_config.CarLockedEntityId, "Your car is not locked", "Car is not locked"));
        }

        private void ClearMessages()
        {
            VoiceMessage = "";
            InstantMessage = "";
        }

        private bool GetEntityState(string entityId, out string? state)
        {
            state = _app.States.FirstOrDefault(e => e.EntityId == entityId)?.State?.ToString();
            return state != null;
        }

        private (string voiceMessage, string instantMessage) GetStateOnMessage(string entityId, string voiceMessage, string instantMessage)
        {
            return StateIsOn(entityId) ? (voiceMessage, instantMessage) : (string.Empty, string.Empty);
        }

        private void SendNotifications()
        {
            _app.CallService("notify", "alexa_media", new
            {
                message = $"<voice name=\"Emma\">{VoiceMessage}</voice>",
                target = new List<string> {"media_player.downstairs"},
                data = new
                {
                    type = "announce"
                }
            });

            _app.CallService("notify", "twinstead", new
            {
                message = InstantMessage
            });
        }

        private bool StateIsOn(string entityId, string onState = "on")
        {
            return GetEntityState(entityId, out var state) && state.ToLower() == onState;
        }
    }

    public class NotificationEngine : NetDaemonRxApp
    {
        private NotificationEngineImpl _impl;

        public string CarDoorsEntityId { get; set; }
        public string CarLockedEntityId { get; set; }

        public override void Initialize()
        {
            var config = new NotificationEngineConfig(CarLockedEntityId, CarDoorsEntityId);
            _impl = new NotificationEngineImpl(this, config);
            _impl.Initialize();
        }

        [HomeAssistantServiceCall]
        public void Notify(dynamic data)
        {
            if (!((IDictionary<string, object>)data).ContainsKey("options")) return;

            var options = (data.options as object[] ?? Array.Empty<object>())
                .Select(o => o as string)
                .Where(o => !string.IsNullOrEmpty(o))!
                .ToList<string>();
            _impl.Notify(options);
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