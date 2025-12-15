using CommunityToolkit.Mvvm.ComponentModel;

namespace EquipmentStatusTracker.WPF.Models;

public enum EquipmentType
{
    Valve,
    Breaker,
    Pump,
    Chiller,
    Generator,
    ATS,
    UPS,
    Motor,
    Transformer,
    Switch,
    PDU,
    STS
}

public enum EquipmentStatus
{
    Normal,
    Abnormal,
    Warning,
    Unknown
}

public partial class Equipment : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private EquipmentType _type;
    
    [ObservableProperty]
    private string _normalPosition = string.Empty;
    
    [ObservableProperty]
    private string _currentPosition = string.Empty;
    
    [ObservableProperty]
    private double _x;
    
    [ObservableProperty]
    private double _y;
    
    [ObservableProperty]
    private double _width = 50;
    
    [ObservableProperty]
    private double _height = 50;
    
    [ObservableProperty]
    private string _notes = string.Empty;
    
    [ObservableProperty]
    private DateTime? _lastUpdated;

    [ObservableProperty]
    private bool _isEnergized;

    [ObservableProperty]
    private bool _isSelected;

    public EquipmentStatus Status
    {
        get
        {
            if (string.IsNullOrEmpty(NormalPosition) || NormalPosition == "unknown")
                return EquipmentStatus.Unknown;
            
            if (CurrentPosition.Equals(NormalPosition, StringComparison.OrdinalIgnoreCase))
                return EquipmentStatus.Normal;
            
            if (CurrentPosition.Equals("standby", StringComparison.OrdinalIgnoreCase) ||
                CurrentPosition.Equals("bypass", StringComparison.OrdinalIgnoreCase) ||
                CurrentPosition.Equals("test", StringComparison.OrdinalIgnoreCase))
                return EquipmentStatus.Warning;
            
            return EquipmentStatus.Abnormal;
        }
    }

    /// <summary>
    /// Returns true if the equipment is "ON" but not electrically energized.
    /// Used to show a flashing indicator for equipment that should be powered but isn't.
    /// </summary>
    public bool ShouldFlashNoPower
    {
        get
        {
            // Only flash if not energized
            if (IsEnergized) return false;
            
            // Check if equipment is in an "active" state that would need power
            var activeStates = new[] { "on", "closed", "energized", "available", "normal", "source 1", "source 2" };
            return activeStates.Any(s => CurrentPosition.Equals(s, StringComparison.OrdinalIgnoreCase));
        }
    }

    public string[] PositionOptions => GetPositionOptions();

    partial void OnCurrentPositionChanged(string value)
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ShouldFlashNoPower));
    }

    partial void OnNormalPositionChanged(string value)
    {
        OnPropertyChanged(nameof(Status));
    }

    partial void OnIsEnergizedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldFlashNoPower));
    }

    public string[] GetPositionOptions()
    {
        return Type switch
        {
            EquipmentType.Valve => ["open", "closed"],
            EquipmentType.Breaker => ["closed", "open", "tripped"],
            EquipmentType.Pump => ["on", "off"],
            EquipmentType.Chiller => ["available", "unavailable"],
            EquipmentType.Generator => ["on", "off", "standby"],
            EquipmentType.ATS => ["normal", "emergency", "test"],
            EquipmentType.UPS => ["on", "off", "bypass"],
            EquipmentType.Motor => ["on", "off"],
            EquipmentType.Transformer => ["energized", "de-energized"],
            EquipmentType.Switch => ["open", "closed"],
            EquipmentType.PDU => ["on", "off"],
            EquipmentType.STS => ["source 1", "source 2", "bypass", "off"],
            _ => ["on", "off"]
        };
    }

    public static string GetDefaultNormalPosition(EquipmentType type)
    {
        return type switch
        {
            EquipmentType.Valve => "open",
            EquipmentType.Breaker => "closed",
            EquipmentType.Pump => "off",
            EquipmentType.Chiller => "available",
            EquipmentType.Generator => "off",
            EquipmentType.Switch => "open",
            EquipmentType.Motor => "off",
            EquipmentType.ATS => "normal",
            EquipmentType.UPS => "on",
            EquipmentType.PDU => "on",
            EquipmentType.Transformer => "energized",
            EquipmentType.STS => "source 1",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Returns true if this equipment type can conduct electricity when in the right state
    /// </summary>
    public bool CanConductElectricity()
    {
        return Type switch
        {
            EquipmentType.Breaker => CurrentPosition.Equals("closed", StringComparison.OrdinalIgnoreCase),
            EquipmentType.Switch => CurrentPosition.Equals("closed", StringComparison.OrdinalIgnoreCase),
            EquipmentType.ATS => CurrentPosition.Equals("normal", StringComparison.OrdinalIgnoreCase) || 
                                 CurrentPosition.Equals("emergency", StringComparison.OrdinalIgnoreCase),
            EquipmentType.UPS => CurrentPosition.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                                 CurrentPosition.Equals("bypass", StringComparison.OrdinalIgnoreCase),
            EquipmentType.PDU => CurrentPosition.Equals("on", StringComparison.OrdinalIgnoreCase),
            EquipmentType.STS => CurrentPosition.Equals("source 1", StringComparison.OrdinalIgnoreCase) ||
                                 CurrentPosition.Equals("source 2", StringComparison.OrdinalIgnoreCase),
            EquipmentType.Transformer => CurrentPosition.Equals("energized", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// Returns true if this equipment is a power source (generator)
    /// </summary>
    public bool IsPowerSource()
    {
        return Type == EquipmentType.Generator && 
               CurrentPosition.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    public static EquipmentType ParseType(string typeString)
    {
        var lower = typeString.ToLowerInvariant();
        
        if (lower.Contains("valve")) return EquipmentType.Valve;
        if (lower.Contains("breaker")) return EquipmentType.Breaker;
        if (lower.Contains("pump")) return EquipmentType.Pump;
        if (lower.Contains("chiller")) return EquipmentType.Chiller;
        if (lower.Contains("generator") || lower.Contains("gen")) return EquipmentType.Generator;
        if (lower.Contains("ats")) return EquipmentType.ATS;
        if (lower.Contains("ups")) return EquipmentType.UPS;
        if (lower.Contains("motor")) return EquipmentType.Motor;
        if (lower.Contains("transformer")) return EquipmentType.Transformer;
        if (lower.Contains("switch")) return EquipmentType.Switch;
        if (lower.Contains("pdu")) return EquipmentType.PDU;
        if (lower.Contains("sts")) return EquipmentType.STS;
        
        return EquipmentType.Valve;
    }
}
