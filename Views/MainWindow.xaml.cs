using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using EquipmentStatusTracker.WPF.Models;
using EquipmentStatusTracker.WPF.ViewModels;

namespace EquipmentStatusTracker.WPF.Views;

public partial class MainWindow : Window
{
    private bool _isPanning;
    private bool _isMiddleMousePanning;
    private bool _isSpacePanning;
    private bool _isSpacePressed;
    private bool _isDraggingEquipment;
    private bool _isBoxSelecting;
    private bool _isDraggingGroup;
    private bool _isResizingGroup;
    private string? _resizeHandle;
    private Point _lastMousePosition;
    private Point _boxSelectStart;
    private Point _dragStartPosition;
    private Point _resizeStartPosition;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private double _resizeStartX;
    private double _resizeStartY;
    private DateTime _lastClickTime = DateTime.MinValue;
    private Equipment? _lastClickedEquipment;
    private Equipment? _draggedEquipment;
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

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !_isSpacePressed)
        {
            _isSpacePressed = true;
            // Change cursor to hand to indicate pan mode
            Mouse.OverrideCursor = Cursors.Hand;
            // Prevent space from activating focused buttons
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
            _isSpacePanning = false;
            // Reset cursor back to default
            Mouse.OverrideCursor = null;
            // Prevent space from activating focused buttons
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isSpacePressed)
        {
            _isSpacePanning = true;
            _lastMousePosition = e.GetPosition(this);
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        // Safety: check actual keyboard state and sync our flag
        bool spaceActuallyPressed = Keyboard.IsKeyDown(Key.Space);
        if (!spaceActuallyPressed && (_isSpacePressed || Mouse.OverrideCursor == Cursors.Hand))
        {
            _isSpacePressed = false;
            _isSpacePanning = false;
            Mouse.OverrideCursor = null;
        }

        if (_isSpacePanning && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.PanX += delta.X;
            ViewModel.PanY += delta.Y;

            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSpacePanning)
        {
            _isSpacePanning = false;
            // Reset cursor if space is no longer pressed
            if (!_isSpacePressed)
            {
                Mouse.OverrideCursor = null;
            }
            e.Handled = true;
        }
    }

    private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.ViewportWidth = e.NewSize.Width;
        ViewModel.ViewportHeight = e.NewSize.Height;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        // If mouse button is released, stop any active operations
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_isResizingGroup)
            {
                _isResizingGroup = false;
                _resizingGroup = null;
                _resizeHandle = null;
                if (this.IsMouseCaptured)
                {
                    this.ReleaseMouseCapture();
                }
            }
            if (_draggedGroup != null)
            {
                _isDraggingGroup = false;
                _draggedGroup = null;
                if (this.IsMouseCaptured)
                {
                    this.ReleaseMouseCapture();
                }
            }
            if (_isDraggingEquipment)
            {
                _isDraggingEquipment = false;
                _draggedEquipment = null;
                if (this.IsMouseCaptured)
                {
                    this.ReleaseMouseCapture();
                }
            }
            if (_isResizingEquipment)
            {
                _isResizingEquipment = false;
                _resizingEquipment = null;
                _equipmentResizeHandle = null;
                if (this.IsMouseCaptured)
                {
                    this.ReleaseMouseCapture();
                }
            }
            return;
        }

        // Handle equipment resizing
        if (_isResizingEquipment && _resizingEquipment != null && _equipmentResizeHandle != null)
        {
            try
            {
                var currentPos = e.GetPosition(EquipmentOverlay);
                var deltaX = currentPos.X - _equipmentResizeStartPosition.X;
                var deltaY = currentPos.Y - _equipmentResizeStartPosition.Y;

                double newX = _equipmentResizeStartX;
                double newY = _equipmentResizeStartY;
                double newWidth = _equipmentResizeStartWidth;
                double newHeight = _equipmentResizeStartHeight;

                if (_equipmentResizeHandle.Contains("W"))
                {
                    newX = _equipmentResizeStartX + deltaX;
                    newWidth = _equipmentResizeStartWidth - deltaX;
                }
                if (_equipmentResizeHandle.Contains("E"))
                {
                    newWidth = _equipmentResizeStartWidth + deltaX;
                }
                if (_equipmentResizeHandle.Contains("N"))
                {
                    newY = _equipmentResizeStartY + deltaY;
                    newHeight = _equipmentResizeStartHeight - deltaY;
                }
                if (_equipmentResizeHandle.Contains("S"))
                {
                    newHeight = _equipmentResizeStartHeight + deltaY;
                }

                // Enforce minimum size of 30x30
                if (newWidth < 30) newWidth = 30;
                if (newHeight < 30) newHeight = 30;

                ViewModel.ResizeEquipment(_resizingEquipment, newX, newY, newWidth, newHeight);
                e.Handled = true;
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resizing equipment: {ex.Message}");
            }
        }

        // Handle group resizing
        if (_isResizingGroup && _resizingGroup != null && _resizeHandle != null)
        {
            try
            {
                var currentPos = e.GetPosition(GroupsOverlay);
                var deltaX = currentPos.X - _resizeStartPosition.X;
                var deltaY = currentPos.Y - _resizeStartPosition.Y;

                // Apply resize based on handle
                double newX = _resizeStartX;
                double newY = _resizeStartY;
                double newWidth = _resizeStartWidth;
                double newHeight = _resizeStartHeight;

                if (_resizeHandle.Contains("W"))
                {
                    newX = _resizeStartX + deltaX;
                    newWidth = _resizeStartWidth - deltaX;
                }
                if (_resizeHandle.Contains("E"))
                {
                    newWidth = _resizeStartWidth + deltaX;
                }
                if (_resizeHandle.Contains("N"))
                {
                    newY = _resizeStartY + deltaY;
                    newHeight = _resizeStartHeight - deltaY;
                }
                if (_resizeHandle.Contains("S"))
                {
                    newHeight = _resizeStartHeight + deltaY;
                }

                // Enforce minimum size
                if (newWidth < 50) newWidth = 50;
                if (newHeight < 50) newHeight = 50;

                ViewModel.ResizeGroup(_resizingGroup, newX, newY, newWidth, newHeight);
                e.Handled = true;
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resizing group: {ex.Message}");
            }
        }
        
        // Handle group dragging (only start if mouse has moved)
        if (_draggedGroup != null && !_isResizingGroup)
        {
            var currentPos = e.GetPosition(GroupsOverlay);
            var deltaX = Math.Abs(currentPos.X - _dragStartPosition.X);
            var deltaY = Math.Abs(currentPos.Y - _dragStartPosition.Y);
            
            // Only start dragging if mouse has moved more than a few pixels
            if (!_isDraggingGroup && (deltaX > 3 || deltaY > 3))
            {
                _isDraggingGroup = true;
            }
            
            if (_isDraggingGroup)
            {
                try
                {
                    var totalDeltaX = currentPos.X - _dragStartPosition.X;
                    var totalDeltaY = currentPos.Y - _dragStartPosition.Y;

                    ViewModel.MoveGroup(_draggedGroup, totalDeltaX, totalDeltaY);
                    _dragStartPosition = currentPos;
                    e.Handled = true;
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error dragging group: {ex.Message}");
                }
            }
        }
        
        // Handle equipment dragging
        if (_isDraggingEquipment && _draggedEquipment != null)
        {
            try
            {
                var currentPos = e.GetPosition(this);
                var screenDelta = currentPos - _dragStartPosition;
                
                // Convert screen delta to canvas delta
                var canvasDeltaX = screenDelta.X / ViewModel.ZoomLevel;
                var canvasDeltaY = screenDelta.Y / ViewModel.ZoomLevel;

                var selectedEquipment = ViewModel.GetSelectedEquipment();
                foreach (var eq in selectedEquipment)
                {
                    // Get original position from ViewModel
                    var originalPos = ViewModel.GetOriginalPosition(eq.Id);
                    if (originalPos.HasValue)
                    {
                        var newX = originalPos.Value.X + canvasDeltaX;
                        var newY = originalPos.Value.Y + canvasDeltaY;
                        ViewModel.MoveEquipment(eq, newX, newY, saveAfter: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error dragging equipment: {ex.Message}");
                _isDraggingEquipment = false;
                _draggedEquipment = null;
                this.ReleaseMouseCapture();
            }
            return;
        }
        
        // Handle box selection
        if (_isBoxSelecting)
        {
            var currentPos = e.GetPosition(SelectionCanvas);
            
            var x = Math.Min(_boxSelectStart.X, currentPos.X);
            var y = Math.Min(_boxSelectStart.Y, currentPos.Y);
            var width = Math.Abs(currentPos.X - _boxSelectStart.X);
            var height = Math.Abs(currentPos.Y - _boxSelectStart.Y);
            
            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Handle equipment resizing end
        if (_isResizingEquipment)
        {
            _isResizingEquipment = false;
            _resizingEquipment = null;
            _equipmentResizeHandle = null;
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }
            e.Handled = true;
            return;
        }

        // Handle group resizing end
        if (_isResizingGroup)
        {
            _isResizingGroup = false;
            _resizingGroup = null;
            _resizeHandle = null;
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }
            e.Handled = true;
            return;
        }

        // Handle group dragging end (or just selection if no drag occurred)
        if (_draggedGroup != null && !_isResizingGroup)
        {
            _isDraggingGroup = false;
            _draggedGroup = null;
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }
            e.Handled = true;
            return;
        }
        
        // Handle equipment drag end
        if (_isDraggingEquipment)
        {
            try
            {
                ViewModel.EndMoveEquipment(ViewModel.GetSelectedEquipment());
            }
            finally
            {
                _isDraggingEquipment = false;
                _draggedEquipment = null;
                this.ReleaseMouseCapture();
            }
            return;
        }
        
        // Handle box selection end
        if (_isBoxSelecting)
        {
            _isBoxSelecting = false;
            SelectionRectangle.Visibility = Visibility.Collapsed;
            this.ReleaseMouseCapture();

            var rectLeft = Canvas.GetLeft(SelectionRectangle);
            var rectTop = Canvas.GetTop(SelectionRectangle);
            var rectRight = rectLeft + SelectionRectangle.Width;
            var rectBottom = rectTop + SelectionRectangle.Height;

            var canvasLeft = (rectLeft - ViewModel.PanX) / ViewModel.ZoomLevel;
            var canvasTop = (rectTop - ViewModel.PanY) / ViewModel.ZoomLevel;
            var canvasRight = (rectRight - ViewModel.PanX) / ViewModel.ZoomLevel;
            var canvasBottom = (rectBottom - ViewModel.PanY) / ViewModel.ZoomLevel;

            ViewModel.SelectInRect(canvasLeft, canvasTop, canvasRight, canvasBottom);
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel connection first
            if (!string.IsNullOrEmpty(ViewModel.SelectedConnectionTool) && ViewModel.ConnectionSource != null)
            {
                ViewModel.CancelConnection();
                e.Handled = true;
                return;
            }

            // Deselect connection
            if (ViewModel.SelectedConnection != null)
            {
                ViewModel.SelectedConnection = null;
                e.Handled = true;
                return;
            }

            // Deselect group
            if (ViewModel.SelectedGroup != null)
            {
                ViewModel.SelectedGroup = null;
                e.Handled = true;
                return;
            }

            // Clear equipment selection
            if (ViewModel.SelectedCount > 0)
            {
                ViewModel.ClearSelection();
                e.Handled = true;
                return;
            }

            // Deselect tools
            if (!string.IsNullOrEmpty(ViewModel.SelectedConnectionTool))
            {
                ViewModel.SelectedConnectionTool = "";
                e.Handled = true;
                return;
            }

            if (ViewModel.SelectedTool != "Select")
            {
                ViewModel.SelectedTool = "Select";
                e.Handled = true;
                return;
            }
        }

        // Keyboard shortcuts (edit mode only for copy/paste/select all)
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.C:
                    if (ViewModel.IsEditMode)
                    {
                        ViewModel.CopySelection();
                        e.Handled = true;
                    }
                    break;
                case Key.V:
                    if (ViewModel.IsEditMode)
                    {
                        ViewModel.PasteSelection();
                        e.Handled = true;
                    }
                    break;
                case Key.A:
                    if (ViewModel.IsEditMode)
                    {
                        ViewModel.SelectAll();
                        e.Handled = true;
                    }
                    break;
                case Key.Z:
                    if (ViewModel.IsEditMode)
                    {
                        ViewModel.UndoCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
                case Key.Y:
                    if (ViewModel.IsEditMode)
                    {
                        ViewModel.RedoCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }

        if (e.Key == Key.Delete)
        {
            // Only allow deletion in edit mode
            if (!ViewModel.IsEditMode) return;
            
            // Prioritize equipment deletion if any is selected
            if (ViewModel.SelectedCount > 0)
            {
                ViewModel.DeleteSelectedEquipment();
                e.Handled = true;
            }
            else if (ViewModel.SelectedConnection != null)
            {
                ViewModel.DeleteSelectedConnection();
                e.Handled = true;
            }
            else if (ViewModel.SelectedGroup != null)
            {
                ViewModel.DeleteSelectedGroup();
                e.Handled = true;
            }
        }
    }

    private void EquipmentItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Equipment equipment)
        {
            ViewModel.SelectEquipmentCommand.Execute(equipment);
        }
    }

    private void EquipmentItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Equipment equipment)
        {
            var now = DateTime.Now;
            
            if (_lastClickedEquipment == equipment && (now - _lastClickTime).TotalMilliseconds < 500)
            {
                SnapToEquipment(equipment);
                _lastClickedEquipment = null;
            }
            else
            {
                _lastClickedEquipment = equipment;
                _lastClickTime = now;
            }
        }
    }

    private void SnapToEquipment(Equipment equipment)
    {
        var canvasWidth = EquipmentOverlay.ActualWidth > 0 ? EquipmentOverlay.ActualWidth : 800;
        var canvasHeight = EquipmentOverlay.ActualHeight > 0 ? EquipmentOverlay.ActualHeight : 600;
        
        var equipmentCenterX = equipment.X + equipment.Width / 2;
        var equipmentCenterY = equipment.Y + equipment.Height / 2;
        
        ViewModel.ZoomLevel = 1.5;
        ViewModel.PanX = (canvasWidth / 2) - (equipmentCenterX * ViewModel.ZoomLevel);
        ViewModel.PanY = (canvasHeight / 2) - (equipmentCenterY * ViewModel.ZoomLevel);
    }

    private void Equipment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Equipment equipment)
        {
            // Handle connection tool
            if (!string.IsNullOrEmpty(ViewModel.SelectedConnectionTool))
            {
                if (ViewModel.TryHandleConnectionClick(equipment))
                {
                    e.Handled = true;
                    return;
                }
            }

            // Handle selection with modifiers
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                ViewModel.AddToSelection(equipment);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                ViewModel.ToggleSelection(equipment);
            }
            else if (!equipment.IsSelected)
            {
                ViewModel.SetSelection(equipment);
            }

            // Always open detail panel when selecting equipment (in both view and edit mode)
            ViewModel.SelectEquipmentCommand.Execute(equipment);

            // Start dragging in edit mode (when not using a connection tool)
            if (ViewModel.IsEditMode && string.IsNullOrEmpty(ViewModel.SelectedConnectionTool))
            {
                _isDraggingEquipment = true;
                _draggedEquipment = equipment;
                _dragStartPosition = e.GetPosition(this);
                ViewModel.BeginMoveEquipment(ViewModel.GetSelectedEquipment());
                this.CaptureMouse();
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }
    }

    private void Equipment_MouseMove(object sender, MouseEventArgs e)
    {
        // Drag logic handled at window level
        e.Handled = _isDraggingEquipment;
    }

    private void Equipment_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Release logic handled at window level
        e.Handled = _isDraggingEquipment;
    }

    private void Anchor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string anchor)
        {
            // Find the equipment this anchor belongs to (with a safety limit)
            var parent = element;
            int maxDepth = 20;
            while (parent != null && !(parent.DataContext is Equipment) && maxDepth > 0)
            {
                parent = parent.Parent as FrameworkElement;
                maxDepth--;
            }

            if (parent?.DataContext is Equipment equipment)
            {
                if (ViewModel.ConnectionSource == null)
                {
                    ViewModel.SetPendingSourceAnchor(anchor);
                }
                else
                {
                    ViewModel.SetPendingTargetAnchor(anchor);
                }

                ViewModel.TryHandleConnectionClick(equipment);
                e.Handled = true;
            }
        }
    }

    private void Connection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // The sender is the Line element, get the Connection from DataContext
        if (sender is FrameworkElement element)
        {
            // Try to get Connection from the element or its parent
            Connection? connection = element.DataContext as Connection;
            if (connection == null && element.Parent is FrameworkElement parent)
            {
                connection = parent.DataContext as Connection;
            }
            
            if (connection != null)
            {
                ViewModel.ClearSelection();
                ViewModel.SelectedConnection = connection;
                e.Handled = true;
            }
        }
    }

    private void Group_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't start dragging if clicking on a resize handle
        if (e.OriginalSource is FrameworkElement source && source.Tag is string tag && tag != null)
        {
            return; // Let the resize handle handler take over
        }

        if (sender is FrameworkElement element && element.DataContext is EquipmentGroup group)
        {
            ViewModel.ClearSelection();
            ViewModel.SelectedGroup = group;

            if (ViewModel.IsEditMode && string.IsNullOrEmpty(ViewModel.SelectedConnectionTool))
            {
                // Store initial position but don't start dragging yet - wait for mouse movement
                _dragStartPosition = e.GetPosition(GroupsOverlay);
                _draggedGroup = group;
                this.CaptureMouse(); // Capture at window level for reliable tracking
            }

            e.Handled = true;
        }
    }

    private void Group_ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement handle && handle.DataContext is EquipmentGroup group)
        {
            ViewModel.ClearSelection();
            ViewModel.SelectedGroup = group;

            if (ViewModel.IsEditMode)
            {
                _isResizingGroup = true;
                _resizingGroup = group;
                _resizeHandle = handle.Tag?.ToString();
                _resizeStartPosition = e.GetPosition(GroupsOverlay);
                _resizeStartWidth = group.Width;
                _resizeStartHeight = group.Height;
                _resizeStartX = group.X;
                _resizeStartY = group.Y;
                this.CaptureMouse(); // Capture at window level for reliable tracking
            }

            e.Handled = true;
        }
    }

    private void Equipment_ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement handle && handle.DataContext is Equipment equipment)
        {
            ViewModel.SetSelection(equipment);

            if (ViewModel.IsEditMode)
            {
                _isResizingEquipment = true;
                _resizingEquipment = equipment;
                _equipmentResizeHandle = handle.Tag?.ToString();
                _equipmentResizeStartPosition = e.GetPosition(EquipmentOverlay);
                _equipmentResizeStartWidth = equipment.Width;
                _equipmentResizeStartHeight = equipment.Height;
                _equipmentResizeStartX = equipment.X;
                _equipmentResizeStartY = equipment.Y;
                this.CaptureMouse();
            }

            e.Handled = true;
        }
    }

    private void Group_MouseMove(object sender, MouseEventArgs e)
    {
        // Resize and drag logic handled at window level
        e.Handled = _isResizingGroup || _isDraggingGroup;
    }

    private void Group_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Release logic handled at window level
        e.Handled = _isResizingGroup || _isDraggingGroup;
    }

    private void LayerName_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is Layer layer)
        {
            ViewModel.SetActiveLayerCommand.Execute(layer);
        }
    }

    private void LayerName_LostFocus(object sender, RoutedEventArgs e)
    {
        // Trigger auto-save when layer name editing is complete
        ViewModel.TriggerAutoSave();
    }

    private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is CanvasLabel label)
        {
            // Deselect other labels
            foreach (var l in ViewModel.Labels)
            {
                l.IsSelected = l == label;
            }
            ViewModel.SelectedLabel = label;
            ViewModel.ClearSelection();
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Space + left click = pan (priority over everything else)
        if (_isSpacePressed)
        {
            _isSpacePanning = true;
            _lastMousePosition = e.GetPosition(this);
            DiagramCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        var source = e.OriginalSource as FrameworkElement;

        // Check if clicking on empty canvas area
        if (source == DiagramCanvas || source?.Name == "DiagramCanvas")
        {
            // In edit mode, check if we should add equipment or start box select
            if (ViewModel.IsEditMode)
            {
                // Check if an equipment type tool is selected
                if (!string.IsNullOrEmpty(ViewModel.SelectedTool) && ViewModel.SelectedTool != "Select")
                {
                    var pos = e.GetPosition(EquipmentOverlay);

                    // Handle Label tool
                    if (ViewModel.SelectedTool == "Label")
                    {
                        ViewModel.AddLabelAtPosition(pos.X, pos.Y);
                        e.Handled = true;
                        return;
                    }

                    // Add equipment at click position
                    if (Enum.TryParse<EquipmentType>(ViewModel.SelectedTool, out var equipmentType))
                    {
                        ViewModel.AddEquipmentAtPosition(equipmentType, pos.X, pos.Y);
                    }
                    e.Handled = true;
                    return;
                }

                // Start box selection if select tool is active
                if (ViewModel.SelectedTool == "Select" || string.IsNullOrEmpty(ViewModel.SelectedTool))
                {
                    if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.Control)
                    {
                        ViewModel.ClearSelection();
                    }

                    _isBoxSelecting = true;
                    _boxSelectStart = e.GetPosition(SelectionCanvas);
                    
                    Canvas.SetLeft(SelectionRectangle, _boxSelectStart.X);
                    Canvas.SetTop(SelectionRectangle, _boxSelectStart.Y);
                    SelectionRectangle.Width = 0;
                    SelectionRectangle.Height = 0;
                    SelectionRectangle.Visibility = Visibility.Visible;
                    
                    // Capture mouse at window level for reliable tracking
                    this.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }

            // Start panning
            _isPanning = true;
            _lastMousePosition = e.GetPosition(this);
            DiagramCanvas.CaptureMouse();
        }
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Middle mouse button panning
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isMiddleMousePanning = true;
            _lastMousePosition = e.GetPosition(this);
            DiagramCanvas.CaptureMouse();
            e.Handled = true;
        }

        // Space + left click panning
        if (_isSpacePressed && e.LeftButton == MouseButtonState.Pressed)
        {
            _isSpacePanning = true;
            _lastMousePosition = e.GetPosition(this);
            DiagramCanvas.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Released && _isMiddleMousePanning)
        {
            _isMiddleMousePanning = false;
            DiagramCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        // Space panning release
        if (e.LeftButton == MouseButtonState.Released && _isSpacePanning)
        {
            _isSpacePanning = false;
            DiagramCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        // Middle mouse panning
        if (_isMiddleMousePanning && e.MiddleButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.PanX += delta.X;
            ViewModel.PanY += delta.Y;

            _lastMousePosition = currentPosition;
            return;
        }

        // Space + drag panning
        if (_isSpacePanning && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.PanX += delta.X;
            ViewModel.PanY += delta.Y;

            _lastMousePosition = currentPosition;
            return;
        }

        // Left mouse panning
        if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;
            
            ViewModel.PanX += delta.X;
            ViewModel.PanY += delta.Y;
            
            _lastMousePosition = currentPosition;
            return;
        }

        // Box selection
        if (_isBoxSelecting && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(SelectionCanvas);
            
            var x = Math.Min(_boxSelectStart.X, currentPos.X);
            var y = Math.Min(_boxSelectStart.Y, currentPos.Y);
            var width = Math.Abs(currentPos.X - _boxSelectStart.X);
            var height = Math.Abs(currentPos.Y - _boxSelectStart.Y);
            
            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            DiagramCanvas.ReleaseMouseCapture();
        }

        if (_isBoxSelecting)
        {
            _isBoxSelecting = false;
            SelectionRectangle.Visibility = Visibility.Collapsed;
            this.ReleaseMouseCapture();

            // Calculate selection rectangle in canvas coordinates
            var rectLeft = Canvas.GetLeft(SelectionRectangle);
            var rectTop = Canvas.GetTop(SelectionRectangle);
            var rectRight = rectLeft + SelectionRectangle.Width;
            var rectBottom = rectTop + SelectionRectangle.Height;

            // Transform screen coordinates to canvas coordinates
            var canvasLeft = (rectLeft - ViewModel.PanX) / ViewModel.ZoomLevel;
            var canvasTop = (rectTop - ViewModel.PanY) / ViewModel.ZoomLevel;
            var canvasRight = (rectRight - ViewModel.PanX) / ViewModel.ZoomLevel;
            var canvasBottom = (rectBottom - ViewModel.PanY) / ViewModel.ZoomLevel;

            ViewModel.SelectInRect(canvasLeft, canvasTop, canvasRight, canvasBottom);
        }
    }

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 0.1 : -0.1;
        var newZoom = Math.Clamp(ViewModel.ZoomLevel + delta, 0.25, 3.0);
        ViewModel.ZoomLevel = newZoom;
    }

    private void ExportToImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if there's anything to export
            if (ViewModel.EquipmentCollection.Count == 0 && ViewModel.Groups.Count == 0 && ViewModel.Labels.Count == 0)
            {
                MessageBox.Show("There is no content to export.", "Export to Image", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Calculate bounds of all content
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var eq in ViewModel.EquipmentCollection)
            {
                minX = Math.Min(minX, eq.X);
                minY = Math.Min(minY, eq.Y);
                maxX = Math.Max(maxX, eq.X + eq.Width);
                maxY = Math.Max(maxY, eq.Y + eq.Height + 30); // Account for label below
            }

            foreach (var group in ViewModel.Groups)
            {
                minX = Math.Min(minX, group.X);
                minY = Math.Min(minY, group.Y);
                maxX = Math.Max(maxX, group.X + group.Width);
                maxY = Math.Max(maxY, group.Y + group.Height);
            }

            foreach (var label in ViewModel.Labels)
            {
                minX = Math.Min(minX, label.X);
                minY = Math.Min(minY, label.Y);
                maxX = Math.Max(maxX, label.X + 200); // Estimate label width
                maxY = Math.Max(maxY, label.Y + label.FontSize + 20);
            }

            // Add padding
            double padding = 50;
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;

            double contentWidth = maxX - minX;
            double contentHeight = maxY - minY;

            // Store current transform values
            double originalPanX = ViewModel.PanX;
            double originalPanY = ViewModel.PanY;
            double originalZoom = ViewModel.ZoomLevel;

            // Set zoom to 1 and pan to show content from (0,0) at top-left
            ViewModel.ZoomLevel = 1.0;
            ViewModel.PanX = -minX;
            ViewModel.PanY = -minY;

            // Force layout update
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            // Find the canvas container (parent of all overlays)
            var canvasContainer = DiagramCanvas.Parent as Grid;
            if (canvasContainer == null)
            {
                MessageBox.Show("Could not find canvas container.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create bitmap with content dimensions
            int pixelWidth = (int)Math.Ceiling(contentWidth);
            int pixelHeight = (int)Math.Ceiling(contentHeight);

            // Clamp to reasonable size (max 8000x8000)
            double scale = 1.0;
            if (pixelWidth > 8000 || pixelHeight > 8000)
            {
                scale = Math.Min(8000.0 / pixelWidth, 8000.0 / pixelHeight);
                pixelWidth = (int)(pixelWidth * scale);
                pixelHeight = (int)(pixelHeight * scale);
            }

            // Render with VisualBrush approach for accurate capture
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                // Draw background
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                    null,
                    new Rect(0, 0, pixelWidth, pixelHeight));

                // Draw grid pattern
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(48, 96, 108, 128)), 1);
                gridPen.Freeze();
                int gridSize = ViewModel.GridSize;
                double scaledGridSize = gridSize * scale;
                for (double x = 0; x < pixelWidth; x += scaledGridSize)
                {
                    context.DrawLine(gridPen, new Point(x, 0), new Point(x, pixelHeight));
                }
                for (double y = 0; y < pixelHeight; y += scaledGridSize)
                {
                    context.DrawLine(gridPen, new Point(0, y), new Point(pixelWidth, y));
                }

                // Apply scale transform for the content
                if (scale != 1.0)
                {
                    context.PushTransform(new ScaleTransform(scale, scale));
                }

                // Create a visual brush from the entire canvas container
                var visualBrush = new VisualBrush(canvasContainer)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = new Rect(0, 0, contentWidth, contentHeight)
                };

                context.DrawRectangle(visualBrush, null, new Rect(0, 0, contentWidth, contentHeight));

                if (scale != 1.0)
                {
                    context.Pop();
                }
            }

            // Restore original transform
            ViewModel.PanX = originalPanX;
            ViewModel.PanY = originalPanY;
            ViewModel.ZoomLevel = originalZoom;

            var renderBitmap = new RenderTargetBitmap(
                pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            // Show save dialog
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All Files (*.*)|*.*",
                Title = "Export Diagram to Image",
                FileName = $"{ViewModel.ProjectName}.png",
                DefaultExt = "png"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder;
                var extension = System.IO.Path.GetExtension(dialog.FileName).ToLower();

                if (extension == ".jpg" || extension == ".jpeg")
                {
                    encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                }
                else
                {
                    encoder = new PngBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = File.Create(dialog.FileName))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show($"Image exported successfully to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Export error: {ex.Message}");
            MessageBox.Show($"Failed to export image: {ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
