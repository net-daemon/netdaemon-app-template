using System;
using System.Globalization;
using System.Linq;
using Microsoft.Reactive.Testing;
using Moq;
using NetDaemon.Common;
using NetDaemon.Daemon.Fakes;
using HouseModeApp;
using Xunit;

/// <summary>
///     Tests the fluent API parts of the daemon
/// </summary>
/// <remarks>
///     Mainly the tests checks if correct underlying call to "CallService"
///     has been made.
/// </remarks>
public class HouseModeTests : RxAppMock
{
    public HouseModeTests()
    {
        Setup(e => e.SetState(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<bool>())).Callback<string, object, object, bool>((entityId, state, attributes, waitForResponse) => UpdateMockState(entityId, state.ToString() ?? string.Empty, attributes));
    }

    public DateTime DateTimeFromString(string testTime)
    {
        return DateTime.ParseExact(testTime, "HH:mm:ss", new DateTimeFormatInfo());
    }

    private void UpdateMockState(string entityId, string newState, object? attributes)
    {
        var state = MockState.FirstOrDefault(e => e.EntityId == entityId);
        if (state == null) return;
        MockState.Remove(state);
        MockState.Add(new EntityState {EntityId = entityId, State = newState, Attribute = attributes});
    }


    [Fact]
    public void AfterModeIsSetToMorningThenModeIsSetToDayModeAtEightAm()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "morning"});

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(DateTimeFromString("07:59:59").Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "morning");

        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "day");
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredGreaterAndEqualToSevenAmSetModeToMorning()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "night"});

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(DateTimeFromString("07:00:00").Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "morning");
    }


    [Fact]
    public void WhenLandingMotionIsTriggeredLessThanFiveAmSetModeDoesNothing()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "night"});

        var app = new HouseModeImplementation(Object, TestScheduler); // time is "00:00:00"
        app.Initialize();

        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "morning", times: Times.Never());
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredLessThanEightAmSetModeToMorning()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "night"});

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(DateTimeFromString("07:59:59").Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "morning");
    }

    [Fact]
    public void WhenAfterSevenPmHouseModeIsNight()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "day" });

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(DateTimeFromString("19:59:59").Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "day");

        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyState("input_select.house_mode", "night");
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredWhenModeIsOnNightModeDoNothing()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "night"});

        var app = new HouseModeImplementation(Object, TestScheduler); // time is "00:00:00"
        app.Initialize();

        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "day", times: Times.Never());
    }

    [Fact]
    public void WhenLastMotionIsMasterForMoreThanFiveMinutesAndModeIsNightThenSetModeToSleeping()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion"});
        MockState.Add(new() {EntityId = "input_select.house_mode", State = "night"});
        var app = new HouseModeImplementation(Object, TestScheduler); // time is "00:00:00"
        app.Initialize();

        TestScheduler.AdvanceTo(DateTimeFromString("20:00:00").Ticks);
        TriggerStateChange("sensor.template_last_motion", "Landing Motion", "Master Motion");
        TestScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
        VerifyState("input_select.house_mode", "sleeping");
    }

}