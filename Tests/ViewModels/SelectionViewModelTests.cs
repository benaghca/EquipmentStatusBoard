using EquipmentStatusTracker.WPF.Models;
using EquipmentStatusTracker.WPF.ViewModels;
using Xunit;

namespace EquipmentStatusTracker.Tests.ViewModels;

public class SelectionViewModelTests
{
    private static Equipment CreateEquipment(string id = "test-1") => new()
    {
        Id = id,
        Name = $"Test Equipment {id}",
        Type = EquipmentType.Valve
    };

    [Fact]
    public void AddToSelection_ShouldAddEquipment()
    {
        var vm = new SelectionViewModel();
        var equipment = CreateEquipment();

        vm.AddToSelection(equipment);

        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.HasSelection);
        Assert.Contains(equipment, vm.SelectedItems);
        Assert.True(equipment.IsSelected);
    }

    [Fact]
    public void AddToSelection_ShouldNotAddDuplicates()
    {
        var vm = new SelectionViewModel();
        var equipment = CreateEquipment();

        vm.AddToSelection(equipment);
        vm.AddToSelection(equipment);

        Assert.Equal(1, vm.SelectedCount);
    }

    [Fact]
    public void ToggleSelection_ShouldAddIfNotSelected()
    {
        var vm = new SelectionViewModel();
        var equipment = CreateEquipment();

        vm.ToggleSelection(equipment);

        Assert.True(equipment.IsSelected);
        Assert.Contains(equipment, vm.SelectedItems);
    }

    [Fact]
    public void ToggleSelection_ShouldRemoveIfSelected()
    {
        var vm = new SelectionViewModel();
        var equipment = CreateEquipment();

        vm.AddToSelection(equipment);
        vm.ToggleSelection(equipment);

        Assert.False(equipment.IsSelected);
        Assert.DoesNotContain(equipment, vm.SelectedItems);
    }

    [Fact]
    public void SetSelection_ShouldClearPreviousAndSelectNew()
    {
        var vm = new SelectionViewModel();
        var eq1 = CreateEquipment("eq-1");
        var eq2 = CreateEquipment("eq-2");
        var allEquipment = new[] { eq1, eq2 };

        vm.AddToSelection(eq1);
        vm.SetSelection(eq2, allEquipment);

        Assert.Equal(1, vm.SelectedCount);
        Assert.False(eq1.IsSelected);
        Assert.True(eq2.IsSelected);
    }

    [Fact]
    public void SelectAll_ShouldSelectAllEquipment()
    {
        var vm = new SelectionViewModel();
        var equipment = new[]
        {
            CreateEquipment("eq-1"),
            CreateEquipment("eq-2"),
            CreateEquipment("eq-3")
        };

        vm.SelectAll(equipment);

        Assert.Equal(3, vm.SelectedCount);
        Assert.All(equipment, eq => Assert.True(eq.IsSelected));
    }

    [Fact]
    public void ClearAll_ShouldRemoveAllSelections()
    {
        var vm = new SelectionViewModel();
        var equipment = new[]
        {
            CreateEquipment("eq-1"),
            CreateEquipment("eq-2")
        };

        vm.SelectAll(equipment);
        vm.ClearAll(equipment);

        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelection);
        Assert.All(equipment, eq => Assert.False(eq.IsSelected));
    }

    [Fact]
    public void SelectInRect_ShouldSelectEquipmentInBounds()
    {
        var vm = new SelectionViewModel();
        var insideRect = new Equipment
        {
            Id = "inside",
            X = 50,
            Y = 50,
            Width = 20,
            Height = 20
        };
        var outsideRect = new Equipment
        {
            Id = "outside",
            X = 200,
            Y = 200,
            Width = 20,
            Height = 20
        };
        var allEquipment = new[] { insideRect, outsideRect };

        // Select in rect from (0,0) to (100,100)
        vm.SelectInRect(0, 0, 100, 100, allEquipment);

        Assert.True(insideRect.IsSelected);
        Assert.False(outsideRect.IsSelected);
        Assert.Equal(1, vm.SelectedCount);
    }

    [Fact]
    public void CopySelection_ShouldCopySelectedEquipment()
    {
        var vm = new SelectionViewModel();
        var eq1 = CreateEquipment("eq-1");
        var eq2 = CreateEquipment("eq-2");

        vm.AddToSelection(eq1);
        vm.AddToSelection(eq2);
        vm.CopySelection(Enumerable.Empty<Connection>());

        Assert.True(vm.HasClipboard);
        Assert.Equal(2, vm.ClipboardEquipment.Count);
    }

    [Fact]
    public void CopySelection_ShouldCopyConnectionsBetweenSelected()
    {
        var vm = new SelectionViewModel();
        var eq1 = CreateEquipment("eq-1");
        var eq2 = CreateEquipment("eq-2");
        var eq3 = CreateEquipment("eq-3");

        var connBetweenSelected = new Connection
        {
            Id = "conn-1",
            SourceEquipmentId = "eq-1",
            TargetEquipmentId = "eq-2"
        };
        var connToUnselected = new Connection
        {
            Id = "conn-2",
            SourceEquipmentId = "eq-1",
            TargetEquipmentId = "eq-3"
        };

        vm.AddToSelection(eq1);
        vm.AddToSelection(eq2);
        vm.CopySelection(new[] { connBetweenSelected, connToUnselected });

        Assert.Single(vm.ClipboardConnections);
        Assert.Equal("conn-1", vm.ClipboardConnections[0].Id);
    }

    [Fact]
    public void BeginMove_ShouldTrackStartPositions()
    {
        var vm = new SelectionViewModel();
        var equipment = new Equipment
        {
            Id = "eq-1",
            X = 100,
            Y = 200
        };

        vm.BeginMove(new[] { equipment });
        var pos = vm.GetOriginalPosition("eq-1");

        Assert.NotNull(pos);
        Assert.Equal(100, pos.Value.X);
        Assert.Equal(200, pos.Value.Y);
    }

    [Fact]
    public void EndMove_ShouldClearTrackingState()
    {
        var vm = new SelectionViewModel();
        var equipment = new Equipment { Id = "eq-1", X = 100, Y = 200 };

        vm.BeginMove(new[] { equipment });
        vm.EndMove();

        Assert.Null(vm.GetOriginalPosition("eq-1"));
    }

    [Fact]
    public void GetOriginalPosition_ShouldReturnNullForUntracked()
    {
        var vm = new SelectionViewModel();

        var pos = vm.GetOriginalPosition("nonexistent");

        Assert.Null(pos);
    }
}
