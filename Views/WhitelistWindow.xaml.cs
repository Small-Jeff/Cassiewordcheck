using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class WhitelistWindow : Window
{
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;

    public WhitelistWindow(WordList wordlist, LocalizationService localization)
    {
        InitializeComponent();
        _wordlist = wordlist;
        _localization = localization;

        this.EnableDarkTitleBar();
        RefreshList();
    }

    private void OnNewWordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            AddWord();
    }

    private void OnAddWord(object sender, RoutedEventArgs e)
    {
        AddWord();
    }

    private void AddWord()
    {
        var word = NewWordBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(word)) return;

        if (_wordlist.AddToWhitelist(word))
        {
            NewWordBox.Clear();
            RefreshList();
        }
    }

    private void OnRemoveWord(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string word)
        {
            _wordlist.RemoveFromWhitelist(word);
            RefreshList();
        }
    }

    private void OnImport(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Whitelist",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var count = 0;
                foreach (var line in File.ReadLines(dialog.FileName))
                {
                    var word = line.Trim().ToLowerInvariant();
                    if (word.Length > 0 && !word.StartsWith('#') && _wordlist.AddToWhitelist(word))
                        count++;
                }
                RefreshList();
            }
            catch { }
        }
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        var words = _wordlist.Whitelist.OrderBy(w => w).ToList();
        if (words.Count == 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Whitelist",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "whitelist.txt",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllLines(dialog.FileName, words);
            }
            catch { }
        }
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        if (_wordlist.Whitelist.Count == 0) return;

        var result = MessageBox.Show(
            _localization["whitelist.confirm_clear"],
            _localization["whitelist.title"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _wordlist.ClearWhitelist();
            RefreshList();
        }
    }

    private void RefreshList()
    {
        var items = _wordlist.Whitelist
            .OrderBy(w => w)
            .Select(w => new { Text = w })
            .ToList();

        WhitelistItems.ItemsSource = items;
        EmptyLabel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
