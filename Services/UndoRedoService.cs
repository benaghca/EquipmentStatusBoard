namespace EquipmentStatusTracker.WPF.Services;

public interface IUndoRedoAction
{
    void Undo();
    void Redo();
    string Description { get; }
}

public class GenericAction : IUndoRedoAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;
    public string Description { get; }

    public GenericAction(Action undoAction, Action redoAction, string description = "")
    {
        _undoAction = undoAction;
        _redoAction = redoAction;
        Description = description;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

public class CompoundAction : IUndoRedoAction
{
    private readonly List<IUndoRedoAction> _actions = new();
    public string Description { get; }

    public CompoundAction(string description = "")
    {
        Description = description;
    }

    public void AddAction(IUndoRedoAction action)
    {
        _actions.Add(action);
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }

    public void Redo()
    {
        // Redo in forward order
        foreach (var action in _actions)
        {
            action.Redo();
        }
    }

    public bool HasActions => _actions.Count > 0;
}

public class UndoRedoService
{
    private readonly Stack<IUndoRedoAction> _undoStack = new();
    private readonly Stack<IUndoRedoAction> _redoStack = new();
    private const int MaxStackSize = 100;

    public event EventHandler? StateChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Record(IUndoRedoAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear(); // Clear redo stack when new action is recorded

        // Limit stack size
        if (_undoStack.Count > MaxStackSize)
        {
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < MaxStackSize; i++)
            {
                _undoStack.Push(temp[MaxStackSize - 1 - i]);
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetUndoDescription()
    {
        return CanUndo ? _undoStack.Peek().Description : "";
    }

    public string GetRedoDescription()
    {
        return CanRedo ? _redoStack.Peek().Description : "";
    }
}

