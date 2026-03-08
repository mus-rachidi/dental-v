using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClinicManager.Models;

namespace ClinicManager.Views.Controls;

public partial class ToothShapeControl : UserControl
{
    public static readonly DependencyProperty ToothTypeProperty =
        DependencyProperty.Register(nameof(ToothType), typeof(ToothType), typeof(ToothShapeControl),
            new PropertyMetadata(ToothType.Molar, OnTypeChanged));

    public static readonly DependencyProperty ConditionColorProperty =
        DependencyProperty.Register(nameof(ConditionColor), typeof(Brush), typeof(ToothShapeControl),
            new PropertyMetadata(Brushes.LightGray, OnColorChanged));

    public ToothType ToothType
    {
        get => (ToothType)GetValue(ToothTypeProperty);
        set => SetValue(ToothTypeProperty, value);
    }

    public Brush ConditionColor
    {
        get => (Brush)GetValue(ConditionColorProperty);
        set => SetValue(ConditionColorProperty, value);
    }

    public ToothShapeControl()
    {
        InitializeComponent();
        UpdateShape();
        UpdateColors();
    }

    private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothShapeControl c)
            c.UpdateShape();
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothShapeControl c)
            c.UpdateColors();
    }

    private void UpdateShape()
    {
        IncisorPath.Visibility = Visibility.Collapsed;
        CaninePath.Visibility = Visibility.Collapsed;
        PremolarPath.Visibility = Visibility.Collapsed;
        MolarPath.Visibility = Visibility.Collapsed;
        WisdomPath.Visibility = Visibility.Collapsed;

        var path = ToothType switch
        {
            ToothType.Incisor => IncisorPath,
            ToothType.Canine => CaninePath,
            ToothType.Premolar => PremolarPath,
            ToothType.Molar => MolarPath,
            ToothType.WisdomTooth => WisdomPath,
            _ => MolarPath
        };
        path.Visibility = Visibility.Visible;
    }

    private void UpdateColors()
    {
        var fill = ConditionColor.Clone();
        fill.Opacity = 0.5;
        var stroke = ConditionColor;

        IncisorPath.Fill = fill;
        IncisorPath.Stroke = stroke;
        CaninePath.Fill = fill;
        CaninePath.Stroke = stroke;
        PremolarPath.Fill = fill;
        PremolarPath.Stroke = stroke;
        MolarPath.Fill = fill;
        MolarPath.Stroke = stroke;
        WisdomPath.Fill = fill;
        WisdomPath.Stroke = stroke;
    }
}
