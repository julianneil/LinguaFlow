namespace LinguaFlow;

using System.Windows;
using LinguaFlow.ViewModels;

/// <summary>
/// Interaction surface for the primary LinguaFlow document workspace.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes the main window and attaches the view model used by the shell.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
