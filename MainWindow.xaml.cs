using System.Windows;
using MediaScanner.ViewModels;

namespace MediaScanner;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}