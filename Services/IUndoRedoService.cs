namespace EquipmentStatusTracker.WPF.Services;

/// <summary>
/// Interface for undo/redo functionality.
/// </summary>
public interface IUndoRedoService
{
    /// <summary>
    /// Gets whether an undo operation is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether a redo operation is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Records an action that can be undone.
    /// </summary>
    void Record(IUndoRedoAction action);

    /// <summary>
    /// Undoes the last recorded action.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone action.
    /// </summary>
    void Redo();

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets description of the action that would be undone.
    /// </summary>
    string GetUndoDescription();

    /// <summary>
    /// Gets description of the action that would be redone.
    /// </summary>
    string GetRedoDescription();
}
