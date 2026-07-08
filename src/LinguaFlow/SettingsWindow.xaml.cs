namespace LinguaFlow;

using System.Windows;
using LinguaFlow.ViewModels;

/// <summary>
/// Interaction surface for editing persisted application settings.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes the settings window.
    /// </summary>
    /// <param name="viewModel">Settings view model to edit.</param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += result =>
        {
            DialogResult = result;
            Close();
        };
    }
}
