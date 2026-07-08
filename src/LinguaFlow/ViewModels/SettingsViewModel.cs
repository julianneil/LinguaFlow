namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using LinguaFlow.Helpers;
using LinguaFlow.Models;

public sealed class SettingsViewModel : ObservableObject
{
    private string selectedTranslationMode;
    private string selectedTranslationEngine;
    private string selectedModel;
    private string ollamaEndpoint;
    private bool notifyWhenOllamaUnavailable;
    private string debounceDelayMillisecondsText;
    private string selectedTranslationStyle;
    private string fontSizeText;

    public SettingsViewModel(AppSettings settings, IEnumerable<string> availableModels)
    {
        TranslationModes = new ObservableCollection<string>(["Live", "Manual"]);
        TranslationEngines = new ObservableCollection<string>(["Built-in", "Ollama"]);
        TranslationStyles = new ObservableCollection<string>(["Natural", "Professional", "Business", "Legal", "Medical", "Academic", "Casual", "Literal"]);
        AvailableModels = new ObservableCollection<string>(availableModels.DefaultIfEmpty(settings.OllamaModel));

        selectedTranslationMode = settings.TranslationMode;
        selectedTranslationEngine = settings.TranslationEngine;
        selectedModel = settings.OllamaModel;
        ollamaEndpoint = settings.OllamaEndpoint;
        notifyWhenOllamaUnavailable = settings.NotifyWhenOllamaUnavailable;
        debounceDelayMillisecondsText = settings.DebounceDelayMilliseconds.ToString(CultureInfo.InvariantCulture);
        selectedTranslationStyle = settings.TranslationStyle;
        fontSizeText = settings.FontSize.ToString(CultureInfo.InvariantCulture);

        SaveCommand = new RelayCommand(_ => RequestClose?.Invoke(true));
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public event Action<bool>? RequestClose;

    public ObservableCollection<string> TranslationModes { get; }

    public ObservableCollection<string> TranslationEngines { get; }

    public ObservableCollection<string> AvailableModels { get; }

    public ObservableCollection<string> TranslationStyles { get; }

    public string SelectedTranslationMode
    {
        get => selectedTranslationMode;
        set => SetProperty(ref selectedTranslationMode, value);
    }

    public string SelectedTranslationEngine
    {
        get => selectedTranslationEngine;
        set => SetProperty(ref selectedTranslationEngine, value);
    }

    public string SelectedModel
    {
        get => selectedModel;
        set => SetProperty(ref selectedModel, value);
    }

    public string OllamaEndpoint
    {
        get => ollamaEndpoint;
        set => SetProperty(ref ollamaEndpoint, value);
    }

    public bool NotifyWhenOllamaUnavailable
    {
        get => notifyWhenOllamaUnavailable;
        set => SetProperty(ref notifyWhenOllamaUnavailable, value);
    }

    public string DebounceDelayMillisecondsText
    {
        get => debounceDelayMillisecondsText;
        set => SetProperty(ref debounceDelayMillisecondsText, value);
    }

    public string SelectedTranslationStyle
    {
        get => selectedTranslationStyle;
        set => SetProperty(ref selectedTranslationStyle, value);
    }

    public string FontSizeText
    {
        get => fontSizeText;
        set => SetProperty(ref fontSizeText, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            TranslationMode = SelectedTranslationMode,
            TranslationEngine = SelectedTranslationEngine,
            OllamaModel = SelectedModel,
            OllamaEndpoint = OllamaEndpoint,
            NotifyWhenOllamaUnavailable = NotifyWhenOllamaUnavailable,
            DebounceDelayMilliseconds = ParseDelay(DebounceDelayMillisecondsText),
            TranslationStyle = SelectedTranslationStyle,
            FontSize = ParseFontSize(FontSizeText)
        };
    }

    private static int ParseDelay(string value)
    {
        // Keep these as text fields in the dialog. WPF numeric bindings can throw while the
        // user is halfway through editing, which is how a simple delay change used to close the app.
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delay)
            ? Math.Clamp(delay, 250, 5000)
            : 700;
    }

    private static double ParseFontSize(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize)
            ? Math.Clamp(fontSize, 10, 28)
            : 15;
    }
}
