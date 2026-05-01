using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.ViewModels;

public partial class WhitelistViewModel : ObservableObject
{
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private string _newWord = "";

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<WhitelistItem> Words { get; } = [];

    public WhitelistViewModel(WordList wordlist, LocalizationService localization)
    {
        _wordlist = wordlist;
        _localization = localization;
        RefreshList();
    }

    [RelayCommand]
    private void AddWord()
    {
        var word = NewWord.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(word)) return;

        if (_wordlist.AddToWhitelist(word))
        {
            NewWord = "";
            RefreshList();
        }
    }

    [RelayCommand]
    private void RemoveWord(string? word)
    {
        if (word is null) return;
        _wordlist.RemoveFromWhitelist(word);
        RefreshList();
    }

    [RelayCommand]
    private void ClearAll()
    {
        if (!Words.Any()) return;
        _wordlist.ClearWhitelist();
        RefreshList();
    }

    public void ImportFromFile(string path)
    {
        try
        {
            var count = 0;
            foreach (var line in File.ReadLines(path))
            {
                var word = line.Trim().ToLowerInvariant();
                if (word.Length > 0 && !word.StartsWith('#') && _wordlist.AddToWhitelist(word))
                    count++;
            }
            RefreshList();
        }
        catch { }
    }

    public string ExportToFile(string path)
    {
        var words = Words.Select(w => w.Text).OrderBy(w => w).ToList();
        File.WriteAllLines(path, words);
        return string.Format(_localization["whitelist.export_done"], words.Count);
    }

    private void RefreshList()
    {
        Words.Clear();
        foreach (var word in _wordlist.Whitelist.OrderBy(w => w))
        {
            Words.Add(new WhitelistItem { Text = word });
        }
        IsEmpty = Words.Count == 0;
    }
}

public class WhitelistItem
{
    public string Text { get; set; } = "";
}
