using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Views;

/// <summary>
/// Partial class containing canvas element interactions and export functionality.
/// </summary>
public partial class MainWindow
{
    private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.Canvas.ViewportWidth = e.NewSize.Width;
        ViewModel.Canvas.ViewportHeight = e.NewSize.Height;
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

        ViewModel.Canvas.ZoomLevel = 1.5;
        ViewModel.Canvas.PanX = (canvasWidth / 2) - (equipmentCenterX * ViewModel.Canvas.ZoomLevel);
        ViewModel.Canvas.PanY = (canvasHeight / 2) - (equipmentCenterY * ViewModel.Canvas.ZoomLevel);
    }

    private void Equipment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Equipment equipment)
        {
            // Handle connection tool
            if (!string.IsNullOrEmpty(ViewModel.Tool.SelectedConnectionTool))
            {
                if (ViewModel.TryHandleConnectionClick(equipment))
                {
                    e.Handled = true;
                    return;
                }
            }

            // Handle selection with modifiers
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                ViewModel.AddToSelection(equipment);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
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
            if (ViewModel.Tool.IsEditMode && string.IsNullOrEmpty(ViewModel.Tool.SelectedConnectionTool))
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
                if (ViewModel.Tool.ConnectionSource == null)
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

            if (ViewModel.Tool.IsEditMode && string.IsNullOrEmpty(ViewModel.Tool.SelectedConnectionTool))
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

            if (ViewModel.Tool.IsEditMode)
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

            if (ViewModel.Tool.IsEditMode)
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

            // Start dragging in edit mode
            if (ViewModel.Tool.IsEditMode)
            {
                _isDraggingLabel = true;
                _draggedLabel = label;
                _labelDragStartX = label.X;
                _labelDragStartY = label.Y;
                _dragStartPosition = e.GetPosition(LabelsOverlay);
                this.CaptureMouse();
            }

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
            if (ViewModel.Tool.IsEditMode)
            {
                // Check if an equipment type tool is selected
                if (!string.IsNullOrEmpty(ViewModel.Tool.SelectedTool) && ViewModel.Tool.SelectedTool != "Select")
                {
                    var pos = e.GetPosition(EquipmentOverlay);

                    // Handle Label tool
                    if (ViewModel.Tool.SelectedTool == "Label")
                    {
                        ViewModel.AddLabelAtPosition(pos.X, pos.Y);
                        e.Handled = true;
                        return;
                    }

                    // Add equipment at click position
                    if (Enum.TryParse<EquipmentType>(ViewModel.Tool.SelectedTool, out var equipmentType))
                    {
                        ViewModel.AddEquipmentAtPosition(equipmentType, pos.X, pos.Y);
                    }
                    e.Handled = true;
                    return;
                }

                // Start box selection if select tool is active
                if (ViewModel.Tool.SelectedTool == "Select" || string.IsNullOrEmpty(ViewModel.Tool.SelectedTool))
                {
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
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
        // Track mouse position in canvas coordinates for paste-at-cursor
        var screenPos = e.GetPosition(DiagramCanvas);
        ViewModel.MouseCanvasX = (screenPos.X - ViewModel.Canvas.PanX) / ViewModel.Canvas.ZoomLevel;
        ViewModel.MouseCanvasY = (screenPos.Y - ViewModel.Canvas.PanY) / ViewModel.Canvas.ZoomLevel;

        // Middle mouse panning
        if (_isMiddleMousePanning && e.MiddleButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.Canvas.PanX += delta.X;
            ViewModel.Canvas.PanY += delta.Y;

            _lastMousePosition = currentPosition;
            return;
        }

        // Space + drag panning
        if (_isSpacePanning && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.Canvas.PanX += delta.X;
            ViewModel.Canvas.PanY += delta.Y;

            _lastMousePosition = currentPosition;
            return;
        }

        // Left mouse panning
        if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            ViewModel.Canvas.PanX += delta.X;
            ViewModel.Canvas.PanY += delta.Y;

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
            var canvasLeft = (rectLeft - ViewModel.Canvas.PanX) / ViewModel.Canvas.ZoomLevel;
            var canvasTop = (rectTop - ViewModel.Canvas.PanY) / ViewModel.Canvas.ZoomLevel;
            var canvasRight = (rectRight - ViewModel.Canvas.PanX) / ViewModel.Canvas.ZoomLevel;
            var canvasBottom = (rectBottom - ViewModel.Canvas.PanY) / ViewModel.Canvas.ZoomLevel;

            ViewModel.SelectInRect(canvasLeft, canvasTop, canvasRight, canvasBottom);
        }
    }

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 0.1 : -0.1;
        var newZoom = Math.Clamp(ViewModel.Canvas.ZoomLevel + delta, 0.25, 3.0);
        ViewModel.Canvas.ZoomLevel = newZoom;
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
            double originalPanX = ViewModel.Canvas.PanX;
            double originalPanY = ViewModel.Canvas.PanY;
            double originalZoom = ViewModel.Canvas.ZoomLevel;

            // Set zoom to 1 and pan to show content from (0,0) at top-left
            ViewModel.Canvas.ZoomLevel = 1.0;
            ViewModel.Canvas.PanX = -minX;
            ViewModel.Canvas.PanY = -minY;

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
                int gridSize = ViewModel.Canvas.GridSize;
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
            ViewModel.Canvas.PanX = originalPanX;
            ViewModel.Canvas.PanY = originalPanY;
            ViewModel.Canvas.ZoomLevel = originalZoom;

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
