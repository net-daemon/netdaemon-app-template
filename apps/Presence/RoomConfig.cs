using System.Collections.Generic;

namespace Presence
{
    public class RoomConfig
    {
        public RoomConfig()
        {
            PresenceEntityIds = new List<string>();
            ControlEntityIds = new List<string>();
            KeepAliveEntityIds = new List<string>();
            NightControlEntityIds = new List<string>();
        }
        public string Name { get; set; }
        public int? LuxLimit { get; set; }
        public string? LuxEntityId { get; set; }
        public string? LuxLimitEntityId { get; set; }
        public int Timeout { get; set; }
        public IEnumerable<string> PresenceEntityIds { get; set; }
        public IEnumerable<string> ControlEntityIds { get; set; }
        public IEnumerable<string> KeepAliveEntityIds { get; set; }
        public IEnumerable<string> NightControlEntityIds { get; set; }
        public IEnumerable<string> NightTimeEntityStates { get; set; }
        public string? NightTimeEntityId { get; set; }
        public int NightTimeout { get; set; }
    }
}