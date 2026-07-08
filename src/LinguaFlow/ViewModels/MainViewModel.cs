namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using LinguaFlow.Helpers;

/// <summary>
/// View model for the main document workspace.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private string selectedModel = "mistral-nemo:latest";
    private string selectedTranslationEngine = "Built-in";
    private string translationStatus = "Ready";
    private string latencyDisplay = "Latency: --";
    private string tokenUsageDisplay = "Tokens: --";
    private int wordCount;
    private int characterCount;

    /// <summary>
    /// Initializes commands and shell defaults for the first application phase.
    /// </summary>
    public MainViewModel()
    {
        TranslationEngines = new ObservableCollection<string>
        {
            "Built-in",
            "Ollama"
        };

        AvailableModels = new ObservableCollection<string>
        {
            "mistral-nemo:latest"
        };

        NewDocumentCommand = CreatePlaceholderCommand("New document is not connected yet.");
        OpenDocumentCommand = CreatePlaceholderCommand("Open document is not connected yet.");
        SaveDocumentCommand = CreatePlaceholderCommand("Save document is not connected yet.");
        ExportDocxCommand = CreatePlaceholderCommand("DOCX export is not connected yet.");
        ExportPdfCommand = CreatePlaceholderCommand("PDF export is not connected yet.");
        CopyTranslationCommand = CreatePlaceholderCommand("Copy translation is not connected yet.");
        OpenSettingsCommand = CreatePlaceholderCommand("Settings are not connected yet.");
    }

    /// <summary>
    /// Translation engines available to the workspace.
    /// </summary>
    public ObservableCollection<string> TranslationEngines { get; }

    /// <summary>
    /// Models available to the selector. Ollama discovery will populate this in Phase 2.
    /// </summary>
    public ObservableCollection<string> AvailableModels { get; }

    /// <summary>
    /// Selected translation engine for document updates.
    /// </summary>
    public string SelectedTranslationEngine
    {
        get => selectedTranslationEngine;
        set
        {
            if (SetProperty(ref selectedTranslationEngine, value))
            {
                OnPropertyChanged(nameof(IsOllamaSelected));
            }
        }
    }

    /// <summary>
    /// Indicates whether the Ollama-specific controls should be active.
    /// </summary>
    public bool IsOllamaSelected => SelectedTranslationEngine == "Ollama";

    /// <summary>
    /// Selected Ollama model for translation requests.
    /// </summary>
    public string SelectedModel
    {
        get => selectedModel;
        set => SetProperty(ref selectedModel, value);
    }

    /// <summary>
    /// Current translation state shown in the status bar.
    /// </summary>
    public string TranslationStatus
    {
        get => translationStatus;
        set => SetProperty(ref translationStatus, value);
    }

    /// <summary>
    /// Last translation latency shown in the status bar.
    /// </summary>
    public string LatencyDisplay
    {
        get => latencyDisplay;
        set => SetProperty(ref latencyDisplay, value);
    }

    /// <summary>
    /// Word count for the active English document.
    /// </summary>
    public int WordCount
    {
        get => wordCount;
        set => SetProperty(ref wordCount, value);
    }

    /// <summary>
    /// Character count for the active English document.
    /// </summary>
    public int CharacterCount
    {
        get => characterCount;
        set => SetProperty(ref characterCount, value);
    }

    /// <summary>
    /// Token usage for the last translation request.
    /// </summary>
    public string TokenUsageDisplay
    {
        get => tokenUsageDisplay;
        set => SetProperty(ref tokenUsageDisplay, value);
    }

    /// <summary>
    /// Starts a new document.
    /// </summary>
    public ICommand NewDocumentCommand { get; }

    /// <summary>
    /// Opens an existing document.
    /// </summary>
    public ICommand OpenDocumentCommand { get; }

    /// <summary>
    /// Saves the active document.
    /// </summary>
    public ICommand SaveDocumentCommand { get; }

    /// <summary>
    /// Exports the active document as DOCX.
    /// </summary>
    public ICommand ExportDocxCommand { get; }

    /// <summary>
    /// Exports the active document as PDF.
    /// </summary>
    public ICommand ExportPdfCommand { get; }

    /// <summary>
    /// Copies the translated document to the clipboard.
    /// </summary>
    public ICommand CopyTranslationCommand { get; }

    /// <summary>
    /// Opens the application settings.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    private RelayCommand CreatePlaceholderCommand(string statusMessage)
    {
        // Phase 1 keeps command surfaces visible while later phases add document and translation services.
        return new RelayCommand(_ => TranslationStatus = statusMessage);
    }
}
