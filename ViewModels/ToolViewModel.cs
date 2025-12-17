using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.ViewModels;

/// <summary>
/// ViewModel for managing edit mode, tool selection, and connection creation state.
/// </summary>
public partial class ToolViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _selectedTool = "Select";

    [ObservableProperty]
    private string _selectedConnectionTool = "";

    [ObservableProperty]
    private Equipment? _connectionSource;

    [ObservableProperty]
    private string _connectionHint = "";

    // Pending anchor points for connection creation
    private string _pendingSourceAnchor = "Center";
    private string _pendingTargetAnchor = "Center";

    public string PendingSourceAnchor => _pendingSourceAnchor;
    public string PendingTargetAnchor => _pendingTargetAnchor;

    public AnchorPoint[] AnchorPointOptions { get; } = System.Enum.GetValues<AnchorPoint>();

    /// <summary>
    /// Toggles edit mode on/off.
    /// </summary>
    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
        if (!IsEditMode)
        {
            SelectedTool = "Select";
            SelectedConnectionTool = "";
            CancelConnection();
        }
    }

    /// <summary>
    /// Selects a placement/manipulation tool.
    /// </summary>
    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (SelectedTool == tool) return;

        SelectedTool = tool;
        if (!string.IsNullOrEmpty(SelectedConnectionTool))
        {
            SelectedConnectionTool = "";
            CancelConnection();
        }
    }

    /// <summary>
    /// Selects a connection drawing tool.
    /// </summary>
    [RelayCommand]
    private void SelectConnectionTool(string tool)
    {
        if (SelectedConnectionTool == tool) return;

        SelectedConnectionTool = tool;
        if (!string.IsNullOrEmpty(SelectedTool))
        {
            SelectedTool = "";
        }
        CancelConnection();
    }

    /// <summary>
    /// Sets the pending source anchor for the next connection.
    /// </summary>
    public void SetPendingSourceAnchor(string anchor)
    {
        _pendingSourceAnchor = anchor;
    }

    /// <summary>
    /// Sets the pending target anchor for the next connection.
    /// </summary>
    public void SetPendingTargetAnchor(string anchor)
    {
        _pendingTargetAnchor = anchor;
    }

    /// <summary>
    /// Handles a click on equipment during connection creation.
    /// Returns true if the click was handled as part of connection creation.
    /// </summary>
    public ConnectionClickResult HandleConnectionClick(Equipment equipment)
    {
        if (string.IsNullOrEmpty(SelectedConnectionTool))
        {
            return ConnectionClickResult.NotHandled;
        }

        if (ConnectionSource == null)
        {
            ConnectionSource = equipment;
            ConnectionHint = $"Select target for {SelectedConnectionTool} connection from {equipment.Name}";
            return ConnectionClickResult.SourceSelected;
        }
        else if (ConnectionSource != equipment)
        {
            var result = new ConnectionClickResult
            {
                Handled = true,
                IsComplete = true,
                Source = ConnectionSource,
                Target = equipment,
                SourceAnchor = _pendingSourceAnchor,
                TargetAnchor = _pendingTargetAnchor,
                ConnectionType = SelectedConnectionTool
            };
            CancelConnection();
            return result;
        }

        return ConnectionClickResult.NotHandled;
    }

    /// <summary>
    /// Cancels the current connection creation operation.
    /// </summary>
    public void CancelConnection()
    {
        ConnectionSource = null;
        ConnectionHint = "";
        _pendingSourceAnchor = "Center";
        _pendingTargetAnchor = "Center";
    }

    /// <summary>
    /// Checks if currently in connection creation mode.
    /// </summary>
    public bool IsCreatingConnection => ConnectionSource != null;
}

/// <summary>
/// Result of a connection click operation.
/// </summary>
public struct ConnectionClickResult
{
    public bool Handled;
    public bool IsComplete;
    public Equipment? Source;
    public Equipment? Target;
    public string SourceAnchor;
    public string TargetAnchor;
    public string ConnectionType;

    public static ConnectionClickResult NotHandled => new() { Handled = false };
    public static ConnectionClickResult SourceSelected => new() { Handled = true, IsComplete = false };
}
