using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using EquipmentStatusTracker.WPF.Models;
using EquipmentStatusTracker.WPF.Services;

namespace EquipmentStatusTracker.WPF.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ProjectService _projectService = new();
    private readonly UndoRedoService _undoRedoService = new();
    private readonly EventHandler? _undoRedoStateChangedHandler;
    private bool _disposed;

    [ObservableProperty]
    private string _projectName = "Untitled Project";

    [ObservableProperty]
    private ObservableCollection<Equipment> _equipmentCollection = new();

    [ObservableProperty]
    private ObservableCollection<Equipment> _filteredEquipment = new();

    [ObservableProperty]
    private ObservableCollection<HistoryEntry> _history = new();

    [ObservableProperty]
    private ObservableCollection<Connection> _connections = new();

    [ObservableProperty]
    private ObservableCollection<EquipmentGroup> _groups = new();

    [ObservableProperty]
    private ObservableCollection<CanvasLabel> _labels = new();

    [ObservableProperty]
    private ObservableCollection<Layer> _layers = new() { Layer.CreateDefault() };

    [ObservableProperty]
    private Layer? _activeLayer;

    // Filtered collections for layer visibility
    public IEnumerable<Equipment> VisibleEquipment =>
        EquipmentCollection.Where(e => IsLayerVisible(e.LayerId));

    public IEnumerable<Connection> VisibleConnections =>
        Connections.Where(c => IsLayerVisible(c.LayerId));

    public IEnumerable<EquipmentGroup> VisibleGroups =>
        Groups.Where(g => IsLayerVisible(g.LayerId));

    public IEnumerable<CanvasLabel> VisibleLabels =>
        Labels.Where(l => IsLayerVisible(l.LayerId));

    private bool IsLayerVisible(string layerId)
    {
        var layer = Layers.FirstOrDefault(l => l.Id == layerId);
        return layer?.IsVisible ?? true;
    }

    private bool IsLayerLocked(string layerId)
    {
        var layer = Layers.FirstOrDefault(l => l.Id == layerId);
        return layer?.IsLocked ?? false;
    }

    private void NotifyVisibleCollectionsChanged()
    {
        OnPropertyChanged(nameof(VisibleEquipment));
        OnPropertyChanged(nameof(VisibleConnections));
        OnPropertyChanged(nameof(VisibleGroups));
        OnPropertyChanged(nameof(VisibleLabels));
    }

    [ObservableProperty]
    private CanvasLabel? _selectedLabel;

    [ObservableProperty]
    private Equipment? _selectedEquipment;
    
    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        // Ensure visual selection state matches SelectedEquipment
        foreach (var eq in EquipmentCollection)
        {
            eq.IsSelected = eq == value && _selectedEquipmentItems.Contains(eq);
        }
    }

    [ObservableProperty]
    private Connection? _selectedConnection;

    [ObservableProperty]
    private EquipmentGroup? _selectedGroup;

    [ObservableProperty]
    private bool _isDetailPanelOpen;

    [ObservableProperty]
    private bool _isHistoryPanelOpen;

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isWelcomeVisible = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _currentFilter = "all";

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panX = 50;

    [ObservableProperty]
    private double _panY = 50;

    // Mouse position in canvas coordinates (for paste at cursor)
    public double MouseCanvasX { get; set; }
    public double MouseCanvasY { get; set; }

    // Edit mode properties
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

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private int _gridSize = 20;

    // Viewport dimensions (set by the View)
    [ObservableProperty]
    private double _viewportWidth = 800;

    [ObservableProperty]
    private double _viewportHeight = 600;

    private string _pendingSourceAnchor = "Center";
    private string _pendingTargetAnchor = "Center";

    public int[] GridSizeOptions { get; } = { 10, 20, 30, 40, 50 };

    public AnchorPoint[] AnchorPointOptions { get; } = Enum.GetValues<AnchorPoint>();

    // Selection
    private List<Equipment> _selectedEquipmentItems = new();
    private List<Equipment> _clipboardEquipment = new();
    private List<Connection> _clipboardConnections = new();

    // Move tracking for undo
    private Dictionary<string, (double X, double Y)> _moveStartPositions = new();

    public int NormalCount => EquipmentCollection.Count(e => e.Status == EquipmentStatus.Normal);
    public int AbnormalCount => EquipmentCollection.Count(e => e.Status == EquipmentStatus.Abnormal);
    public int WarningCount => EquipmentCollection.Count(e => e.Status == EquipmentStatus.Warning || e.Status == EquipmentStatus.Unknown);
    public int SelectedCount => _selectedEquipmentItems.Count;

    public bool CanUndo => _undoRedoService.CanUndo;
    public bool CanRedo => _undoRedoService.CanRedo;

    public MainViewModel()
    {
        _undoRedoStateChangedHandler = (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        };
        _undoRedoService.StateChanged += _undoRedoStateChangedHandler;

        // Try to load autosave
        var autoSaved = _projectService.LoadAutoSave();
        if (autoSaved != null && autoSaved.Equipment.Count > 0)
        {
            LoadProject(autoSaved);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Unsubscribe from events to prevent memory leaks
            if (_undoRedoStateChangedHandler != null)
            {
                _undoRedoService.StateChanged -= _undoRedoStateChangedHandler;
            }
        }

        _disposed = true;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnCurrentFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedConnectionChanged(Connection? value)
    {
        // Deselect previous
        foreach (var conn in Connections)
        {
            conn.IsSelected = conn == value;
        }
    }

    partial void OnSelectedGroupChanged(EquipmentGroup? value)
    {
        foreach (var group in Groups)
        {
            group.IsSelected = group == value;
        }

        // Open detail panel when group is selected, close equipment selection
        if (value != null)
        {
            SelectedEquipment = null;
            SelectedLabel = null;
            IsHistoryPanelOpen = false;
            IsDetailPanelOpen = true;
        }
    }

    partial void OnSelectedLabelChanged(CanvasLabel? value)
    {
        foreach (var label in Labels)
        {
            label.IsSelected = label == value;
        }

        // Open detail panel when label is selected
        if (value != null)
        {
            SelectedEquipment = null;
            SelectedGroup = null;
            IsHistoryPanelOpen = false;
            IsDetailPanelOpen = true;
        }
    }

    partial void OnGridSizeChanging(int value)
    {
        // Prevent division by zero - enforce minimum grid size of 1
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(GridSize), "Grid size must be at least 1");
        }
    }

    private void ApplyFilter()
    {
        var filtered = EquipmentCollection.Where(eq =>
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!eq.Name.ToLower().Contains(searchLower) && 
                    !eq.Type.ToString().ToLower().Contains(searchLower))
                    return false;
            }

            return CurrentFilter switch
            {
                "abnormal" => eq.Status == EquipmentStatus.Abnormal,
                "valve" => eq.Type == EquipmentType.Valve,
                "breaker" => eq.Type == EquipmentType.Breaker,
                _ => true
            };
        });

        FilteredEquipment = new ObservableCollection<Equipment>(filtered);
    }

    private void UpdateStatusCounts()
    {
        OnPropertyChanged(nameof(NormalCount));
        OnPropertyChanged(nameof(AbnormalCount));
        OnPropertyChanged(nameof(WarningCount));
    }

    private void LoadProject(ProjectData project)
    {
        ProjectName = project.ProjectName;
        EquipmentCollection = new ObservableCollection<Equipment>(project.Equipment);
        History = new ObservableCollection<HistoryEntry>(project.History);
        Connections = new ObservableCollection<Connection>(project.Connections ?? new List<Connection>());
        Groups = new ObservableCollection<EquipmentGroup>(project.Groups ?? new List<EquipmentGroup>());
        Labels = new ObservableCollection<CanvasLabel>(project.Labels ?? new List<CanvasLabel>());

        // Initialize layers - migrate old projects
        if (project.Layers == null || project.Layers.Count == 0)
        {
            Layers = new ObservableCollection<Layer> { Layer.CreateDefault() };

            // Ensure all existing items have default layer
            foreach (var eq in EquipmentCollection)
                if (string.IsNullOrEmpty(eq.LayerId)) eq.LayerId = "default";
            foreach (var conn in Connections)
                if (string.IsNullOrEmpty(conn.LayerId)) conn.LayerId = "default";
            foreach (var group in Groups)
                if (string.IsNullOrEmpty(group.LayerId)) group.LayerId = "default";
            foreach (var label in Labels)
                if (string.IsNullOrEmpty(label.LayerId)) label.LayerId = "default";
        }
        else
        {
            Layers = new ObservableCollection<Layer>(project.Layers);
        }
        ActiveLayer = Layers.FirstOrDefault();

        IsWelcomeVisible = false;
        _undoRedoService.Clear();
        ApplyFilter();
        UpdateStatusCounts();
        RecalculateElectricalStatus();
        NotifyVisibleCollectionsChanged();
    }

    [RelayCommand]
    private void NewProject()
    {
        ProjectName = "New Project";
        EquipmentCollection.Clear();
        History.Clear();
        Connections.Clear();
        Groups.Clear();
        Labels.Clear();
        Layers = new ObservableCollection<Layer> { Layer.CreateDefault() };
        ActiveLayer = Layers.FirstOrDefault();
        IsWelcomeVisible = false;
        _undoRedoService.Clear();
        ClearSelection();
        ApplyFilter();
        UpdateStatusCounts();
        NotifyVisibleCollectionsChanged();
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Open Project",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            var project = _projectService.LoadStatus(dialog.FileName);
            if (project != null)
            {
                // Use filename as project name if project name is empty or default
                if (string.IsNullOrWhiteSpace(project.ProjectName) || 
                    project.ProjectName == "New Project" || 
                    project.ProjectName == "Untitled Project")
                {
                    project.ProjectName = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
                
                LoadProject(project);
                SaveAutoSave();
            }
        }
    }

    [RelayCommand]
    private void LoadDemo()
    {
        var project = ProjectService.CreateDemoProject();
        LoadProject(project);
        SaveAutoSave();
    }

    [RelayCommand]
    private void SaveProject()
    {
        // Sanitize filename - remove invalid characters
        var sanitizedName = string.Join("_", ProjectName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = "Untitled Project";
        }

        var dialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Save Project",
            FileName = $"{sanitizedName}.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            DefaultExt = "json"
        };

        if (dialog.ShowDialog() == true)
        {
            var project = new ProjectData
            {
                ProjectName = ProjectName,
                Equipment = EquipmentCollection.ToList(),
                History = History.ToList(),
                Connections = Connections.ToList(),
                Groups = Groups.ToList(),
                Labels = Labels.ToList(),
                Layers = Layers.ToList()
            };
            _projectService.SaveStatus(project, dialog.FileName);
            
            // Update project name from filename if it was changed
            var savedName = Path.GetFileNameWithoutExtension(dialog.FileName);
            if (!string.IsNullOrEmpty(savedName))
            {
                ProjectName = savedName;
            }
        }
    }


    [RelayCommand]
    private void ToggleHistory()
    {
        IsHistoryPanelOpen = !IsHistoryPanelOpen;
        if (IsHistoryPanelOpen)
        {
            IsDetailPanelOpen = false;
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private void SelectEquipment(Equipment? equipment)
    {
        SelectedEquipment = equipment;
        if (equipment != null)
        {
            // Clear group and label selection when selecting equipment
            SelectedGroup = null;
            SelectedLabel = null;
            IsHistoryPanelOpen = false;
            IsDetailPanelOpen = true;
        }
        else
        {
            IsDetailPanelOpen = false;
        }
    }

    [RelayCommand]
    private void CloseDetailPanel()
    {
        SelectedEquipment = null;
        IsDetailPanelOpen = false;
    }

    [RelayCommand]
    private void ToggleDetailPanel()
    {
        if (IsDetailPanelOpen)
        {
            SelectedEquipment = null;
            IsDetailPanelOpen = false;
        }
        else if (SelectedEquipment != null)
        {
            IsHistoryPanelOpen = false;
            IsDetailPanelOpen = true;
        }
    }

    [RelayCommand]
    private void CloseHistoryPanel()
    {
        IsHistoryPanelOpen = false;
    }

    [RelayCommand]
    private void SetPosition(string position)
    {
        if (SelectedEquipment == null) return;

        // Validate that position is valid for this equipment type
        var validPositions = SelectedEquipment.GetPositionOptions();
        if (!validPositions.Contains(position, StringComparer.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid position '{position}' for equipment type {SelectedEquipment.Type}. Valid options: {string.Join(", ", validPositions)}");
            return;
        }

        var oldPosition = SelectedEquipment.CurrentPosition;
        if (string.Equals(oldPosition, position, StringComparison.OrdinalIgnoreCase)) return;

        SelectedEquipment.CurrentPosition = position;
        SelectedEquipment.LastUpdated = DateTime.Now;

        History.Insert(0, new HistoryEntry
        {
            EquipmentId = SelectedEquipment.Id,
            EquipmentName = SelectedEquipment.Name,
            FromPosition = oldPosition,
            ToPosition = position,
            Timestamp = DateTime.Now
        });

        while (History.Count > 1000)
        {
            History.RemoveAt(History.Count - 1);
        }

        UpdateStatusCounts();
        ApplyFilter();
        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    [RelayCommand]
    private void SetNormalPosition(string position)
    {
        if (SelectedEquipment == null) return;

        // Validate that position is valid for this equipment type
        var validPositions = SelectedEquipment.GetPositionOptions();
        if (!validPositions.Contains(position, StringComparer.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid normal position '{position}' for equipment type {SelectedEquipment.Type}. Valid options: {string.Join(", ", validPositions)}");
            return;
        }

        SelectedEquipment.NormalPosition = position;
        SelectedEquipment.LastUpdated = DateTime.Now;

        UpdateStatusCounts();
        ApplyFilter();
        SaveAutoSave();
    }

    [RelayCommand]
    private void SetLOTO(bool isLOTO)
    {
        if (SelectedEquipment == null) return;

        SelectedEquipment.IsLOTO = isLOTO;
        SelectedEquipment.LastUpdated = DateTime.Now;
        SaveAutoSave();
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        CurrentFilter = filter;
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
    private void FitToView()
    {
        if (EquipmentCollection.Count == 0)
        {
            // No equipment, reset to default
            ZoomLevel = 1.0;
            PanX = 50;
            PanY = 50;
            return;
        }

        // Calculate bounding box of all equipment
        double minX = EquipmentCollection.Min(e => e.X);
        double minY = EquipmentCollection.Min(e => e.Y);
        double maxX = EquipmentCollection.Max(e => e.X + e.Width);
        double maxY = EquipmentCollection.Max(e => e.Y + e.Height);

        // Include groups in bounding box
        if (Groups.Count > 0)
        {
            minX = Math.Min(minX, Groups.Min(g => g.X));
            minY = Math.Min(minY, Groups.Min(g => g.Y));
            maxX = Math.Max(maxX, Groups.Max(g => g.X + g.Width));
            maxY = Math.Max(maxY, Groups.Max(g => g.Y + g.Height));
        }

        double contentWidth = maxX - minX;
        double contentHeight = maxY - minY;

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

    [RelayCommand]
    private void ExportHistory()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Export History",
            FileName = "equipment-history.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            _projectService.ExportHistory(History.ToList(), dialog.FileName);
        }
    }

    [RelayCommand]
    private void ExportEquipmentCsv()
    {
        if (EquipmentCollection.Count == 0)
        {
            System.Windows.MessageBox.Show("No equipment to export.", "Export Equipment",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Export Equipment Status",
            FileName = $"{ProjectName}-equipment.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            _projectService.ExportEquipmentCsv(EquipmentCollection.ToList(), Layers.ToList(), dialog.FileName);
            System.Windows.MessageBox.Show($"Equipment exported to:\n{dialog.FileName}", "Export Complete",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    // Edit Mode Commands
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

    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (SelectedTool == tool) return; // Already selected
        
        SelectedTool = tool;
        if (!string.IsNullOrEmpty(SelectedConnectionTool))
        {
            SelectedConnectionTool = "";
            CancelConnection();
        }
    }

    [RelayCommand]
    private void SelectConnectionTool(string tool)
    {
        if (SelectedConnectionTool == tool) return; // Already selected
        
        SelectedConnectionTool = tool;
        if (!string.IsNullOrEmpty(SelectedTool))
        {
            SelectedTool = "";
        }
        CancelConnection();
    }

    [RelayCommand]
    private void Undo()
    {
        _undoRedoService.Undo();
        RecalculateElectricalStatus();
        UpdateAllConnectionPositions();
        SaveAutoSave();
    }

    [RelayCommand]
    private void Redo()
    {
        _undoRedoService.Redo();
        RecalculateElectricalStatus();
        UpdateAllConnectionPositions();
        SaveAutoSave();
    }

    [RelayCommand]
    private void AddLayer()
    {
        var layer = new Layer
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Layer {Layers.Count + 1}",
            Order = Layers.Count,
            IsVisible = true,
            IsLocked = false
        };
        Layers.Add(layer);
        ActiveLayer = layer;
        NotifyVisibleCollectionsChanged();
        SaveAutoSave();
    }

    [RelayCommand]
    private void DeleteLayer(Layer layer)
    {
        if (layer.Id == "default") return; // Cannot delete default layer

        // Move all items to default layer
        foreach (var eq in EquipmentCollection.Where(e => e.LayerId == layer.Id))
            eq.LayerId = "default";
        foreach (var conn in Connections.Where(c => c.LayerId == layer.Id))
            conn.LayerId = "default";
        foreach (var group in Groups.Where(g => g.LayerId == layer.Id))
            group.LayerId = "default";
        foreach (var label in Labels.Where(l => l.LayerId == layer.Id))
            label.LayerId = "default";

        Layers.Remove(layer);
        if (ActiveLayer == layer)
            ActiveLayer = Layers.FirstOrDefault();

        NotifyVisibleCollectionsChanged();
        SaveAutoSave();
    }

    [RelayCommand]
    private void ToggleLayerVisibility(Layer layer)
    {
        layer.IsVisible = !layer.IsVisible;
        NotifyVisibleCollectionsChanged();
        SaveAutoSave();
    }

    [RelayCommand]
    private void ToggleLayerLock(Layer layer)
    {
        layer.IsLocked = !layer.IsLocked;
        SaveAutoSave();
    }

    [RelayCommand]
    private void SetActiveLayer(Layer layer)
    {
        ActiveLayer = layer;
    }

    public void AddEquipmentAtPosition(EquipmentType type, double x, double y)
    {
        // Custom sizes per equipment type
        double width = type switch
        {
            EquipmentType.BusBar => 120,
            EquipmentType.Junction => 30,
            _ => 50
        };
        double height = type switch
        {
            EquipmentType.BusBar => 20,
            EquipmentType.Junction => 30,
            _ => 50
        };

        // Snap the click point (which becomes the center) to grid
        if (SnapToGrid)
        {
            x = Math.Round(x / GridSize) * GridSize;
            y = Math.Round(y / GridSize) * GridSize;
        }

        // Click point is center, calculate top-left position
        double topLeftX = x - width / 2;
        double topLeftY = y - height / 2;

        var equipment = new Equipment
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{type}-{EquipmentCollection.Count + 1}",
            Type = type,
            NormalPosition = Equipment.GetDefaultNormalPosition(type),
            CurrentPosition = Equipment.GetDefaultNormalPosition(type),
            X = topLeftX,
            Y = topLeftY,
            Width = width,
            Height = height,
            LayerId = ActiveLayer?.Id ?? "default"
        };

        EquipmentCollection.Add(equipment);
        NotifyVisibleCollectionsChanged();

        // Record undo action
        _undoRedoService.Record(new GenericAction(
            () => EquipmentCollection.Remove(equipment),
            () => EquipmentCollection.Add(equipment),
            $"Add {type}"
        ));

        ApplyFilter();
        UpdateStatusCounts();
        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    public void AddLabelAtPosition(double x, double y)
    {
        // Snap to grid if enabled
        if (SnapToGrid)
        {
            x = Math.Round(x / GridSize) * GridSize;
            y = Math.Round(y / GridSize) * GridSize;
        }

        var label = new CanvasLabel
        {
            Id = Guid.NewGuid().ToString(),
            Text = "New Label",
            X = x,
            Y = y,
            FontSize = 14,
            Color = "#FFFFFF",
            LayerId = ActiveLayer?.Id ?? "default"
        };

        Labels.Add(label);
        NotifyVisibleCollectionsChanged();
        SelectedLabel = label;

        _undoRedoService.Record(new GenericAction(
            () => Labels.Remove(label),
            () => Labels.Add(label),
            "Add label"
        ));

        SaveAutoSave();
    }

    [RelayCommand]
    private void DeleteSelectedLabel()
    {
        if (SelectedLabel == null) return;

        var label = SelectedLabel;
        Labels.Remove(label);

        _undoRedoService.Record(new GenericAction(
            () => Labels.Add(label),
            () => Labels.Remove(label),
            "Delete label"
        ));

        SelectedLabel = null;
        NotifyVisibleCollectionsChanged();
        SaveAutoSave();
    }

    public void BeginMoveEquipment(IEnumerable<Equipment> equipment)
    {
        _moveStartPositions.Clear();
        foreach (var eq in equipment)
        {
            _moveStartPositions[eq.Id] = (eq.X, eq.Y);
        }
    }

    public (double X, double Y)? GetOriginalPosition(string equipmentId)
    {
        if (_moveStartPositions.TryGetValue(equipmentId, out var pos))
        {
            return pos;
        }
        return null;
    }

    public void MoveEquipment(Equipment equipment, double newX, double newY, bool saveAfter = true)
    {
        if (SnapToGrid)
        {
            // Snap the GridAnchor point to grid, not the top-left corner
            var (anchorOffsetX, anchorOffsetY) = equipment.GetAnchorOffset(equipment.GridAnchor);
            double anchorX = Math.Round((newX + anchorOffsetX) / GridSize) * GridSize;
            double anchorY = Math.Round((newY + anchorOffsetY) / GridSize) * GridSize;
            newX = anchorX - anchorOffsetX;
            newY = anchorY - anchorOffsetY;
        }

        equipment.X = newX;
        equipment.Y = newY;

        UpdateConnectionsForEquipment(equipment.Id);
        UpdateEquipmentGroupMembership(equipment);

        if (saveAfter)
        {
            SaveAutoSave();
        }
    }

    public void EndMoveEquipment(IEnumerable<Equipment> equipment)
    {
        var movedEquipment = equipment.ToList();
        var startPositions = new Dictionary<string, (double X, double Y)>(_moveStartPositions);
        var endPositions = movedEquipment.ToDictionary(e => e.Id, e => (e.X, e.Y));

        // Check if anything actually moved
        bool hasMoved = movedEquipment.Any(e => 
            startPositions.TryGetValue(e.Id, out var start) && 
            (Math.Abs(start.X - e.X) > 0.1 || Math.Abs(start.Y - e.Y) > 0.1));

        if (hasMoved)
        {
            _undoRedoService.Record(new GenericAction(
                () =>
                {
                    foreach (var eq in movedEquipment)
                    {
                        if (startPositions.TryGetValue(eq.Id, out var pos))
                        {
                            eq.X = pos.X;
                            eq.Y = pos.Y;
                            UpdateConnectionsForEquipment(eq.Id);
                        }
                    }
                },
                () =>
                {
                    foreach (var eq in movedEquipment)
                    {
                        if (endPositions.TryGetValue(eq.Id, out var pos))
                        {
                            eq.X = pos.X;
                            eq.Y = pos.Y;
                            UpdateConnectionsForEquipment(eq.Id);
                        }
                    }
                },
                $"Move {movedEquipment.Count} item(s)"
            ));
        }

        _moveStartPositions.Clear();
        SaveAutoSave();
    }

    private void UpdateConnectionsForEquipment(string equipmentId)
    {
        foreach (var conn in Connections.Where(c => c.SourceEquipmentId == equipmentId || c.TargetEquipmentId == equipmentId))
        {
            UpdateConnectionPosition(conn);
        }
    }

    public void DeleteSelectedEquipment()
    {
        if (_selectedEquipmentItems.Count == 0) return;

        var toDelete = _selectedEquipmentItems.ToList();
        var deletedConnections = Connections.Where(c => 
            toDelete.Any(e => e.Id == c.SourceEquipmentId || e.Id == c.TargetEquipmentId)).ToList();

        var compound = new CompoundAction("Delete selection");

        foreach (var conn in deletedConnections)
        {
            var capturedConn = conn;
            compound.AddAction(new GenericAction(
                () => Connections.Add(capturedConn),
                () => Connections.Remove(capturedConn),
                "Delete connection"
            ));
            Connections.Remove(conn);
        }

        foreach (var eq in toDelete)
        {
            var capturedEq = eq;
            compound.AddAction(new GenericAction(
                () => EquipmentCollection.Add(capturedEq),
                () => EquipmentCollection.Remove(capturedEq),
                $"Delete {eq.Name}"
            ));
            EquipmentCollection.Remove(eq);
        }

        _undoRedoService.Record(compound);
        ClearSelection();
        NotifyVisibleCollectionsChanged();
        ApplyFilter();
        UpdateStatusCounts();
        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    // Selection methods
    public void ClearSelection()
    {
        // Clear all equipment selection to prevent stuck selections
        foreach (var eq in EquipmentCollection)
        {
            eq.IsSelected = false;
        }
        _selectedEquipmentItems.Clear();
        SelectedConnection = null;
        SelectedGroup = null;
        OnPropertyChanged(nameof(SelectedCount));
    }

    public void AddToSelection(Equipment equipment)
    {
        if (!_selectedEquipmentItems.Contains(equipment))
        {
            _selectedEquipmentItems.Add(equipment);
            equipment.IsSelected = true;
            OnPropertyChanged(nameof(SelectedCount));
        }
    }

    public void ToggleSelection(Equipment equipment)
    {
        if (_selectedEquipmentItems.Contains(equipment))
        {
            _selectedEquipmentItems.Remove(equipment);
            equipment.IsSelected = false;
        }
        else
        {
            _selectedEquipmentItems.Add(equipment);
            equipment.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    public void SetSelection(Equipment equipment)
    {
        ClearSelection();
        AddToSelection(equipment);
    }

    public void SelectAll()
    {
        ClearSelection();
        foreach (var eq in EquipmentCollection)
        {
            AddToSelection(eq);
        }
    }

    public void SelectInRect(double x1, double y1, double x2, double y2)
    {
        var minX = Math.Min(x1, x2);
        var maxX = Math.Max(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxY = Math.Max(y1, y2);

        foreach (var eq in EquipmentCollection)
        {
            var eqCenterX = eq.X + eq.Width / 2;
            var eqCenterY = eq.Y + eq.Height / 2;

            if (eqCenterX >= minX && eqCenterX <= maxX && eqCenterY >= minY && eqCenterY <= maxY)
            {
                AddToSelection(eq);
            }
        }
    }

    public IReadOnlyList<Equipment> GetSelectedEquipment() => _selectedEquipmentItems.AsReadOnly();

    public void CopySelection()
    {
        _clipboardEquipment = _selectedEquipmentItems.ToList();
        _clipboardConnections = Connections
            .Where(c => _clipboardEquipment.Any(e => e.Id == c.SourceEquipmentId) &&
                       _clipboardEquipment.Any(e => e.Id == c.TargetEquipmentId))
            .ToList();
    }

    public void PasteSelection()
    {
        if (_clipboardEquipment.Count == 0) return;

        ClearSelection();
        var idMap = new Dictionary<string, string>();

        // Calculate the center of the clipboard items to offset from mouse position
        double clipboardCenterX = _clipboardEquipment.Average(e => e.X + e.Width / 2);
        double clipboardCenterY = _clipboardEquipment.Average(e => e.Y + e.Height / 2);

        // Offset to place items centered on mouse position
        double offsetX = MouseCanvasX - clipboardCenterX;
        double offsetY = MouseCanvasY - clipboardCenterY;

        var compound = new CompoundAction("Paste");

        foreach (var original in _clipboardEquipment)
        {
            var newId = Guid.NewGuid().ToString();
            idMap[original.Id] = newId;

            var newEquipment = new Equipment
            {
                Id = newId,
                Name = $"{original.Name}-copy",
                Type = original.Type,
                NormalPosition = original.NormalPosition,
                CurrentPosition = original.CurrentPosition,
                X = original.X + offsetX,
                Y = original.Y + offsetY,
                Width = original.Width,
                Height = original.Height,
                Notes = original.Notes,
                LayerId = ActiveLayer?.Id ?? "default"
            };

            EquipmentCollection.Add(newEquipment);
            AddToSelection(newEquipment);

            var capturedEq = newEquipment;
            compound.AddAction(new GenericAction(
                () => EquipmentCollection.Remove(capturedEq),
                () => EquipmentCollection.Add(capturedEq),
                $"Paste {newEquipment.Name}"
            ));
        }

        // Recreate connections between pasted items
        foreach (var original in _clipboardConnections)
        {
            if (idMap.TryGetValue(original.SourceEquipmentId, out var newSourceId) &&
                idMap.TryGetValue(original.TargetEquipmentId, out var newTargetId))
            {
                var newConnection = new Connection
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceEquipmentId = newSourceId,
                    TargetEquipmentId = newTargetId,
                    Type = original.Type,
                    SourceAnchor = original.SourceAnchor,
                    TargetAnchor = original.TargetAnchor
                };

                Connections.Add(newConnection);
                UpdateConnectionPosition(newConnection);

                var capturedConn = newConnection;
                compound.AddAction(new GenericAction(
                    () => Connections.Remove(capturedConn),
                    () => Connections.Add(capturedConn),
                    "Paste connection"
                ));
            }
        }

        _undoRedoService.Record(compound);
        NotifyVisibleCollectionsChanged();
        ApplyFilter();
        UpdateStatusCounts();
        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    // Connection methods
    public void SetPendingSourceAnchor(string anchor)
    {
        _pendingSourceAnchor = anchor;
    }

    public void SetPendingTargetAnchor(string anchor)
    {
        _pendingTargetAnchor = anchor;
    }

    public bool TryHandleConnectionClick(Equipment equipment)
    {
        if (string.IsNullOrEmpty(SelectedConnectionTool)) return false;

        if (ConnectionSource == null)
        {
            ConnectionSource = equipment;
            ConnectionHint = $"Select target for {SelectedConnectionTool} connection from {equipment.Name}";
            return true;
        }
        else if (ConnectionSource != equipment)
        {
            CreateConnection(ConnectionSource, equipment);
            CancelConnection();
            return true;
        }

        return false;
    }

    private void CreateConnection(Equipment source, Equipment target)
    {
        var type = SelectedConnectionTool == "Electrical" ? ConnectionType.Electrical : ConnectionType.Pipe;
        
        var connection = new Connection
        {
            Id = Guid.NewGuid().ToString(),
            SourceEquipmentId = source.Id,
            TargetEquipmentId = target.Id,
            Type = type,
            SourceAnchor = _pendingSourceAnchor,
            TargetAnchor = _pendingTargetAnchor,
            LayerId = ActiveLayer?.Id ?? "default"
        };

        UpdateConnectionPosition(connection);
        Connections.Add(connection);
        NotifyVisibleCollectionsChanged();

        _undoRedoService.Record(new GenericAction(
            () => Connections.Remove(connection),
            () => Connections.Add(connection),
            $"Add {type} connection"
        ));

        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    public void CancelConnection()
    {
        ConnectionSource = null;
        ConnectionHint = "";
        _pendingSourceAnchor = "Center";
        _pendingTargetAnchor = "Center";
    }

    public void DeleteSelectedConnection()
    {
        if (SelectedConnection == null) return;

        var conn = SelectedConnection;
        Connections.Remove(conn);

        _undoRedoService.Record(new GenericAction(
            () => Connections.Add(conn),
            () => Connections.Remove(conn),
            "Delete connection"
        ));

        SelectedConnection = null;
        NotifyVisibleCollectionsChanged();
        RecalculateElectricalStatus();
        SaveAutoSave();
    }

    public void UpdateConnectionPosition(Connection connection)
    {
        var source = EquipmentCollection.FirstOrDefault(e => e.Id == connection.SourceEquipmentId);
        var target = EquipmentCollection.FirstOrDefault(e => e.Id == connection.TargetEquipmentId);

        if (source != null && target != null)
        {
            var sourceCenter = new Point(source.X + source.Width / 2, source.Y + source.Height / 2);
            var targetCenter = new Point(target.X + target.Width / 2, target.Y + target.Height / 2);

            var sourcePoint = GetAnchorPoint(source, connection.SourceAnchor, targetCenter);
            var targetPoint = GetAnchorPoint(target, connection.TargetAnchor, sourceCenter);

            connection.X1 = sourcePoint.X;
            connection.Y1 = sourcePoint.Y;
            connection.X2 = targetPoint.X;
            connection.Y2 = targetPoint.Y;

            // Calculate path data for rendering
            connection.PathData = CalculateConnectionPath(connection, sourcePoint, targetPoint);
        }
    }

    private string CalculateConnectionPath(Connection connection, Point start, Point end)
    {
        // Use invariant culture for path data to ensure proper parsing
        var ic = System.Globalization.CultureInfo.InvariantCulture;

        if (connection.Routing == ConnectionRouting.Straight)
        {
            return string.Format(ic, "M {0},{1} L {2},{3}", start.X, start.Y, end.X, end.Y);
        }

        // Orthogonal routing - create right-angle path
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;

        // Determine if we should go horizontal-first or vertical-first
        // based on anchor positions and relative positions
        bool horizontalFirst = DetermineRoutingDirection(connection, start, end);

        if (horizontalFirst)
        {
            // Horizontal first, then vertical
            double midX = start.X + dx / 2;
            return string.Format(ic, "M {0},{1} L {2},{3} L {4},{5} L {6},{7}",
                start.X, start.Y, midX, start.Y, midX, end.Y, end.X, end.Y);
        }
        else
        {
            // Vertical first, then horizontal
            double midY = start.Y + dy / 2;
            return string.Format(ic, "M {0},{1} L {2},{3} L {4},{5} L {6},{7}",
                start.X, start.Y, start.X, midY, end.X, midY, end.X, end.Y);
        }
    }

    private bool DetermineRoutingDirection(Connection connection, Point start, Point end)
    {
        // Check if anchors suggest a direction
        var sourceAnchor = connection.SourceAnchor;
        var targetAnchor = connection.TargetAnchor;

        // If source is on left/right edge, start horizontal
        if (sourceAnchor.Contains("Left") || sourceAnchor.Contains("Right"))
            return true;

        // If source is on top/bottom edge, start vertical
        if (sourceAnchor.Contains("Top") || sourceAnchor.Contains("Bottom"))
            return false;

        // For center anchors, use the longer dimension
        double dx = Math.Abs(end.X - start.X);
        double dy = Math.Abs(end.Y - start.Y);

        return dx >= dy;
    }

    private Point GetAnchorPoint(Equipment equipment, string anchor, Point towardsPoint)
    {
        // Try to parse as AnchorPoint enum first (new 9-point system)
        if (Enum.TryParse<AnchorPoint>(anchor, out var anchorPoint))
        {
            var (x, y) = equipment.GetAnchorPosition(anchorPoint);
            return new Point(x, y);
        }

        // Legacy support for old 4-point string anchors
        var centerX = equipment.X + equipment.Width / 2;
        var centerY = equipment.Y + equipment.Height / 2;

        return anchor switch
        {
            "Top" => new Point(centerX, equipment.Y),
            "Bottom" => new Point(centerX, equipment.Y + equipment.Height),
            "Left" => new Point(equipment.X, centerY),
            "Right" => new Point(equipment.X + equipment.Width, centerY),
            _ => GetBorderIntersection(equipment, towardsPoint)
        };
    }

    private Point GetBorderIntersection(Equipment equipment, Point target)
    {
        var centerX = equipment.X + equipment.Width / 2;
        var centerY = equipment.Y + equipment.Height / 2;

        var dx = target.X - centerX;
        var dy = target.Y - centerY;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return new Point(centerX, centerY);

        var halfWidth = equipment.Width / 2;
        var halfHeight = equipment.Height / 2;

        double scaleX = Math.Abs(dx) > 0.001 ? halfWidth / Math.Abs(dx) : double.MaxValue;
        double scaleY = Math.Abs(dy) > 0.001 ? halfHeight / Math.Abs(dy) : double.MaxValue;
        double scale = Math.Min(scaleX, scaleY);

        return new Point(centerX + dx * scale, centerY + dy * scale);
    }

    public void UpdateAllConnectionPositions()
    {
        foreach (var conn in Connections)
        {
            UpdateConnectionPosition(conn);
        }
    }

    // Group methods
    [RelayCommand]
    private void CreateGroup()
    {
        if (_selectedEquipmentItems.Count < 2) return;

        var minX = _selectedEquipmentItems.Min(e => e.X);
        var minY = _selectedEquipmentItems.Min(e => e.Y);
        var maxX = _selectedEquipmentItems.Max(e => e.X + e.Width);
        var maxY = _selectedEquipmentItems.Max(e => e.Y + e.Height);

        var group = new EquipmentGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Group {Groups.Count + 1}",
            Type = "Group",
            X = minX - 10,
            Y = minY - 30,
            Width = maxX - minX + 20,
            Height = maxY - minY + 40,
            EquipmentIds = _selectedEquipmentItems.Select(e => e.Id).ToList(),
            LayerId = ActiveLayer?.Id ?? "default"
        };

        Groups.Add(group);
        NotifyVisibleCollectionsChanged();

        _undoRedoService.Record(new GenericAction(
            () => Groups.Remove(group),
            () => Groups.Add(group),
            "Create group"
        ));

        SaveAutoSave();
    }

    public void DeleteSelectedGroup()
    {
        if (SelectedGroup == null) return;

        var group = SelectedGroup;
        Groups.Remove(group);

        _undoRedoService.Record(new GenericAction(
            () => Groups.Add(group),
            () => Groups.Remove(group),
            "Delete group"
        ));

        SelectedGroup = null;
        NotifyVisibleCollectionsChanged();
        SaveAutoSave();
    }

    public void MoveGroup(EquipmentGroup group, double deltaX, double deltaY)
    {
        group.X += deltaX;
        group.Y += deltaY;

        foreach (var eqId in group.EquipmentIds)
        {
            var eq = EquipmentCollection.FirstOrDefault(e => e.Id == eqId);
            if (eq != null)
            {
                eq.X += deltaX;
                eq.Y += deltaY;
                UpdateConnectionsForEquipment(eq.Id);
            }
        }
        
        // After moving the group, check all equipment to see if any should be added
        UpdateGroupMembershipForAllEquipment(group);
    }

    public void ResizeGroup(EquipmentGroup group, double newX, double newY, double newWidth, double newHeight)
    {
        // Calculate the delta for moving equipment
        var deltaX = newX - group.X;
        var deltaY = newY - group.Y;

        group.X = newX;
        group.Y = newY;
        group.Width = newWidth;
        group.Height = newHeight;

        // Move equipment if the group position changed
        if (deltaX != 0 || deltaY != 0)
        {
            foreach (var eqId in group.EquipmentIds)
            {
                var eq = EquipmentCollection.FirstOrDefault(e => e.Id == eqId);
                if (eq != null)
                {
                    eq.X += deltaX;
                    eq.Y += deltaY;
                    UpdateConnectionsForEquipment(eq.Id);
                }
            }
        }
        
        // After resizing, check all equipment to see what should be in the group
        UpdateGroupMembershipForAllEquipment(group);
    }

    public void ResizeEquipment(Equipment equipment, double newX, double newY, double newWidth, double newHeight)
    {
        // Ensure minimum size first
        if (newWidth < 30) newWidth = 30;
        if (newHeight < 30) newHeight = 30;

        if (SnapToGrid)
        {
            // Snap dimensions to grid
            newWidth = Math.Round(newWidth / GridSize) * GridSize;
            newHeight = Math.Round(newHeight / GridSize) * GridSize;
            if (newWidth < 30) newWidth = 30;
            if (newHeight < 30) newHeight = 30;

            // Calculate anchor offset with NEW dimensions
            double anchorOffsetX = equipment.GridAnchor switch
            {
                AnchorPoint.TopLeft or AnchorPoint.MiddleLeft or AnchorPoint.BottomLeft => 0,
                AnchorPoint.TopCenter or AnchorPoint.Center or AnchorPoint.BottomCenter => newWidth / 2,
                _ => newWidth
            };
            double anchorOffsetY = equipment.GridAnchor switch
            {
                AnchorPoint.TopLeft or AnchorPoint.TopCenter or AnchorPoint.TopRight => 0,
                AnchorPoint.MiddleLeft or AnchorPoint.Center or AnchorPoint.MiddleRight => newHeight / 2,
                _ => newHeight
            };

            // Snap the GridAnchor point to grid
            double anchorX = Math.Round((newX + anchorOffsetX) / GridSize) * GridSize;
            double anchorY = Math.Round((newY + anchorOffsetY) / GridSize) * GridSize;
            newX = anchorX - anchorOffsetX;
            newY = anchorY - anchorOffsetY;
        }

        equipment.X = newX;
        equipment.Y = newY;
        equipment.Width = newWidth;
        equipment.Height = newHeight;

        // Update all connections attached to this equipment
        UpdateConnectionsForEquipment(equipment.Id);

        // Update group membership in case equipment moved outside/inside a group
        UpdateEquipmentGroupMembership(equipment);

        SaveAutoSave();
    }

    private void UpdateGroupMembershipForAllEquipment(EquipmentGroup group)
    {
        // Check all equipment to see if it should be in this group
        foreach (var equipment in EquipmentCollection)
        {
            bool isInGroup = IsEquipmentInGroup(equipment, group);
            bool isMember = group.EquipmentIds.Contains(equipment.Id);
            
            if (isInGroup && !isMember)
            {
                // Equipment is inside group but not a member - add it
                group.EquipmentIds.Add(equipment.Id);
            }
            else if (!isInGroup && isMember)
            {
                // Equipment is a member but outside group - remove it
                group.EquipmentIds.Remove(equipment.Id);
            }
        }
    }
    
    private void UpdateEquipmentGroupMembership(Equipment equipment)
    {
        // Check all groups
        foreach (var group in Groups.ToList())
        {
            bool isInGroup = IsEquipmentInGroup(equipment, group);
            bool isMember = group.EquipmentIds.Contains(equipment.Id);
            
            if (isInGroup && !isMember)
            {
                // Equipment moved inside group - add it
                group.EquipmentIds.Add(equipment.Id);
            }
            else if (!isInGroup && isMember)
            {
                // Equipment moved outside group - remove it
                group.EquipmentIds.Remove(equipment.Id);
            }
        }
    }
    
    private bool IsEquipmentInGroup(Equipment equipment, EquipmentGroup group)
    {
        // Check if equipment overlaps with group bounds
        var eqLeft = equipment.X;
        var eqRight = equipment.X + equipment.Width;
        var eqTop = equipment.Y;
        var eqBottom = equipment.Y + equipment.Height;
        
        var groupLeft = group.X;
        var groupRight = group.X + group.Width;
        var groupTop = group.Y;
        var groupBottom = group.Y + group.Height;
        
        // Check if equipment center is within group bounds (more lenient check)
        var eqCenterX = equipment.X + equipment.Width / 2;
        var eqCenterY = equipment.Y + equipment.Height / 2;
        
        if (eqCenterX >= groupLeft && eqCenterX <= groupRight &&
            eqCenterY >= groupTop && eqCenterY <= groupBottom)
        {
            return true;
        }
        
        // Also check if equipment overlaps with group (at least partially inside)
        return !(eqRight < groupLeft || eqLeft > groupRight || eqBottom < groupTop || eqTop > groupBottom);
    }

    // Electrical propagation
    public void RecalculateElectricalStatus()
    {
        // Reset all - set to a different value first to ensure property change fires
        foreach (var eq in EquipmentCollection)
        {
            if (eq.IsEnergized) eq.IsEnergized = false;
        }
        foreach (var conn in Connections.Where(c => c.Type == ConnectionType.Electrical))
        {
            if (conn.IsEnergized) conn.IsEnergized = false;
        }

        // Find power sources
        var sources = EquipmentCollection.Where(e => e.IsPowerSource()).ToList();
        var visited = new HashSet<string>();

        foreach (var source in sources)
        {
            PropagateElectricity(source, visited);
        }
    }

    private void PropagateElectricity(Equipment equipment, HashSet<string> visited)
    {
        if (visited.Contains(equipment.Id)) return;
        visited.Add(equipment.Id);

        equipment.IsEnergized = true;

        // Find electrical connections from this equipment
        var outgoingConnections = Connections
            .Where(c => c.Type == ConnectionType.Electrical && 
                       (c.SourceEquipmentId == equipment.Id || c.TargetEquipmentId == equipment.Id))
            .ToList();

        foreach (var conn in outgoingConnections)
        {
            var otherEquipmentId = conn.SourceEquipmentId == equipment.Id 
                ? conn.TargetEquipmentId 
                : conn.SourceEquipmentId;

            var otherEquipment = EquipmentCollection.FirstOrDefault(e => e.Id == otherEquipmentId);
            if (otherEquipment == null || visited.Contains(otherEquipmentId)) continue;

            // Check if current equipment can conduct
            bool canConduct = equipment.IsPowerSource() || equipment.CanConductElectricity();
            
            if (canConduct)
            {
                conn.IsEnergized = true;
                
                // Check if the other equipment can receive/conduct
                if (otherEquipment.CanConductElectricity() || !IsElectricalEquipment(otherEquipment))
                {
                    PropagateElectricity(otherEquipment, visited);
                }
                else
                {
                    // Mark as visited but not energized (breaker open, etc.)
                    visited.Add(otherEquipmentId);
                }
            }
        }
    }

    private bool IsElectricalEquipment(Equipment eq)
    {
        return eq.Type is EquipmentType.Breaker or EquipmentType.Switch or EquipmentType.ATS
            or EquipmentType.UPS or EquipmentType.PDU or EquipmentType.STS
            or EquipmentType.Transformer or EquipmentType.Generator
            or EquipmentType.BusBar or EquipmentType.Junction;
    }

    private void SaveAutoSave()
    {
        var project = new ProjectData
        {
            ProjectName = ProjectName,
            Equipment = EquipmentCollection.ToList(),
            History = History.ToList(),
            Connections = Connections.ToList(),
            Groups = Groups.ToList(),
            Labels = Labels.ToList(),
            Layers = Layers.ToList()
        };
        _projectService.AutoSave(project);
    }

    public void TriggerAutoSave() => SaveAutoSave();
}
