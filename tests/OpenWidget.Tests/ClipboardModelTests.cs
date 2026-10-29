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
}
