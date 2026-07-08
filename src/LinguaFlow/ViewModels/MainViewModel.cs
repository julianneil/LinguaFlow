namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LinguaFlow.Helpers;
using LinguaFlow.Models;
using LinguaFlow.Services.Ollama;
using LinguaFlow.Services.Translation;

/// <summary>
/// View model for the main document workspace.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly OllamaClient ollamaClient;
    private readonly IReadOnlyDictionary<string, ITranslationService> translationServices;
    private CancellationTokenSource? translationCancellation;
    private string sourceText = string.Empty;
    private string translatedText = string.Empty;
    private string selectedModel = "mistral-nemo:latest";
    private string selectedTranslationEngine = "Built-in";
    private string translationStatus = "Ready";
    private string latencyDisplay = "Latency: --";
    private string tokenUsageDisplay = "Tokens: --";
    private int wordCount;
    private int characterCount;

    /// <summary>
    /// Initializes commands and translation services for the document workspace.
    /// </summary>
    public MainViewModel()
    {
        ollamaClient = new OllamaClient();
        translationServices = new Dictionary<string, ITranslationService>
        {
            ["Built-in"] = new BuiltInTranslationService(),
            ["Ollama"] = new OllamaTranslationService(ollamaClient)
        };

        TranslationEngines = new ObservableCollection<string>(translationServices.Keys);
        AvailableModels = new ObservableCollection<string>
        {
            selectedModel
        };

        NewDocumentCommand = new RelayCommand(_ => NewDocument());
        OpenDocumentCommand = CreatePlaceholderCommand("Open document is not connected yet.");
        SaveDocumentCommand = CreatePlaceholderCommand("Save document is not connected yet.");
        ExportDocxCommand = CreatePlaceholderCommand("DOCX export is not connected yet.");
        ExportPdfCommand = CreatePlaceholderCommand("PDF export is not connected yet.");
        CopyTranslationCommand = new RelayCommand(_ => CopyTranslation(), _ => !string.IsNullOrWhiteSpace(TranslatedText));
        OpenSettingsCommand = CreatePlaceholderCommand("Settings are not connected yet.");
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, CanTranslate);

        _ = LoadOllamaModelsAsync();
    }

    /// <summary>
    /// Translation engines available to the workspace.
    /// </summary>
    public ObservableCollection<string> TranslationEngines { get; }

    /// <summary>
    /// Models available to the selector.
    /// </summary>
    public ObservableCollection<string> AvailableModels { get; }

    /// <summary>
    /// English document text currently in the editor.
    /// </summary>
    public string SourceText
    {
        get => sourceText;
        set
        {
            if (SetProperty(ref sourceText, value))
            {
                UpdateDocumentCounts();
                RaiseTranslationCommandState();
            }
        }
    }

    /// <summary>
    /// Translated document text shown in the preview pane.
    /// </summary>
    public string TranslatedText
    {
        get => translatedText;
        set
        {
            if (SetProperty(ref translatedText, value))
            {
                RaiseCopyCommandState();
            }
        }
    }

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
                TranslationStatus = value == "Ollama" ? "Ollama translation selected." : "Built-in translation selected.";
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

    /// <summary>
    /// Translates the current English editor text.
    /// </summary>
    public ICommand TranslateCommand { get; }

    private async Task LoadOllamaModelsAsync()
    {
        try
        {
            var models = await ollamaClient.GetModelsAsync(CancellationToken.None);
            if (models.Count == 0)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableModels.Clear();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }

                if (AvailableModels.Contains("mistral-nemo:latest"))
                {
                    SelectedModel = "mistral-nemo:latest";
                }
                else
                {
                    SelectedModel = AvailableModels[0];
                }

                TranslationStatus = "Ollama models detected.";
            });
        }
        catch
        {
            TranslationStatus = "Ollama is not available. Built-in translation can still be used.";
        }
    }

    private async Task TranslateAsync()
    {
        translationCancellation?.Cancel();
        translationCancellation = new CancellationTokenSource();

        try
        {
            TranslationStatus = "Translating...";
            var service = translationServices[SelectedTranslationEngine];
            var request = new TranslationRequest(SourceText, "Spanish", "Natural", SelectedModel);
            var result = await service.TranslateAsync(request, translationCancellation.Token);

            TranslatedText = result.Text;
            LatencyDisplay = $"Latency: {result.Latency.TotalMilliseconds:0} ms";
            TokenUsageDisplay = result.TokenUsage is null ? "Tokens: --" : $"Tokens: {result.TokenUsage}";
            TranslationStatus = "Translation complete.";
        }
        catch (OperationCanceledException)
        {
            TranslationStatus = "Translation canceled.";
        }
        catch (Exception exception)
        {
            TranslationStatus = $"Translation failed: {exception.Message}";
        }
    }

    private bool CanTranslate()
    {
        return !string.IsNullOrWhiteSpace(SourceText);
    }

    private void NewDocument()
    {
        SourceText = string.Empty;
        TranslatedText = string.Empty;
        LatencyDisplay = "Latency: --";
        TokenUsageDisplay = "Tokens: --";
        TranslationStatus = "New document ready.";
    }

    private void CopyTranslation()
    {
        if (!string.IsNullOrWhiteSpace(TranslatedText))
        {
            Clipboard.SetText(TranslatedText);
            TranslationStatus = "Translation copied.";
        }
    }

    private void UpdateDocumentCounts()
    {
        CharacterCount = SourceText.Length;
        WordCount = SourceText.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private RelayCommand CreatePlaceholderCommand(string statusMessage)
    {
        // Phase 2 keeps document command surfaces visible while file services are added later.
        return new RelayCommand(_ => TranslationStatus = statusMessage);
    }

    private void RaiseTranslationCommandState()
    {
        if (TranslateCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private void RaiseCopyCommandState()
    {
        if (CopyTranslationCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
