using EquipmentStatusTracker.WPF.Models;
using Xunit;

namespace EquipmentStatusTracker.Tests.Models;

public class EquipmentTests
{
    [Theory]
    [InlineData("open", "open", EquipmentStatus.Normal)]
    [InlineData("closed", "closed", EquipmentStatus.Normal)]
    [InlineData("on", "on", EquipmentStatus.Normal)]
    [InlineData("off", "off", EquipmentStatus.Normal)]
    [InlineData("open", "closed", EquipmentStatus.Abnormal)]
    [InlineData("closed", "open", EquipmentStatus.Abnormal)]
    [InlineData("on", "off", EquipmentStatus.Abnormal)]
    [InlineData("off", "on", EquipmentStatus.Abnormal)]
    public void Status_ShouldReflectNormalVsCurrentPosition(string normalPos, string currentPos, EquipmentStatus expected)
    {
        var equipment = new Equipment
        {
            NormalPosition = normalPos,
            CurrentPosition = currentPos
        };

        Assert.Equal(expected, equipment.Status);
    }

    [Theory]
    [InlineData(EquipmentType.Valve, new[] { "open", "closed" })]
    [InlineData(EquipmentType.Breaker, new[] { "closed", "open", "tripped" })]
    [InlineData(EquipmentType.Pump, new[] { "on", "off" })]
    [InlineData(EquipmentType.Generator, new[] { "on", "off", "standby" })]
    public void GetPositionOptions_ShouldReturnCorrectOptionsForType(EquipmentType type, string[] expectedOptions)
    {
        var equipment = new Equipment { Type = type };
        var options = equipment.GetPositionOptions();

        Assert.Equal(expectedOptions.Length, options.Length);
        foreach (var expected in expectedOptions)
        {
            Assert.Contains(expected, options);
        }
    }

    [Theory]
    [InlineData(EquipmentType.Valve, "open")]
    [InlineData(EquipmentType.Breaker, "closed")]
    [InlineData(EquipmentType.Pump, "off")]
    [InlineData(EquipmentType.Generator, "off")]
    [InlineData(EquipmentType.Chiller, "available")]
    [InlineData(EquipmentType.UPS, "on")]
    [InlineData(EquipmentType.ATS, "normal")]
    public void GetDefaultNormalPosition_ShouldReturnCorrectDefault(EquipmentType type, string expected)
    {
        var result = Equipment.GetDefaultNormalPosition(type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(EquipmentType.Generator, "on", true)]
    [InlineData(EquipmentType.Generator, "off", false)]
    [InlineData(EquipmentType.Generator, "standby", false)]
    [InlineData(EquipmentType.Transformer, "energized", true)]
    [InlineData(EquipmentType.Transformer, "de-energized", false)]
    [InlineData(EquipmentType.Valve, "on", false)]
    [InlineData(EquipmentType.Pump, "on", false)]
    [InlineData(EquipmentType.UPS, "on", false)] // UPS conducts but is not a power source
    public void IsPowerSource_ShouldIdentifyPowerSources(EquipmentType type, string position, bool expected)
    {
        var equipment = new Equipment
        {
            Type = type,
            CurrentPosition = position
        };

        Assert.Equal(expected, equipment.IsPowerSource());
    }

    [Theory]
    [InlineData(EquipmentType.Breaker, "closed", true)]
    [InlineData(EquipmentType.Breaker, "open", false)]
    [InlineData(EquipmentType.Switch, "closed", true)]
    [InlineData(EquipmentType.Switch, "open", false)]
    [InlineData(EquipmentType.ATS, "normal", true)]
    [InlineData(EquipmentType.BusBar, "energized", true)]
    [InlineData(EquipmentType.Junction, "any", true)] // Junction always conducts
    public void CanConductElectricity_ShouldDetermineConduction(EquipmentType type, string position, bool expected)
    {
        var equipment = new Equipment
        {
            Type = type,
            CurrentPosition = position
        };

        Assert.Equal(expected, equipment.CanConductElectricity());
    }

    [Fact]
    public void GetAnchorPosition_Center_ShouldReturnCenterPoint()
    {
        var equipment = new Equipment
        {
            X = 100,
            Y = 200,
            Width = 50,
            Height = 50
        };

        var (x, y) = equipment.GetAnchorPosition(AnchorPoint.Center);

        Assert.Equal(125, x); // 100 + 50/2
        Assert.Equal(225, y); // 200 + 50/2
    }

    [Fact]
    public void GetAnchorPosition_TopLeft_ShouldReturnTopLeftPoint()
    {
        var equipment = new Equipment
        {
            X = 100,
            Y = 200,
            Width = 50,
            Height = 50
        };

        var (x, y) = equipment.GetAnchorPosition(AnchorPoint.TopLeft);

        Assert.Equal(100, x);
        Assert.Equal(200, y);
    }

    [Fact]
    public void GetAnchorPosition_BottomRight_ShouldReturnBottomRightPoint()
    {
        var equipment = new Equipment
        {
            X = 100,
            Y = 200,
            Width = 50,
            Height = 50
        };

        var (x, y) = equipment.GetAnchorPosition(AnchorPoint.BottomRight);

        Assert.Equal(150, x); // 100 + 50
        Assert.Equal(250, y); // 200 + 50
    }
}
