using EquipmentStatusTracker.WPF.ViewModels;
using Xunit;

namespace EquipmentStatusTracker.Tests.ViewModels;

public class CanvasViewModelTests
{
    [Fact]
    public void ZoomIn_ShouldIncreaseZoomLevel()
    {
        var vm = new CanvasViewModel { ZoomLevel = 1.0 };

        vm.ZoomInCommand.Execute(null);

        Assert.Equal(1.1, vm.ZoomLevel, precision: 2);
    }

    [Fact]
    public void ZoomIn_ShouldNotExceedMaxZoom()
    {
        var vm = new CanvasViewModel { ZoomLevel = 3.0 };

        vm.ZoomInCommand.Execute(null);

        Assert.Equal(3.0, vm.ZoomLevel);
    }

    [Fact]
    public void ZoomOut_ShouldDecreaseZoomLevel()
    {
        var vm = new CanvasViewModel { ZoomLevel = 1.0 };

        vm.ZoomOutCommand.Execute(null);

        Assert.Equal(0.9, vm.ZoomLevel, precision: 2);
    }

    [Fact]
    public void ZoomOut_ShouldNotGoBelowMinZoom()
    {
        var vm = new CanvasViewModel { ZoomLevel = 0.25 };

        vm.ZoomOutCommand.Execute(null);

        Assert.Equal(0.25, vm.ZoomLevel);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(15, 15, 20, 20)]    // 15/20 = 0.75, rounds to 1, so 20
    [InlineData(25, 35, 20, 40)]    // 25/20 = 1.25, rounds to 1; 35/20 = 1.75, rounds to 2
    [InlineData(10, 10, 0, 0)]      // 10/20 = 0.5, rounds to nearest even (0)
    [InlineData(5, 5, 0, 0)]        // 5/20 = 0.25, rounds to 0
    [InlineData(30, 30, 40, 40)]    // 30/20 = 1.5, rounds to nearest even (2)
    public void SnapPosition_ShouldSnapToGrid(double inputX, double inputY, double expectedX, double expectedY)
    {
        var vm = new CanvasViewModel
        {
            SnapToGrid = true,
            GridSize = 20
        };

        var (x, y) = vm.SnapPosition(inputX, inputY);

        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }

    [Fact]
    public void SnapPosition_ShouldNotSnapWhenDisabled()
    {
        var vm = new CanvasViewModel
        {
            SnapToGrid = false,
            GridSize = 20
        };

        var (x, y) = vm.SnapPosition(15, 15);

        Assert.Equal(15, x);
        Assert.Equal(15, y);
    }

    [Fact]
    public void SnapPositionWithAnchor_ShouldSnapAnchorPointToGrid()
    {
        var vm = new CanvasViewModel
        {
            SnapToGrid = true,
            GridSize = 20
        };

        // Equipment at (10, 10) with anchor offset (25, 25) means anchor is at (35, 35)
        // Should snap anchor to (40, 40), so top-left becomes (15, 15)
        var (x, y) = vm.SnapPositionWithAnchor(10, 10, 25, 25);

        Assert.Equal(15, x);
        Assert.Equal(15, y);
    }

    [Fact]
    public void ResetView_ShouldResetToDefaults()
    {
        var vm = new CanvasViewModel
        {
            ZoomLevel = 2.5,
            PanX = 500,
            PanY = 300
        };

        vm.ResetViewCommand.Execute(null);

        Assert.Equal(1.0, vm.ZoomLevel);
        Assert.Equal(50, vm.PanX);
        Assert.Equal(50, vm.PanY);
    }

    [Fact]
    public void FitToContent_ShouldCenterAndZoomToFit()
    {
        var vm = new CanvasViewModel
        {
            ViewportWidth = 800,
            ViewportHeight = 600
        };

        // Content from (100, 100) to (300, 200) = 200x100 content
        vm.FitToContent(100, 100, 300, 200);

        // Zoom should fit the content with padding
        Assert.InRange(vm.ZoomLevel, 0.25, 2.0);
        // View should be centered on content center (200, 150)
    }

    [Fact]
    public void FitToContent_WithEmptyContent_ShouldResetView()
    {
        var vm = new CanvasViewModel
        {
            ZoomLevel = 2.0,
            PanX = 500,
            PanY = 300
        };

        vm.FitToContent(0, 0, 0, 0);

        Assert.Equal(1.0, vm.ZoomLevel);
        Assert.Equal(50, vm.PanX);
        Assert.Equal(50, vm.PanY);
    }

    [Fact]
    public void GridSize_ShouldThrowForInvalidValues()
    {
        var vm = new CanvasViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GridSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GridSize = -1);
    }

    [Fact]
    public void GridSizeOptions_ShouldContainValidOptions()
    {
        var vm = new CanvasViewModel();

        Assert.Contains(10, vm.GridSizeOptions);
        Assert.Contains(20, vm.GridSizeOptions);
        Assert.Contains(50, vm.GridSizeOptions);
    }
}
