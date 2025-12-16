using CommunityToolkit.Mvvm.ComponentModel;

namespace EquipmentStatusTracker.WPF.Models;

public partial class Layer : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isLocked = false;

    [ObservableProperty]
    private int _order = 0;

    [ObservableProperty]
    private string _color = "#FF58A6FF";

    public static Layer CreateDefault()
    {
        return new Layer
        {
            Id = "default",
            Name = "Default",
            IsVisible = true,
            IsLocked = false,
            Order = 0
        };
    }
}
