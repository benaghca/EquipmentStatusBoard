using EquipmentStatusTracker.WPF.Models;
using EquipmentStatusTracker.WPF.ViewModels;
using Xunit;

namespace EquipmentStatusTracker.Tests.ViewModels;

public class ToolViewModelTests
{
    [Fact]
    public void IsEditMode_DefaultsFalse()
    {
        var vm = new ToolViewModel();
        Assert.False(vm.IsEditMode);
    }

    [Fact]
    public void SelectedTool_DefaultsToSelect()
    {
        var vm = new ToolViewModel();
        Assert.Equal("Select", vm.SelectedTool);
    }

    [Fact]
    public void SelectedConnectionTool_DefaultsEmpty()
    {
        var vm = new ToolViewModel();
        Assert.Equal("", vm.SelectedConnectionTool);
    }

    [Fact]
    public void ToggleEditMode_TogglesValue()
    {
        var vm = new ToolViewModel();

        vm.ToggleEditModeCommand.Execute(null);
        Assert.True(vm.IsEditMode);

        vm.ToggleEditModeCommand.Execute(null);
        Assert.False(vm.IsEditMode);
    }

    [Fact]
    public void ToggleEditMode_ResetsToolsWhenTurningOff()
    {
        var vm = new ToolViewModel();
        vm.IsEditMode = true;
        vm.SelectedTool = "Breaker";
        vm.SelectedConnectionTool = "Electrical";

        vm.ToggleEditModeCommand.Execute(null);

        Assert.Equal("Select", vm.SelectedTool);
        Assert.Equal("", vm.SelectedConnectionTool);
    }

    [Fact]
    public void SelectTool_ChangesSelectedTool()
    {
        var vm = new ToolViewModel();

        vm.SelectToolCommand.Execute("Breaker");

        Assert.Equal("Breaker", vm.SelectedTool);
    }

    [Fact]
    public void SelectTool_ClearsConnectionTool()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Electrical";

        vm.SelectToolCommand.Execute("Valve");

        Assert.Equal("Valve", vm.SelectedTool);
        Assert.Equal("", vm.SelectedConnectionTool);
    }

    [Fact]
    public void SelectTool_DoesNothingIfAlreadySelected()
    {
        var vm = new ToolViewModel();
        vm.SelectedTool = "Breaker";
        vm.SelectedConnectionTool = "Electrical";

        vm.SelectToolCommand.Execute("Breaker");

        // Connection tool should remain because tool wasn't changed
        Assert.Equal("Electrical", vm.SelectedConnectionTool);
    }

    [Fact]
    public void SelectConnectionTool_ChangesConnectionTool()
    {
        var vm = new ToolViewModel();

        vm.SelectConnectionToolCommand.Execute("Pipe");

        Assert.Equal("Pipe", vm.SelectedConnectionTool);
    }

    [Fact]
    public void SelectConnectionTool_ClearsSelectedTool()
    {
        var vm = new ToolViewModel();
        vm.SelectedTool = "Breaker";

        vm.SelectConnectionToolCommand.Execute("Electrical");

        Assert.Equal("Electrical", vm.SelectedConnectionTool);
        Assert.Equal("", vm.SelectedTool);
    }

    [Fact]
    public void HandleConnectionClick_ReturnsNotHandled_WhenNoConnectionTool()
    {
        var vm = new ToolViewModel();
        var equipment = new Equipment { Id = "eq1", Name = "Test" };

        var result = vm.HandleConnectionClick(equipment);

        Assert.False(result.Handled);
    }

    [Fact]
    public void HandleConnectionClick_SetsSource_OnFirstClick()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Electrical";
        var equipment = new Equipment { Id = "eq1", Name = "Test" };

        var result = vm.HandleConnectionClick(equipment);

        Assert.True(result.Handled);
        Assert.False(result.IsComplete);
        Assert.Equal(equipment, vm.ConnectionSource);
        Assert.Contains("Test", vm.ConnectionHint);
    }

    [Fact]
    public void HandleConnectionClick_ReturnsComplete_OnSecondClick()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Electrical";
        var source = new Equipment { Id = "eq1", Name = "Source" };
        var target = new Equipment { Id = "eq2", Name = "Target" };

        vm.HandleConnectionClick(source);
        var result = vm.HandleConnectionClick(target);

        Assert.True(result.Handled);
        Assert.True(result.IsComplete);
        Assert.Equal(source, result.Source);
        Assert.Equal(target, result.Target);
        Assert.Equal("Electrical", result.ConnectionType);
    }

    [Fact]
    public void HandleConnectionClick_DoesNotComplete_IfClickingSameEquipment()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Electrical";
        var equipment = new Equipment { Id = "eq1", Name = "Test" };

        vm.HandleConnectionClick(equipment);
        var result = vm.HandleConnectionClick(equipment);

        Assert.False(result.Handled);
        Assert.Equal(equipment, vm.ConnectionSource);
    }

    [Fact]
    public void CancelConnection_ClearsState()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Electrical";
        var equipment = new Equipment { Id = "eq1", Name = "Test" };
        vm.HandleConnectionClick(equipment);

        vm.CancelConnection();

        Assert.Null(vm.ConnectionSource);
        Assert.Equal("", vm.ConnectionHint);
    }

    [Fact]
    public void SetPendingAnchors_SetsValues()
    {
        var vm = new ToolViewModel();

        vm.SetPendingSourceAnchor("Top");
        vm.SetPendingTargetAnchor("Bottom");

        Assert.Equal("Top", vm.PendingSourceAnchor);
        Assert.Equal("Bottom", vm.PendingTargetAnchor);
    }

    [Fact]
    public void HandleConnectionClick_UsesAnchors()
    {
        var vm = new ToolViewModel();
        vm.SelectedConnectionTool = "Pipe";
        vm.SetPendingSourceAnchor("Left");
        vm.SetPendingTargetAnchor("Right");

        var source = new Equipment { Id = "eq1", Name = "Source" };
        var target = new Equipment { Id = "eq2", Name = "Target" };

        vm.HandleConnectionClick(source);
        var result = vm.HandleConnectionClick(target);

        Assert.Equal("Left", result.SourceAnchor);
        Assert.Equal("Right", result.TargetAnchor);
    }

    [Fact]
    public void IsCreatingConnection_ReturnsTrueWhenSourceSet()
    {
        var vm = new ToolViewModel();
        Assert.False(vm.IsCreatingConnection);

        vm.SelectedConnectionTool = "Electrical";
        vm.HandleConnectionClick(new Equipment { Id = "eq1", Name = "Test" });

        Assert.True(vm.IsCreatingConnection);
    }

    [Fact]
    public void AnchorPointOptions_ContainsAllValues()
    {
        var vm = new ToolViewModel();

        Assert.Contains(AnchorPoint.Center, vm.AnchorPointOptions);
        Assert.Contains(AnchorPoint.TopCenter, vm.AnchorPointOptions);
        Assert.Contains(AnchorPoint.BottomCenter, vm.AnchorPointOptions);
        Assert.Contains(AnchorPoint.MiddleLeft, vm.AnchorPointOptions);
        Assert.Contains(AnchorPoint.MiddleRight, vm.AnchorPointOptions);
    }
}
