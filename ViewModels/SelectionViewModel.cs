using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.ViewModels;

/// <summary>
/// ViewModel for managing equipment selection state and clipboard operations.
/// </summary>
public partial class SelectionViewModel : ObservableObject
{
    private readonly List<Equipment> _selectedItems = new();
    private List<Equipment> _clipboardEquipment = new();
    private List<Connection> _clipboardConnections = new();

    // Move tracking for undo
    private readonly Dictionary<string, (double X, double Y)> _moveStartPositions = new();

    [ObservableProperty]
    private Equipment? _selectedEquipment;

    [ObservableProperty]
    private Connection? _selectedConnection;

    [ObservableProperty]
    private EquipmentGroup? _selectedGroup;

    [ObservableProperty]
    private CanvasLabel? _selectedLabel;

    public int SelectedCount => _selectedItems.Count;

    public bool HasSelection => _selectedItems.Count > 0;

    public bool HasClipboard => _clipboardEquipment.Count > 0;

    public IReadOnlyList<Equipment> SelectedItems => _selectedItems.AsReadOnly();

    public IReadOnlyList<Equipment> ClipboardEquipment => _clipboardEquipment.AsReadOnly();

    public IReadOnlyList<Connection> ClipboardConnections => _clipboardConnections.AsReadOnly();

    /// <summary>
    /// Clears all selection states.
    /// </summary>
    public void ClearAll(IEnumerable<Equipment> allEquipment)
    {
        foreach (var eq in allEquipment)
        {
            eq.IsSelected = false;
        }
        _selectedItems.Clear();
        SelectedEquipment = null;
        SelectedConnection = null;
        SelectedGroup = null;
        SelectedLabel = null;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>
    /// Clears equipment selection only (not connection/group/label).
    /// </summary>
    public void ClearEquipmentSelection(IEnumerable<Equipment> allEquipment)
    {
        foreach (var eq in allEquipment)
        {
            eq.IsSelected = false;
        }
        _selectedItems.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>
    /// Adds equipment to the selection.
    /// </summary>
    public void AddToSelection(Equipment equipment)
    {
        if (_selectedItems.Contains(equipment)) return;

        _selectedItems.Add(equipment);
        equipment.IsSelected = true;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>
    /// Toggles equipment selection state.
    /// </summary>
    public void ToggleSelection(Equipment equipment)
    {
        if (_selectedItems.Contains(equipment))
        {
            _selectedItems.Remove(equipment);
            equipment.IsSelected = false;
        }
        else
        {
            _selectedItems.Add(equipment);
            equipment.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>
    /// Sets selection to a single equipment item.
    /// </summary>
    public void SetSelection(Equipment equipment, IEnumerable<Equipment> allEquipment)
    {
        ClearEquipmentSelection(allEquipment);
        AddToSelection(equipment);
    }

    /// <summary>
    /// Selects all equipment.
    /// </summary>
    public void SelectAll(IEnumerable<Equipment> allEquipment)
    {
        ClearEquipmentSelection(allEquipment);
        foreach (var eq in allEquipment)
        {
            AddToSelection(eq);
        }
    }

    /// <summary>
    /// Selects equipment within a rectangular region.
    /// </summary>
    public void SelectInRect(double x1, double y1, double x2, double y2, IEnumerable<Equipment> allEquipment)
    {
        var minX = Math.Min(x1, x2);
        var maxX = Math.Max(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxY = Math.Max(y1, y2);

        foreach (var eq in allEquipment)
        {
            var eqCenterX = eq.X + eq.Width / 2;
            var eqCenterY = eq.Y + eq.Height / 2;

            if (eqCenterX >= minX && eqCenterX <= maxX && eqCenterY >= minY && eqCenterY <= maxY)
            {
                AddToSelection(eq);
            }
        }
    }

    /// <summary>
    /// Copies the current selection to clipboard.
    /// </summary>
    public void CopySelection(IEnumerable<Connection> allConnections)
    {
        _clipboardEquipment = _selectedItems.ToList();
        _clipboardConnections = allConnections
            .Where(c => _clipboardEquipment.Any(e => e.Id == c.SourceEquipmentId) &&
                       _clipboardEquipment.Any(e => e.Id == c.TargetEquipmentId))
            .ToList();
        OnPropertyChanged(nameof(HasClipboard));
    }

    /// <summary>
    /// Begins tracking equipment positions for move undo.
    /// </summary>
    public void BeginMove(IEnumerable<Equipment> equipment)
    {
        _moveStartPositions.Clear();
        foreach (var eq in equipment)
        {
            _moveStartPositions[eq.Id] = (eq.X, eq.Y);
        }
    }

    /// <summary>
    /// Gets the original position of equipment before a move operation.
    /// </summary>
    public (double X, double Y)? GetOriginalPosition(string equipmentId)
    {
        return _moveStartPositions.TryGetValue(equipmentId, out var pos) ? pos : null;
    }

    /// <summary>
    /// Gets all tracked move start positions.
    /// </summary>
    public Dictionary<string, (double X, double Y)> GetMoveStartPositions()
    {
        return new Dictionary<string, (double X, double Y)>(_moveStartPositions);
    }

    /// <summary>
    /// Clears move tracking state.
    /// </summary>
    public void EndMove()
    {
        _moveStartPositions.Clear();
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        // Sync visual state - actual implementation in MainViewModel
    }

    partial void OnSelectedConnectionChanged(Connection? value)
    {
        // Sync visual state - actual implementation handled externally
    }

    partial void OnSelectedGroupChanged(EquipmentGroup? value)
    {
        // Sync visual state - actual implementation handled externally
    }

    partial void OnSelectedLabelChanged(CanvasLabel? value)
    {
        // Sync visual state - actual implementation handled externally
    }
}
