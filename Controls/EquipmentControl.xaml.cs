using System.Windows;
using System.Windows.Controls;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Controls;

public partial class EquipmentControl : UserControl
{
    public static readonly DependencyProperty EquipmentProperty =
        DependencyProperty.Register(nameof(Equipment), typeof(Equipment), typeof(EquipmentControl),
            new PropertyMetadata(null, OnEquipmentChanged));

    public Equipment? Equipment
    {
        get => (Equipment?)GetValue(EquipmentProperty);
        set => SetValue(EquipmentProperty, value);
    }

    public EquipmentControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private static void OnEquipmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EquipmentControl control)
        {
            control.DataContext = control;
        }
    }
}

