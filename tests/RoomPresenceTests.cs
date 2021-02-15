using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Moq;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;
using NetDaemon.Daemon.Fakes;
using Presence;
using Xunit;

/// <summary>
///     Tests the fluent API parts of the daemon
/// </summary>
/// <remarks>
///     Mainly the tests checks if correct underlying call to "CallService"
///     has been made.
/// </remarks>
public class RoomPresenceTests : RxAppMock
{
    private RoomPresenceImplementation? _app;
    
    public RoomPresenceTests()
    {
        Setup(s => s.RunEvery(It.IsAny<TimeSpan>(), It.IsAny<Action>())).Returns<TimeSpan, Action>((span, action) =>
        {
            return Observable.Interval(span, TestScheduler)
                .Subscribe(_ => action());
        });

        Setup(n => n.States).Returns(MockState);
        Setup(e => e.SetState(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<bool>())).Callback<string, object, object, bool>((entityId, state, attributes, waitForResponse) => UpdateMockState(entityId, state.ToString() ?? string.Empty, attributes));
        Setup(s => s.RunIn(It.IsAny<TimeSpan>(), It.IsAny<Action>())).Returns<TimeSpan, Action>((span, action) =>
        {
            var result = new DisposableTimerResult(new CancellationToken());
            Observable.Timer(span, TestScheduler)
                .Subscribe(_ => action(), token: result.Token);
            return result;
        });
    }

    private void UpdateMockState(string entityId, string newState, object? attributes)
    {
        var state = MockState.FirstOrDefault(e => e.EntityId == entityId);
        if (state == null) return;
        MockState.Remove(state);
        MockState.Add(new EntityState() { EntityId = entityId, State = newState, Attribute = attributes });
    }

    [Fact]
    public void LightsDontTurnOnWhenNewAndOldEventStateIsOff()
    {
        // ARRANGE
        var config = new RoomConfig()
        {
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            Name = "TestRoom"
        };

        _app = new RoomPresenceImplementation(Object, config);
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First() });
        _app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "off");

        // ASSERT
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Never());
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void LightsTurnOnWhenMotionTriggered()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" }
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Once());
    }

    [Fact]
    public void NightLightsTurnOnWhenMotionTriggeredWhenNightEntityOnAndStateIsInList()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            NightControlEntityIds = new List<string>() { "light.my_night_light" },
            NightTimeEntityId = "input_select.house_mode",
            NightTimeEntityStates = new List<string>() { "sleeping", "night"}
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightTimeEntityId, State = "sleeping" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Never());
        VerifyEntityTurnOn(config.NightControlEntityIds.First(), times: Times.Once());
    }

    [Fact]
    public void NightLightsDontTurnOnButLightsDoTurnOnWhenMotionTriggeredWhenNightEntityNotInStates()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            NightControlEntityIds = new List<string>() { "light.my_night_light" },
            NightTimeEntityId = "binary_sensor.night",
            NightTimeEntityStates = new List<string>() { "sleeping", "night" }
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightTimeEntityId, State = "morning" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Once());
        VerifyEntityTurnOn(config.NightControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void LightsTurnOnWhenMotionTriggeredAndLuxBelowThresholdEntityId()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            LuxEntityId = "sensor.my_lux",
            LuxLimit = 60,
            LuxLimitEntityId = "input_number.my_lux_limit"
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.LuxLimitEntityId, State = "30" });
        MockState.Add(new() { EntityId = config.LuxEntityId, State = "10" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Once());
    }

    [Fact]
    public void LightsTurnOnWhenMotionTriggeredAndLuxBelowThresholdNumeric()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            LuxEntityId = "sensor.my_lux",
            LuxLimit = 30
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.LuxEntityId, State = "10" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Once());
    }

    [Fact]
    public void LuxLimitReturnsDefaultWhenStateCantBeParsed()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            LuxEntityId = "sensor.my_lux",
            LuxLimit = 30,
            LuxLimitEntityId = "input_number.my_lux_limit"
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.LuxEntityId, State = "Unavailable" });
        MockState.Add(new() { EntityId = config.LuxLimitEntityId, State = "Unavailable" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Once());
    }

    [Fact]
    public void AppDoesNotFailSilentlyIfExceptionOccurs()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestEx"
        };
        var app = new RoomPresenceImplementation(Object, config);
        app.Initialize();

        // ACT
        
        // ASSERT
        
    }

    [Fact]
    public void LightsDontTurnOnWhenMotionTriggeredAndLuxAboveThreshold()
    {

        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            LuxEntityId = "sensor.my_lux",
            LuxLimit = 30
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Clear();
        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.LuxEntityId, State = "40" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        // ASSERT
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void LightsTurnOnWhenMotionTriggeredOnMoreThanOneSensor()
    {
        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor", "binary_sensor.my_motion_sensor_2" },
            ControlEntityIds = new List<string>() { "light.my_light", "light.my_light_2" },
            Timeout = 1
        };
        var app = new RoomPresenceImplementation(Object, config);

        foreach (var entityId in config.PresenceEntityIds) MockState.Add(new() { EntityId = entityId, State = "off" });
        foreach (var entityId in config.ControlEntityIds) MockState.Add(new() { EntityId = entityId, State = "off" });

        app.Initialize();

        // ACT
        foreach (var entityId in config.PresenceEntityIds) TriggerStateChange(entityId, "off", "on");

        // ASSERT
        foreach (var entityId in config.ControlEntityIds) VerifyEntityTurnOn(entityId, times: Times.Once());
    }

    [Fact]
    public void NightLightsTurnOffWhenNoPresenceAfterTimeout()
    {
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            NightControlEntityIds = new List<string>() { "light.my_night_light" },
            NightTimeEntityId = "binary_sensor.night",
            NightTimeEntityStates = new List<string>() { "sleeping", "night" },
            Timeout = 1,
            NightTimeout = 3
        };
        var app = new RoomPresenceImplementation(Object, config);


        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.NightTimeEntityId, State = "night" });

        app.Initialize();
        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(2).Ticks);
        TriggerStateChange(config.PresenceEntityIds.First(), "on", "off");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(3).Ticks);
        // ASSERT
        VerifyEntityTurnOff(config.NightControlEntityIds.First(), times: Times.AtLeast(1));
    }

    [Fact]
    public void LightsTurnOffWhenNoPresenceAfterTimeout()
    {
        // ARRANGE 
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            Timeout = 4,
            NightTimeout = 1
        };
        var app = new RoomPresenceImplementation(Object, config);


        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });

        app.Initialize();
        // ACT
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        TriggerStateChange(config.PresenceEntityIds.First(), "on", "off");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);
        // ASSERT
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.AtLeast(1));
    }

    public new void VerifyState(string entityId, dynamic? state = null, dynamic? attributes = null)
    {
        var stateResult = false;
        if (attributes is not null && attributes is not object)
            throw new NotSupportedException("attributes needs to be an object");

        if (state is not null && state is not object)
            throw new NotSupportedException("state needs to be an object");

        var mockState = MockState.First(e => e.EntityId == entityId);

        if (state is not null)
        {
            if (attributes is not null)
                stateResult = mockState.State == state && mockState.Attribute == attributes;
            else
                stateResult = mockState.State == state;
        }

        if (attributes is not null)
            stateResult = mockState.Attribute == attributes;

        if (!stateResult && attributes is null) throw new ArgumentOutOfRangeException(entityId, $"State does not match, expected state '{state}' but was '{mockState.State}'");
        if (!stateResult && attributes is not null) throw new ArgumentOutOfRangeException(entityId, $"State does not match, expected state '{state}' but was '{mockState.State}'\nexpected attributes {attributes} but was {mockState.Attribute}");
    }

    [Fact]
    public void LightGuardStartsTimerForControlEntitiesThatWereNotTurnedOnByPresenceEntities()
    {
        // ARRANGE 
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            NightControlEntityIds = new List<string>() { "light.my_light_night" },
            Timeout = 300
        };
        var app = new RoomPresenceImplementation(Object, config);


        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "on" });
        MockState.Add(new() { EntityId = config.RoomPresenceEntityId, State = "idle" });

        app.Initialize();
        // ACT
        TestScheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
        VerifyState(config.RoomPresenceEntityId.ToLower(), RoomState.Override.ToString().ToLower());

        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(config.Timeout).Ticks);
        // ASSERT
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Once());
        VerifyEntityTurnOff(config.NightControlEntityIds.First(), times: Times.Once());
        VerifyState(config.RoomPresenceEntityId.ToLower(), RoomState.Idle.ToString().ToLower());
    }

    [Fact]
    public void RoomPresenceOnlyUpdateStateWhenDisabled()
    {
        // ARRANGE 
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            Timeout = 1
        };
        var app = new RoomPresenceImplementation(Object, config);


        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.RoomPresenceEntityId, State = "idle" });
        

        app.Initialize();
        // ACT

        MockState.Add(new() { EntityId = config.EnabledSwitchEntityId, State = "off" });
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");

        // ASSERT
        VerifyState(config.RoomPresenceEntityId.ToLower(), RoomState.Disabled.ToString().ToLower());
        VerifyEntityTurnOn(config.ControlEntityIds.First(), times: Times.Never());
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void LightGuardDoesNotStartTimerForControlEntitiesWhenActivePresenceEntitiesExists()
    {
        // ARRANGE 
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            Timeout = 2
        };
        var app = new RoomPresenceImplementation(Object, config);


        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "on" });

        app.Initialize();
        // ACT
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(59).Ticks);
        TriggerStateChange(config.PresenceEntityIds.First(),"off","on");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        // ASSERT
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void LightsDontTurnOffWhenKeepAliveEntityIsOn()
    {
        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            KeepAliveEntityIds = new List<string>() { "binary_sensor.keep_alive" },
            Timeout = 1
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.KeepAliveEntityIds.First(), State = "off" });

        app.Initialize();

        // ACT
        TriggerStateChange(config.KeepAliveEntityIds.First(), "off", "on");
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");

        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        TriggerStateChange(config.PresenceEntityIds.First(), "on", "off");

        // ASSERT
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Never());
    }

    [Fact]
    public void ScenarioTest1()
    {
        // ARRANGE
        var config = new RoomConfig()
        {
            Name = "TestRoom",
            PresenceEntityIds = new List<string>() { "binary_sensor.my_motion_sensor" },
            ControlEntityIds = new List<string>() { "light.my_light" },
            Timeout = 300
        };
        var app = new RoomPresenceImplementation(Object, config);

        MockState.Add(new() { EntityId = config.PresenceEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.ControlEntityIds.First(), State = "off" });
        MockState.Add(new() { EntityId = config.RoomPresenceEntityId, State = "idle" });

        app.Initialize();

        // 1st motion trigger and verify that control entity is on after motion stops
        TriggerStateChange(config.PresenceEntityIds.First(), "off", "on");
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(90).Ticks); // 1:30
        TriggerStateChange(config.PresenceEntityIds.First(), "on", "off");
        
        VerifyState(config.ControlEntityIds.First(), "on");

        // 2nd motion trigger and another 60 seconds later and verify that control entity is on after motion stops
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(30).Ticks); // 2:00
        TriggerStateChange(config.PresenceEntityIds.First(), "off", null, "on", new { last_seen = TestScheduler.Now });
        
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(60).Ticks); // 3:00
        TriggerStateChange(config.PresenceEntityIds.First(), "on", null, "on", new { last_seen = TestScheduler.Now });
        
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(90).Ticks); // 4:30
        TriggerStateChange(config.PresenceEntityIds.First(), "on", "off");
        
        VerifyState(config.ControlEntityIds.First(), "on");

        // No more motion and original timeout expired but control entitiy should remain on as 2nd and 3rd motion should have reset the timeout
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(30).Ticks); // 5:00
        VerifyState(config.ControlEntityIds.First(), "on");

        // Advance to 5 minutes after last motion, now control entity should be off
        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(180).Ticks); // 5:00
        VerifyState(config.ControlEntityIds.First(), "off");
        VerifyEntityTurnOff(config.ControlEntityIds.First(), times: Times.Once()); 
    }
}
