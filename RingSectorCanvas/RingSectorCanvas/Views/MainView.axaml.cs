using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ReactiveUI;
using RingSectorCanvas.ViewModels;

namespace RingSectorCanvas.Views;

public partial class MainView: ReactiveUserControl<MainViewModel>
{
    
    private MainViewModel mainViewModel;
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            // 获取 ViewModel
            mainViewModel = ViewModel;
            this.FindControl<ViewModels.RingSectorCanvas>("SectorCanvas")
                .SectorClicked += i =>
            {
                var label = $"A{i + 1}";
                Debug.WriteLine($"点击了：{label}");

                // 例如变色反馈
                mainViewModel.SectorFills[i] = Brushes.Magenta;
            };
        });
    }
    
    private void RingSector_PointerPressed(object? sender, PointerEventArgs e)
    {
        Debug.WriteLine($"{sender} clicked=========");
    }
}