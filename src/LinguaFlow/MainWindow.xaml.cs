namespace LinguaFlow;

using System.Windows;
using LinguaFlow.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
