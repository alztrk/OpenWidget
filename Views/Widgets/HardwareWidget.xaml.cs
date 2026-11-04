using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;
using OpenWidget.Utils;
using OpenWidget.Models;

namespace OpenWidget.Widgets;

public partial class HardwareWidget : UserControl
{
    private bool _isExpanded = true;

    public HardwareWidget()
    {
        InitializeComponent();
        Loaded += (s, e) => LocalizationService.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => LocalizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindow mainWindow)
        {
            _ = mainWindow.EnsureHardwareSpecsLoadedAsync();
        }
        _isExpanded = Tag is WidgetSlot slot ? slot.IsExpanded : true;
        AnimationHelper.SetAccordionState(ContentPanel, ArrowIcon, _isExpanded);
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
        if (Tag is WidgetSlot slot)
        {
            slot.IsExpanded = _isExpanded;
        }
    }
}
