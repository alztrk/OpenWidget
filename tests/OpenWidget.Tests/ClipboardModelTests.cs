using System;
using OpenWidget.Models;
using Xunit;

namespace OpenWidget.Tests;

public class ClipboardModelTests
{
    [Fact]
    public void ClipboardItem_Defaults_AreValid()
    {
        var item = new ClipboardItem();

        Assert.False(string.IsNullOrWhiteSpace(item.Id));
        Assert.Equal("Text", item.ItemType);
        Assert.Equal("GeoCopy", item.IconKey);
        Assert.False(item.IsImage);
        Assert.False(item.IsJustCopied);
    }

    [Theory]
    [InlineData("Text", "GeoCopy")]
    [InlineData("Image", "GeoMonitor")]
    [InlineData("File", "GeoStorage")]
    [InlineData("Unknown", "GeoCopy")]
    public void ClipboardItem_IconKey_MapsCorrectly(string itemType, string expectedIcon)
    {
        var item = new ClipboardItem { ItemType = itemType };
        Assert.Equal(expectedIcon, item.IconKey);
    }

    [Fact]
    public void ClipboardItem_IsImage_ReturnsTrueOnlyWhenImageAndHasBase64()
    {
        var item1 = new ClipboardItem { ItemType = "Image", ImageBase64 = null };
        var item2 = new ClipboardItem { ItemType = "Image", ImageBase64 = "base64data" };
        var item3 = new ClipboardItem { ItemType = "Text", ImageBase64 = "base64data" };

        Assert.False(item1.IsImage);
        Assert.True(item2.IsImage);
        Assert.False(item3.IsImage);
    }

    [Fact]
    public void ClipboardItem_IsJustCopied_FiresPropertyChanged()
    {
        var item = new ClipboardItem();
        bool fired = false;
        item.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ClipboardItem.IsJustCopied))
            {
                fired = true;
            }
        };

        item.IsJustCopied = true;
        Assert.True(fired);
        Assert.True(item.IsJustCopied);
    }

    [Fact]
    public void ClipboardWidget_ContainsScrollViewerWithExpectedProperties()
    {
        RunInSta(() =>
        {
            var widget = new Widgets.ClipboardWidget();
            var scrollViewer = widget.FindName("ClipboardScrollViewer") as System.Windows.Controls.ScrollViewer;

            Assert.NotNull(scrollViewer);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);
        });
    }

    [Fact]
    public void ClipboardWidget_LayoutAndCardStructure_ValidatesSuccessfully()
    {
        RunInSta(() =>
        {
            var widget = new Widgets.ClipboardWidget();
            var grid = widget.Content as System.Windows.Controls.Grid;

            Assert.NotNull(grid);
            Assert.Equal(2, grid.RowDefinitions.Count);
            Assert.True(grid.RowDefinitions[0].Height.IsAuto);
            Assert.True(grid.RowDefinitions[1].Height.IsStar);

            var scrollViewer = widget.FindName("ClipboardScrollViewer") as System.Windows.Controls.ScrollViewer;
            Assert.NotNull(scrollViewer);
            Assert.False(scrollViewer.Focusable);
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                EnsureApplicationResources();
                action();
            }
            catch (Exception ex) { exception = ex; }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private static void EnsureApplicationResources()
    {
        if (System.Windows.Application.Current == null)
        {
            var app = new System.Windows.Application();
            app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/OpenWidget;component/Resources/Styles/Theme.xaml", UriKind.Absolute)
            });
            app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/OpenWidget;component/Resources/Styles/Components.xaml", UriKind.Absolute)
            });
        }
    }
}
