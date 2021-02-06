using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace Niemand
{
    /// <summary>
    ///     Hello world showcase
    /// </summary>
    public class VoiceTimer : NetDaemonRxApp
    {

        public override void Initialize()
        {
            Entity("input_number.timer_interval_minutes")
                .StateChanges
                .Where(s => int.Parse(s.New.State.ToString()) >= 0)
                .Subscribe(tuple =>
                {
                    LogInformation($"Minutes Remaining: {tuple.New.State}");

                    var minutesRemaining = int.Parse(tuple.New.State.ToString());
                    if (minutesRemaining > 0)
                    {
                        CallService("notify", "alexa_media", new
                        {
                            message = $"<voice name=\"Emma\">{minutesRemaining} minutes remaining</voice>",
                            target = new List<string>() { "media_player.downstairs" },
                            data = new
                            {
                                type = "announce"
                            }
                        });
                        LogInformation($"Waiting...");
                        Delay(TimeSpan.FromMinutes(1));
                        CallService("input_number", "set_value", new { entity_id = "input_number.timer_interval_minutes", value = minutesRemaining - 1 });
                    }
                    else
                    {
                        CallService("media_player", "play_media", new
                        {
                            entity_id = "media_player.downstairs",
                            media_content_type = "sound",
                            media_content_id = "air_horn_03"
                        });
                        CallService("notify", "alexa_media", new
                        {
                            message = $"<voice name=\"Emma\">Timer has finished</voice>",
                            target = new List<string>() { "media_player.downstairs" },
                            data = new
                            {
                                type = "announce"
                            }
                        });
                        LogInformation($"Timer Finished");
                    }
                }
            );
        }
    }
}
