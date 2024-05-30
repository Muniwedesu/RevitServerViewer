using RevitServerViewer.ViewModels;
using RevitServerViewer.Views;
using Wpf.Ui.Controls;

namespace RevitServerViewer;

public class ReactiveFluentWindow : FluentWindow, IViewFor<MainWindowViewModel>
{
    object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (MainWindowViewModel)value; }

    public MainWindowViewModel? ViewModel { get; set; }
}