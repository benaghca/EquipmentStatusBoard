using System.Windows;
using System.Windows.Input;
using EquipmentStatusTracker.WPF.Models;
using EquipmentStatusTracker.WPF.ViewModels;

namespace EquipmentStatusTracker.WPF.Views;

/// <summary>
/// Main window for the Equipment Status Tracker application.
/// This partial class contains core fields and initialization.
/// See MainWindow.Input.cs for keyboard/mouse handling.
/// See MainWindow.Canvas.cs for canvas element interactions and export.
/// </summary>
public partial class MainWindow : Window
{
    // Panning state
    private bool _isPanning;
    private bool _isMiddleMousePanning;
    private bool _isSpacePanning;
    private bool _isSpacePressed;
    private Point _lastMousePosition;

    // Equipment drag state
    private bool _isDraggingEquipment;
    private Point _dragStartPosition;
    private Equipment? _draggedEquipment;

    // Box selection state
    private bool _isBoxSelecting;
    private Point _boxSelectStart;

    // Double-click tracking
    private DateTime _lastClickTime = DateTime.MinValue;
    private Equipment? _lastClickedEquipment;

    // Group drag/resize state
    private bool _isDraggingGroup;
    private bool _isResizingGroup;
    private string? _resizeHandle;
    private Point _resizeStartPosition;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private double _resizeStartX;
    private double _resizeStartY;
    private EquipmentGroup? _draggedGroup;
    private EquipmentGroup? _resizingGroup;

    // Equipment resize state
    private bool _isResizingEquipment;
    private string? _equipmentResizeHandle;
    private Equipment? _resizingEquipment;
    private Point _equipmentResizeStartPosition;
    private double _equipmentResizeStartWidth;
    private double _equipmentResizeStartHeight;
    private double _equipmentResizeStartX;
    private double _equipmentResizeStartY;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        // Add window-level mouse handlers for reliable drag tracking
        this.MouseMove += Window_MouseMove;
        this.MouseLeftButtonUp += Window_MouseLeftButtonUp;

        // Add keyboard handlers for space+drag panning (Preview to intercept before buttons)
        this.PreviewKeyDown += Window_PreviewKeyDown;
        this.PreviewKeyUp += Window_PreviewKeyUp;

        // Add window-level mouse handler for space+drag panning (intercepts before other handlers)
        this.PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
        this.PreviewMouseMove += Window_PreviewMouseMove;
        this.PreviewMouseLeftButtonUp += Window_PreviewMouseLeftButtonUp;

        // Set initial viewport size after layout
        Loaded += (s, e) =>
        {
            ViewModel.ViewportWidth = DiagramCanvas.ActualWidth;
            ViewModel.ViewportHeight = DiagramCanvas.ActualHeight;
        };
    }
}
