using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Views;

/// <summary>
/// Partial class containing keyboard and mouse input handling.
/// </summary>
public partial class MainWindow
{
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
}
