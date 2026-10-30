using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenWidget.Models;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class ClipboardWidget : UserControl
{
    private ClipboardService? _clipboardService;

    public ClipboardWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        DataContextChanged += ClipboardWidget_DataContextChanged;
        Unloaded += (s, e) =>
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            if (_clipboardService != null)
            {
                _clipboardService.Items.CollectionChanged -= Items_CollectionChanged;
            }
        };
    }

    private void ClipboardWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is MainWindow mainWin)
        {
            _clipboardService = mainWin.Clipboard;
            if (_clipboardService != null)
            {
                _clipboardService.Items.CollectionChanged += Items_CollectionChanged;
                UpdateListState();
            }
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(UpdateListState);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_clipboardService == null && Application.Current.MainWindow is MainWindow mainWin)
        {
            _clipboardService = mainWin.Clipboard;
            if (_clipboardService != null)
            {
                _clipboardService.Items.CollectionChanged -= Items_CollectionChanged;
                _clipboardService.Items.CollectionChanged += Items_CollectionChanged;
            }
        }
        UpdateTexts();
        UpdateListState();
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateTexts();
            UpdateListState();
        });
    }

    private void UpdateTexts()
    {
        TxtTitle.Text = LocalizationService.Get("ClipboardTitle");
        TxtEmptyState.Text = LocalizationService.Get("ClipboardEmpty");
        BtnClear.ToolTip = LocalizationService.Get("ClipboardClear");
    }

    private void UpdateListState()
    {
        int count = _clipboardService?.Items.Count ?? 0;
        TxtEmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListClipboard.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;

        string unit = count == 1
            ? LocalizationService.Get("ClipboardItem")
            : LocalizationService.Get("ClipboardItems");
        TxtItemCount.Text = $"{count} {unit}";
    }

    private async void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ClipboardItem item && _clipboardService != null)
        {
            await _clipboardService.CopyToClipboardAsync(item);
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _clipboardService?.Clear();
        UpdateListState();
    }
}
