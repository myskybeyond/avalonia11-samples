using Avalonia.Collections;
using Avalonia.Media;
using ReactiveUI;

namespace RingSectorCanvas.ViewModels;

public class MainViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static
    
    
    private AvaloniaList<IBrush> _sectorFills = new();

    public AvaloniaList<IBrush> SectorFills
    {
        get => _sectorFills;
        set => this.RaiseAndSetIfChanged(ref _sectorFills, value);
    }
    
    public MainViewModel()
    {
        SectorFills = new AvaloniaList<IBrush>
        {
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Brown,
            Brushes.Yellow,
            Brushes.Cyan
        };
        // 示例：3 秒后改变第一个扇区颜色
        // Task.Run(async () =>
        // {
        //     await Task.Delay(3000);
        //     SectorFills[0] = Brushes.Magenta;
        // });
    }
}