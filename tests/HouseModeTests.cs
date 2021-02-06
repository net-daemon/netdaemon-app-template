using System;
using System.Globalization;
using House;
using Microsoft.Reactive.Testing;
using Moq;
using NetDaemon.Daemon.Fakes;
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
    public class DateTimeTest : IDateTime
    {
        DateTime dt;
        public DateTimeTest(DateTime testTime)
        {
            dt = testTime;
        }

        public DateTimeTest(string testTime)
        {
            dt = DateTime.ParseExact(testTime, "HH:mm:ss", new DateTimeFormatInfo());
        }

        public DateTime Now()
        {
            return dt;
        }
    }

    [Fact]
    public void WhenLastMotionIsMasterForMoreThanFiveMinutesThenSetModeToNight()
    {
        MockState.Add(new() {EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Day" });
        var app = new HouseModeImplementation(Object, TestScheduler); // time is "00:00:00"
        app.Initialize();

        TriggerStateChange("sensor.template_last_motion", "Landing Motion", "Master Motion");
        TestScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
        Verify(x => x.CallService("input_select", "select_option", new
        {
            entity_id = "input_select.house_mode",
            option = "Night"
        }, It.IsAny<bool>()), Times.Once);
    }

    

    [Fact]
    public void WhenLandingMotionIsTriggeredLessThanFiveAmSetModeDoesNothing()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Night" });

        var app = new HouseModeImplementation(Object, TestScheduler); // time is "00:00:00"
        app.Initialize();

        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Morning", times: Times.Never());
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredGreaterAndEqualToFiveAmSetModeToMorning()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Night" });

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(new DateTimeTest("05:00:00").Now().Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Morning");
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredLessAndEqualToSevenAmSetModeToMorning()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Night" });

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(new DateTimeTest("06:59:59").Now().Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Morning");
    }

    [Fact]
    public void AfterModeIsSetToMorningThenModeIsSetToDayModeAtSevenAm()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Morning" });

        var app = new HouseModeImplementation(Object, TestScheduler);
        app.Initialize();

        TestScheduler.AdvanceTo(new DateTimeTest("06:59:59").Now().Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Day", times: Times.Never());

        TestScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Day", times: Times.Once());
    }

    [Fact]
    public void WhenLandingMotionIsTriggeredWhenModeIsOnNightModeDoNothing()
    {
        MockState.Add(new() { EntityId = "sensor.template_last_motion", State = "Landing Motion" });
        MockState.Add(new() { EntityId = "input_select.house_mode", State = "Night" });

        var app = new HouseModeImplementation(Object, TestScheduler);// time is "00:00:00"
        app.Initialize();

        TriggerStateChange("sensor.template_last_motion", "Master Motion", "Landing Motion");
        VerifyEntitySetState("input_select.house_mode", "Day", times: Times.Never());
    }
}
