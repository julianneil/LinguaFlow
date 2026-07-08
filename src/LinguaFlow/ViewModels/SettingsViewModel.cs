namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using LinguaFlow.Helpers;
using LinguaFlow.Models;

/// <summary>
/// View model for editing application settings.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private string selectedTranslationMode;
    private string selectedTranslationEngine;
    private string selectedModel;
    private string ollamaEndpoint;
    private bool notifyWhenOllamaUnavailable;
    private int debounceDelayMilliseconds;
    private string selectedTranslationStyle;
    private double fontSize;

    /// <summary>
    /// Creates a settings editor from existing application settings.
    /// </summary>
    /// <param name="settings">Current application settings.</param>
    /// <param name="availableModels">Known Ollama models.</param>
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
        debounceDelayMilliseconds = settings.DebounceDelayMilliseconds;
        selectedTranslationStyle = settings.TranslationStyle;
        fontSize = settings.FontSize;

        SaveCommand = new RelayCommand(_ => RequestClose?.Invoke(true));
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    /// <summary>
    /// Raised when the settings window should close.
    /// </summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// Available translation timing modes.
    /// </summary>
    public ObservableCollection<string> TranslationModes { get; }

    /// <summary>
    /// Available translation engines.
    /// </summary>
    public ObservableCollection<string> TranslationEngines { get; }

    /// <summary>
    /// Known Ollama model names.
    /// </summary>
    public ObservableCollection<string> AvailableModels { get; }

    /// <summary>
    /// Available prompt styles.
    /// </summary>
    public ObservableCollection<string> TranslationStyles { get; }

    /// <summary>
    /// Selected translation timing mode.
    /// </summary>
    public string SelectedTranslationMode
    {
        get => selectedTranslationMode;
        set => SetProperty(ref selectedTranslationMode, value);
    }

    /// <summary>
    /// Selected translation engine.
    /// </summary>
    public string SelectedTranslationEngine
    {
        get => selectedTranslationEngine;
        set => SetProperty(ref selectedTranslationEngine, value);
    }

    /// <summary>
    /// Selected Ollama model.
    /// </summary>
    public string SelectedModel
    {
        get => selectedModel;
        set => SetProperty(ref selectedModel, value);
    }

    /// <summary>
    /// Configured Ollama endpoint.
    /// </summary>
    public string OllamaEndpoint
    {
        get => ollamaEndpoint;
        set => SetProperty(ref ollamaEndpoint, value);
    }

    /// <summary>
    /// Indicates whether the app should remind the user when Ollama is unavailable.
    /// </summary>
    public bool NotifyWhenOllamaUnavailable
    {
        get => notifyWhenOllamaUnavailable;
        set => SetProperty(ref notifyWhenOllamaUnavailable, value);
    }

    /// <summary>
    /// Delay after typing stops before live translation starts.
    /// </summary>
    public int DebounceDelayMilliseconds
    {
        get => debounceDelayMilliseconds;
        set => SetProperty(ref debounceDelayMilliseconds, Math.Clamp(value, 250, 5000));
    }

    /// <summary>
    /// Selected translation style.
    /// </summary>
    public string SelectedTranslationStyle
    {
        get => selectedTranslationStyle;
        set => SetProperty(ref selectedTranslationStyle, value);
    }

    /// <summary>
    /// Editor font size.
    /// </summary>
    public double FontSize
    {
        get => fontSize;
        set => SetProperty(ref fontSize, Math.Clamp(value, 10, 28));
    }

    /// <summary>
    /// Saves settings and closes the dialog.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Closes the dialog without saving.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Creates a settings object from the current editor state.
    /// </summary>
    /// <returns>Updated settings.</returns>
    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            TranslationMode = SelectedTranslationMode,
            TranslationEngine = SelectedTranslationEngine,
            OllamaModel = SelectedModel,
            OllamaEndpoint = OllamaEndpoint,
            NotifyWhenOllamaUnavailable = NotifyWhenOllamaUnavailable,
            DebounceDelayMilliseconds = DebounceDelayMilliseconds,
            TranslationStyle = SelectedTranslationStyle,
            FontSize = FontSize
        };
    }
}
