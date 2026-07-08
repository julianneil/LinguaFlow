namespace LinguaFlow;

using System.Windows;
using LinguaFlow.ViewModels;

public partial class SettingsWindow : Window
{
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
