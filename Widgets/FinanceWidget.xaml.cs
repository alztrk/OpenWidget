using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;
using OpenWidget.Utils;

namespace OpenWidget.Widgets;

public partial class FinanceWidget : UserControl
{
    private bool _isExpanded = true;

    public FinanceWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => LocalizationService.LanguageChanged -= OnLanguageChanged;
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
        TxtTitle.Text = LocalizationService.Get("Market");
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        AnimationHelper.ToggleAccordion(ContentPanel, ArrowIcon, ref _isExpanded);
    }
}