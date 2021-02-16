using System;
using System.Globalization;
using System.Linq;
using Microsoft.Reactive.Testing;
using Moq;
using NetDaemon.Common;
using NetDaemon.Daemon.Fakes;
using HouseModeApp;
using Niemand;
using Xunit;

/// <summary>
///     Tests the fluent API parts of the daemon
/// </summary>
/// <remarks>
///     Mainly the tests checks if correct underlying call to "CallService"
///     has been made.
/// </remarks>
public class NotificationEngineTests : RxAppMock
{
    public NotificationEngineTests()
    {
        Setup(n => n.States).Returns(MockState);
        Setup(e => e.SetState(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<bool>())).Callback<string, object, object, bool>((entityId, state, attributes, waitForResponse) => UpdateMockState(entityId, state.ToString() ?? string.Empty, attributes));
    }

    private void UpdateMockState(string entityId, string newState, object? attributes)
    {
        var state = MockState.FirstOrDefault(e => e.EntityId == entityId);
        if (state == null) return;
        MockState.Remove(state);
        MockState.Add(new EntityState() { EntityId = entityId, State = newState, Attribute = attributes });
    }

    [Fact]
    public void CarIsLocked()
    {
        var config = new NotificationEngineConfig("sensor.door_locked");

        MockState.Add(new() {EntityId = config.CarLockedEntityId, State = "on"});
        
        var app = new NotificationEngineImpl(Object, config); 
        app.Initialize();

        app.CarIsLocked();

        Assert.Equal("Your car is not locked", app.VoiceMessage);
        Assert.Equal("Car is not locked", app.InstantMessage);
    }


    [Fact]
    public void CarDoorsAreOpen()
    {
        var config = new NotificationEngineConfig( carDoorsEntityId: "sensor.doors");
    
        MockState.Add(new() { EntityId = config.CarDoorsEntityId, State = "on" });

        var app = new NotificationEngineImpl(Object, config);
        app.Initialize();

        app.CarDoorsAreOpen();

        Assert.Equal("Your car's doors are open", app.VoiceMessage);
        Assert.Equal("Car doors are open", app.InstantMessage);
        
    }

}