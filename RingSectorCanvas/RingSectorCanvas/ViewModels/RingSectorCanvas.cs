using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace RingSectorCanvas.ViewModels;

public class RingSectorCanvas : Control
{
    public static readonly StyledProperty<AvaloniaList<IBrush>> SectorFillsProperty =
        AvaloniaProperty.Register<RingSectorCanvas, AvaloniaList<IBrush>>(nameof(SectorFills));

    public AvaloniaList<IBrush> SectorFills
    {
        get => GetValue(SectorFillsProperty);
        set => SetValue(SectorFillsProperty, value);
    }

    public static readonly StyledProperty<int> SectorCountProperty =
        AvaloniaProperty.Register<RingSectorCanvas, int>(nameof(SectorCount), 1);

    public int SectorCount
    {
        get => GetValue(SectorCountProperty);
        set => SetValue(SectorCountProperty, value);
    }

    public static readonly StyledProperty<bool> ClockwiseProperty =
        AvaloniaProperty.Register<RingSectorCanvas, bool>(nameof(Clockwise), true);

    public bool Clockwise
    {
        get => GetValue(ClockwiseProperty);
        set => SetValue(ClockwiseProperty, value);
    }

    public double InnerRatio { get; set; } = 0.6; // 小圆半径比例

    private readonly List<StreamGeometry> _sectorGeometries = new();

    public event Action<int>? SectorClicked;

    // 添加字段
    private ToolTip? _toolTip;
    private int? _currentSectorUnderMouse = null;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var center = new Point((int)Bounds.Width / 2, (int)Bounds.Height / 2);
        double baseOuterRadius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 2;
        double baseInnerRadius = baseOuterRadius * InnerRatio;
        double anglePerSector = 360.0 / SectorCount;

        var pen = new Pen(Brushes.LimeGreen, 2);
        var textBrush = Brushes.Black;
        var typeface = new Typeface("Arial");

        _sectorGeometries.Clear();

        for (int i = 0; i < SectorCount; i++)
        {
            // 每个扇区独立半径，避免叠加放大
            double outerRadius = baseOuterRadius;
            double innerRadius = baseInnerRadius;

            if (_currentSectorUnderMouse == i)
            {
                outerRadius *= 1.05;
                innerRadius *= 1.05;
            }

            var startAngle = -90 + i * anglePerSector;
            var endAngle = startAngle + anglePerSector;
            //逆时针
            if (!Clockwise)
            {
                startAngle = -90 - i * anglePerSector;
                endAngle = startAngle - anglePerSector;
            }
            
            double rad1 = Math.PI * startAngle / 180.0;
            double rad2 = Math.PI * endAngle / 180.0;

            // 圆弧四点
            var outerStart = new Point(center.X + outerRadius * Math.Cos(rad1),
                center.Y + outerRadius * Math.Sin(rad1));
            var outerEnd = new Point(center.X + outerRadius * Math.Cos(rad2), center.Y + outerRadius * Math.Sin(rad2));
            var innerEnd = new Point(center.X + innerRadius * Math.Cos(rad2), center.Y + innerRadius * Math.Sin(rad2));
            var innerStart = new Point(center.X + innerRadius * Math.Cos(rad1),
                center.Y + innerRadius * Math.Sin(rad1));
            
            double midAngle = (startAngle + endAngle) / 2.0;
            double labelRadius = (innerRadius + outerRadius) / 2;
            var labelPos = new Point(
                center.X + labelRadius * Math.Cos(Math.PI * midAngle / 180.0),
                center.Y + labelRadius * Math.Sin(Math.PI * midAngle / 180.0));
            
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(outerStart, true);
                if (Clockwise)
                {
                    ctx.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, anglePerSector > 180,
                        SweepDirection.Clockwise);
                    ctx.LineTo(innerEnd);
                    ctx.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, anglePerSector > 180,
                        SweepDirection.CounterClockwise);
                }
                else
                {
                    ctx.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, anglePerSector > 180, SweepDirection.CounterClockwise);
                    ctx.LineTo(innerEnd);
                    ctx.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, anglePerSector > 180, SweepDirection.Clockwise);

                }
                ctx.Dispose();
            }

            _sectorGeometries.Add(geometry);
            // 动态颜色填充
            var fill = i < SectorFills.Count ? SectorFills[i] : Brushes.Transparent;

            // 画边框
            context.DrawGeometry(fill, pen, geometry);
            // 绘制文本
            var text = new FormattedText(
                $"G{i + 1}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                12, // 字体大小
                textBrush // 前景画笔
            );
            // 居中位置
            var origin = new Point(labelPos.X - text.Width / 2, labelPos.Y - text.Height / 2);
            // 绘制
            context.DrawText(text, origin);
            Debug.WriteLine(
                $"扇区{i}: G{i + 1}, color={fill}, startAngle={startAngle}, endAngle={endAngle}, 文本位置= {origin}");
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);

        for (int i = 0; i < _sectorGeometries.Count; i++)
        {
            if (_sectorGeometries[i].FillContains(point))
            {
                SectorClicked?.Invoke(i);
                break;
            }
        }
    }

    public RingSectorCanvas()
    {
        this.GetObservable(SectorFillsProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SectorCountProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SectorFillsProperty).Subscribe(fills =>
        {
            if (fills is INotifyCollectionChanged notify)
            {
                notify.CollectionChanged += (_, __) => InvalidateVisual();
            }

            InvalidateVisual(); // 初次也要刷新
        });
        this.PointerPressed += OnPointerPressed; // 注册点击事件
        this.PointerMoved += OnPointerMoved;
        this.PointerExited += OnPointerLeave;

        _toolTip = new ToolTip();
        ToolTip.SetTip(this, _toolTip);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var point = e.GetPosition(this);

        for (int i = 0; i < _sectorGeometries.Count; i++)
        {
            if (_sectorGeometries[i].FillContains(point))
            {
                if (_currentSectorUnderMouse != i)
                {
                    _currentSectorUnderMouse = i;
                    InvalidateVisual(); // 触发重绘
                    _toolTip!.Content = $"这是 G{i + 1} 区域";
                    ToolTip.SetIsOpen(this, true);
                }

                return;
            }
        }

        if (_currentSectorUnderMouse != null)
        {
            _currentSectorUnderMouse = null;
            InvalidateVisual();
        }

        // 未命中任何区域
        ToolTip.SetIsOpen(this, false);
        _currentSectorUnderMouse = null;
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        if (_currentSectorUnderMouse != null)
        {
            _currentSectorUnderMouse = null;
            InvalidateVisual();
        }

        ToolTip.SetIsOpen(this, false);
        _currentSectorUnderMouse = null;
    }
    
    private double NormalizeAngle(double angle)
    {
        // 让角度始终在 [0, 360) 范围内
        return (angle % 360 + 360) % 360;
    }

}