using CommunityToolkit.Mvvm.ComponentModel;

namespace EquipmentStatusTracker.WPF.Models;

public class ProjectData
{
    public string ProjectName { get; set; } = "Untitled Project";
    public List<Equipment> Equipment { get; set; } = new();
    public List<HistoryEntry> History { get; set; } = new();
    public List<PipeConnection> Pipes { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<EquipmentGroup> Groups { get; set; } = new();
    public DateTime? LastSaved { get; set; }
}

public class PipeConnection
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}

public enum ConnectionType
{
    Pipe,
    Electrical
}

public partial class Connection : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _sourceEquipmentId = string.Empty;

    [ObservableProperty]
    private string _targetEquipmentId = string.Empty;

    [ObservableProperty]
    private ConnectionType _type;

    [ObservableProperty]
    private double _x1;

    [ObservableProperty]
    private double _y1;

    [ObservableProperty]
    private double _x2;

    [ObservableProperty]
    private double _y2;

    [ObservableProperty]
    private bool _isEnergized;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _sourceAnchor = "Center";

    [ObservableProperty]
    private string _targetAnchor = "Center";

    public string StrokeColor => Type == ConnectionType.Electrical 
        ? (IsEnergized ? "#FFFFD700" : "#FF666666")
        : "#FF0066CC";

    public double StrokeThickness => Type == ConnectionType.Electrical ? 3 : 5;

    public string? StrokeDashArray => Type == ConnectionType.Electrical ? "4,2" : null;

    partial void OnIsEnergizedChanged(bool value)
    {
        OnPropertyChanged(nameof(StrokeColor));
        OnPropertyChanged(nameof(StrokeThickness));
    }

    partial void OnTypeChanged(ConnectionType value)
    {
        OnPropertyChanged(nameof(StrokeColor));
        OnPropertyChanged(nameof(StrokeThickness));
        OnPropertyChanged(nameof(StrokeDashArray));
    }
}

public partial class EquipmentGroup : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = "Group";

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width = 100;

    [ObservableProperty]
    private double _height = 100;

    [ObservableProperty]
    private bool _isSelected;

    public List<string> EquipmentIds { get; set; } = new();

    public string BorderColor => IsSelected ? "#FF3B82F6" : "#FF4A5568";
    public string BackgroundColor => IsSelected ? "#203B82F6" : "#104A5568";
    public string TypeLabel => Type.ToUpper();

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(BackgroundColor));
    }
}
