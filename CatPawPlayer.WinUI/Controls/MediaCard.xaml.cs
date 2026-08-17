using CatPawPlayer.WinUI.Models;
using CatPawPlayer.WinUI.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace CatPawPlayer.WinUI.Controls;

public sealed partial class MediaCard : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(VodItem), typeof(MediaCard),
            new PropertyMetadata(null, OnItemChanged));

    public VodItem? Item
    {
        get => (VodItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MediaCard card && e.NewValue is VodItem item)
            card.BindItem(item);
    }

    public MediaCard()
    {
        InitializeComponent();
    }

    private void BindItem(VodItem item)
    {
        TitleText.Text = item.VodName;
        SubText.Text = item.SubText;

        if (!string.IsNullOrEmpty(item.VodPic))
        {
            try
            {
                CoverImage.Source = new BitmapImage(new Uri(item.VodPic)) { DecodePixelWidth = 200 };
            }
            catch
            {
                SetFallbackImage();
            }
        }
        else
        {
            SetFallbackImage();
        }

        if (item.HasBadge)
        {
            BadgeText.Text = item.BadgeText;
            BadgeBorder.Visibility = Visibility.Visible;
        }
        else
        {
            BadgeBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void SetFallbackImage()
    {
        CoverImage.Source = new BitmapImage(new Uri("https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=300&q=80"));
    }

    private void CardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Item == null) return;

        // Support opening config center only if this specific item is the config center item
        if (Item.VodId == "config-center")
        {
            try
            {
                Process.Start(new ProcessStartInfo("http://127.0.0.1:9988/website") { UseShellExecute = true });
            }
            catch { }
            return;
        }

        // Determine destination site: item's specific Site, or site matching SiteKey, or current ActiveSite
        var targetSite = Item.Site ?? App.Sites.FirstOrDefault(s => s.Key == Item.SiteKey) ?? App.ActiveSite;
        if (targetSite == null) return;

        MainWindow.Instance?.NavigateToDetail(Item, targetSite);
    }

    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PlayOverlay.Visibility = Visibility.Visible;
    }

    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        PlayOverlay.Visibility = Visibility.Collapsed;
    }

    private void Card_PointerPressed(object sender, PointerRoutedEventArgs e) { }
}
