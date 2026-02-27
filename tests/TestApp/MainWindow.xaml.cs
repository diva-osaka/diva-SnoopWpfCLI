using System.Diagnostics;
using System.Windows;
using TestApp.ViewModels;

namespace TestApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        UpdateProcessInfo();
    }

    private void CountButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClickCount++;
        _viewModel.StatusMessage = $"Button clicked {_viewModel.ClickCount} time(s)";
    }

    private void UpdateProcessInfo()
    {
        var process = Process.GetCurrentProcess();
        ProcessInfoText.Text = $"PID: {process.Id} | Process: {process.ProcessName}";
    }
}
