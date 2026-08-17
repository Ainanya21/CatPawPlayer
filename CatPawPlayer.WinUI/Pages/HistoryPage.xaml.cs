using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CatPawPlayer.WinUI.Models;
using System.Collections.ObjectModel;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class HistoryPage : Page
{
    public ObservableCollection<HistoryItem> HistoryItems { get; set; } = [];

    public HistoryPage()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryItems.Clear();
        foreach (var item in App.History)
        {
            HistoryItems.Add(item);
        }
        HistoryListView.ItemsSource = HistoryItems;
        UpdateEmptyState();
    }

    private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryItem item)
        {
            var site = App.Sites.FirstOrDefault(s => s.Key == item.SiteKey) ?? App.ActiveSite;
            if (site != null)
            {
                var vod = new VodItem
                {
                    VodId = item.VodId,
                    VodName = item.VodName,
                    VodPic = item.VodPic,
                };
                Frame.Navigate(typeof(DetailPage), (vod, site));
            }
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        HistoryItems.Clear();
        App.History.Clear();
        App.Settings.SaveHistory(App.History);
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        if (HistoryItems.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            HistoryListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            HistoryListView.Visibility = Visibility.Visible;
        }
    }
}
