using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Presence
{
    public class RoomPresence : NetDaemonRxApp
    {
        private readonly List<RoomPresenceImplementation> _roomServices = new();
        public IEnumerable<RoomConfig>? Rooms { get; set; }

        public override void Initialize()
        {
            LogInformation($"Starting RoomPresence for {Rooms.Count()} rooms in config");
            foreach (var room in Rooms)
            {
                LogInformation($"Initialise for {room.Name}");
                var roomPresenceImplementation = new RoomPresenceImplementation(this, room);
                _roomServices.Add( roomPresenceImplementation);
                roomPresenceImplementation.Initialize();
            }
            
        }
    }
}
