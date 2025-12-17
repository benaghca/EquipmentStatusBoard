using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentStatusTracker.WPF.ViewModels;

/// <summary>
/// ViewModel for canvas state management including zoom, pan, grid, and viewport.
/// </summary>
public partial class CanvasViewModel : ObservableObject
{
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panX = 50;

    [ObservableProperty]
    private double _panY = 50;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private int _gridSize = 20;

    [ObservableProperty]
    private double _viewportWidth = 800;

    [ObservableProperty]
    private double _viewportHeight = 600;

    // Mouse position in canvas coordinates (for paste at cursor)
    public double MouseCanvasX { get; set; }
    public double MouseCanvasY { get; set; }

    public int[] GridSizeOptions { get; } = { 10, 20, 30, 40, 50 };

    partial void OnGridSizeChanging(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(GridSize), "Grid size must be at least 1");
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.1, 3.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.25);
    }

    [RelayCommand]
    private void ResetView()
    {
        ZoomLevel = 1.0;
        PanX = 50;
        PanY = 50;
    }

    /// <summary>
    /// Fits the view to show all content within the given bounding box.
    /// </summary>
    public void FitToContent(double minX, double minY, double maxX, double maxY)
    {
        double contentWidth = maxX - minX;
        double contentHeight = maxY - minY;

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            ResetView();
            return;
        }

        // Add padding (10% on each side)
        double padding = 0.1;
        double paddedWidth = contentWidth * (1 + padding * 2);
        double paddedHeight = contentHeight * (1 + padding * 2);

        // Calculate zoom to fit
        double zoomX = ViewportWidth / paddedWidth;
        double zoomY = ViewportHeight / paddedHeight;
        double newZoom = Math.Min(zoomX, zoomY);

        // Clamp zoom to reasonable bounds
        newZoom = Math.Clamp(newZoom, 0.25, 2.0);

        // Calculate pan to center the content
        double centerX = (minX + maxX) / 2;
        double centerY = (minY + maxY) / 2;

        ZoomLevel = newZoom;
        PanX = (ViewportWidth / 2) - (centerX * newZoom);
        PanY = (ViewportHeight / 2) - (centerY * newZoom);
    }

    /// <summary>
    /// Snaps a position to the grid if snap is enabled.
    /// </summary>
    public (double X, double Y) SnapPosition(double x, double y)
    {
        if (!SnapToGrid) return (x, y);

        return (
            Math.Round(x / GridSize) * GridSize,
            Math.Round(y / GridSize) * GridSize
        );
    }

    /// <summary>
    /// Snaps a position to the grid considering an anchor offset.
    /// </summary>
    public (double X, double Y) SnapPositionWithAnchor(double x, double y, double anchorOffsetX, double anchorOffsetY)
    {
        if (!SnapToGrid) return (x, y);

        double anchorX = Math.Round((x + anchorOffsetX) / GridSize) * GridSize;
        double anchorY = Math.Round((y + anchorOffsetY) / GridSize) * GridSize;

        return (anchorX - anchorOffsetX, anchorY - anchorOffsetY);
    }
}
