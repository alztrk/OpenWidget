using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenWidget.Models;
using OpenWidget.Services;
using OpenWidget.Utils;

namespace OpenWidget.Widgets;

public partial class FinanceWidget : UserControl
{
    private bool _isExpanded = true;
    private MainWindow? _host;
    private readonly ObservableCollection<FinanceDisplayRow> _rows = new();
    private readonly ObservableCollection<FinanceUnitOption> _unitOptions = new();
    private bool _suppressUnitChanged;
    private string _selectedUnitKey = "TRY";

    public FinanceWidget()
    {
        InitializeComponent();
        FinanceItems.ItemsSource = _rows;
        CmbUnit.ItemsSource = _unitOptions;
        Loaded += (s, e) => LocalizationService.LanguageChanged += OnLanguageChanged;
        Unloaded += UserControl_Unloaded;
        DataObject.AddPastingHandler(TxtAmount, OnAmountPaste);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _isExpanded = Tag is WidgetSlot slot ? slot.IsExpanded : true;
        AnimationHelper.SetAccordionState(ContentPanel, ArrowIcon, _isExpanded);
        UpdateTexts();
        AttachHost(DataContext as MainWindow);
        RefreshUnitOptions();
        RefreshRows();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        DetachHost();
        DataObject.RemovePastingHandler(TxtAmount, OnAmountPaste);
    }

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachHost(e.NewValue as MainWindow);
        RefreshUnitOptions();
        RefreshRows();
    }

    private void AttachHost(MainWindow? host)
    {
        if (ReferenceEquals(_host, host))
        {
            return;
        }

        DetachHost();
        _host = host;
        if (_host != null)
        {
            _host.PropertyChanged += OnHostPropertyChanged;
        }
    }

    private void DetachHost()
    {
        if (_host != null)
        {
            _host.PropertyChanged -= OnHostPropertyChanged;
            _host = null;
        }
    }

    private void OnHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null
            or nameof(MainWindow.Finance)
            or nameof(MainWindow.MarketCurrency))
        {
            Dispatcher.Invoke(() =>
            {
                RefreshUnitOptions();
                RefreshRows();
            });
        }
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateTexts();
            RefreshRows();
        });
    }

    private void UpdateTexts()
    {
        TxtTitle.Text = LocalizationService.Get("Market");
        TxtConvertLabel.Text = LocalizationService.Get("FinanceConvertLabel");
        TxtAmount.ToolTip = LocalizationService.Get("FinanceConvertPlaceholder");
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        AnimationHelper.ToggleAccordion(ContentPanel, ArrowIcon, ref _isExpanded);
        if (Tag is WidgetSlot slot)
        {
            slot.IsExpanded = _isExpanded;
        }
    }

    private void TxtAmount_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _host?.BeginInteractiveInput();
        if (!TxtAmount.IsKeyboardFocusWithin)
        {
            TxtAmount.Focus();
            e.Handled = true;
        }
    }

    private void CmbUnit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _host?.BeginInteractiveInput();
    }

    private void InteractiveInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _host?.BeginInteractiveInput();
    }

    private void InteractiveInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _host?.EndInteractiveInput();
    }

    private void CmbUnit_DropDownOpened(object sender, EventArgs e)
    {
        _host?.BeginInteractiveInput();
    }

    private void CmbUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressUnitChanged)
        {
            return;
        }

        if (CmbUnit.SelectedValue is string key && !string.IsNullOrWhiteSpace(key))
        {
            _selectedUnitKey = key;
        }

        RefreshRows();
    }

    private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var next = TxtAmount.Text
            .Remove(TxtAmount.SelectionStart, TxtAmount.SelectionLength)
            .Insert(TxtAmount.SelectionStart, e.Text);
        e.Handled = !FinanceAmountConverter.IsValidAmountText(next);
    }

    private void TxtAmount_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void OnAmountPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
        {
            e.CancelCommand();
            return;
        }

        var paste = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        var next = TxtAmount.Text
            .Remove(TxtAmount.SelectionStart, TxtAmount.SelectionLength)
            .Insert(TxtAmount.SelectionStart, paste);
        if (!FinanceAmountConverter.IsValidAmountText(next))
        {
            e.CancelCommand();
        }
    }

    private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshRows();
    }

    private void RefreshUnitOptions()
    {
        var currency = _host?.MarketCurrency ?? "TRY";
        var items = _host?.Finance.VisibleItems ?? [];
        var previous = _selectedUnitKey;

        _suppressUnitChanged = true;
        _unitOptions.Clear();
        _unitOptions.Add(new FinanceUnitOption(currency, currency.ToUpperInvariant()));

        foreach (var item in items)
        {
            if (string.Equals(item.Key, currency, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _unitOptions.Add(new FinanceUnitOption(item.Key, FinanceAmountConverter.GetUnitOptionCode(item.Key)));
        }

        var match = _unitOptions.FirstOrDefault(option =>
            string.Equals(option.Key, previous, StringComparison.OrdinalIgnoreCase));
        _selectedUnitKey = match?.Key ?? currency;
        CmbUnit.SelectedValue = _selectedUnitKey;
        _suppressUnitChanged = false;
    }

    private void RefreshRows()
    {
        var items = _host?.Finance.VisibleItems ?? [];
        var currency = _host?.MarketCurrency ?? "TRY";
        var mode = FinanceConvertMode.None;
        decimal? amount = null;
        decimal? sourceUnitPrice = null;

        if (FinanceAmountConverter.TryParseAmount(TxtAmount.Text, out var parsed))
        {
            mode = FinanceConvertMode.Spend;
            amount = parsed;
            if (!FinanceAmountConverter.TryResolveSourceUnitPrice(
                    _selectedUnitKey,
                    currency,
                    key => items.FirstOrDefault(item =>
                        string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.UnitPrice,
                    out var resolved))
            {
                sourceUnitPrice = null;
            }
            else
            {
                sourceUnitPrice = resolved;
            }
        }

        _rows.Clear();
        foreach (var item in items)
        {
            var isInverse = FinanceAmountConverter.IsInverseSourceRow(
                item.Key, mode, amount, currency, _selectedUnitKey);
            var label = isInverse
                ? FinanceAmountConverter.GetMarketCurrencyLabel(currency)
                : item.Label;

            _rows.Add(new FinanceDisplayRow(
                item.IconKey,
                label,
                FinanceAmountConverter.FormatDisplayPrice(
                    item,
                    mode,
                    amount,
                    currency,
                    _selectedUnitKey,
                    sourceUnitPrice),
                item.ChangeFormatted,
                item.ChangeDirection,
                isInverse));
        }
    }

    private sealed record FinanceUnitOption(string Key, string Code);

    private sealed record FinanceDisplayRow(
        string IconKey,
        string Label,
        string DisplayPrice,
        string ChangeFormatted,
        string ChangeDirection,
        bool IsInverseRow);
}
