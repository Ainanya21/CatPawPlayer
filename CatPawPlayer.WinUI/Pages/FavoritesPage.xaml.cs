using CatPawPlayer.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class FavoritesPage : Page
{
    public ObservableCollection<VodItem> FavoritesItems { get; set; } = [];

    public FavoritesPage()
    {
        InitializeComponent();
        Loaded += FavoritesPage_Loaded;
    }

    private void FavoritesPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadFavorites();
        UpdateGridItemSize(FavoritesGridView);
    }

    private void LoadFavorites()
    {
        FavoritesItems.Clear();
        if (App.Favorites != null)
        {
            foreach (var item in App.Favorites)
            {
                FavoritesItems.Add(item);
            }
        }
        FavoritesGridView.ItemsSource = FavoritesItems;
        CountText.Text = $"共 {FavoritesItems.Count} 部";
        UpdateEmptyState();
        UpdateGridItemSize(FavoritesGridView);
    }

    private void FavoritesGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is VodItem item && App.ActiveSite != null)
        {
            Frame.Navigate(typeof(DetailPage), (item, App.ActiveSite));
        }
    }

    private void FavoritesGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGridItemSize(FavoritesGridView);
    }

    private static void UpdateGridItemSize(GridView gridView)
    {
        if (gridView.ItemsPanelRoot is ItemsWrapGrid wrapGrid && gridView.ActualWidth > 100)
        {
            double totalWidth = gridView.ActualWidth;
            int columns = Math.Max(2, (int)Math.Floor(totalWidth / 170.0));
            double itemWidth = Math.Floor(totalWidth / columns);
            double itemHeight = Math.Floor(itemWidth * 1.5);
            wrapGrid.ItemWidth = itemWidth;
            wrapGrid.ItemHeight = itemHeight;
        }
    }

    private void UpdateEmptyState()
    {
        if (FavoritesItems.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            FavoritesGridView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            FavoritesGridView.Visibility = Visibility.Visible;
        }
    }
}
