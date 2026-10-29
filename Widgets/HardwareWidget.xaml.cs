using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;
using OpenWidget.Utils;

namespace OpenWidget.Widgets;

public partial class HardwareWidget : UserControl
{
    private bool _isExpanded = true;

    public HardwareWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTexts();
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(UpdateTexts);
    }

    private void UpdateTexts()
    {
        TxtHardwareTitle.Text = LocalizationService.Get("Hardware");
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        AnimationHelper.ToggleAccordion(ContentPanel, ArrowIcon, ref _isExpanded);
    }
}