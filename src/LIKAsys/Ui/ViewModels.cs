using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace LIKAsys.Ui
{
    public class Obs : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void Raise([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string n = null)
        {
            if (Equals(field, value)) return false;
            field = value; Raise(n); return true;
        }
    }

    /// <summary>One line of the widget (CPU / GPU / VRAM / RAM / FPS).</summary>
    public class MetricRowVm : Obs
    {
        public string Key { get; set; }

        private string _label = "", _value = "--", _unit = "", _detail = "";
        private double _percent, _percentRest = 100;
        private double _fill, _rowOpacity = 1, _rowShift;
        private Geometry _icon;
        private Brush _valueBrush = Brushes.White, _barBrush = Brushes.Cyan;
        private Brush _labelBrush = Brushes.Gray, _detailBrush = Brushes.Gray, _unitBrush = Brushes.Gray;
        private Brush _trackBrush = Brushes.DimGray, _barMask;
        private Brush _iconStroke = Brushes.Cyan, _iconFill, _iconShadowBrush;
        private double _iconThickness = 1.5, _iconBox = 19;
        private double _labelSize = 10.5, _valueSize = 14, _unitSize = 9.5, _detailSize = 10;
        private double _valueMinWidth = 42, _barHeight = 3;
        private double _valueWidth = double.NaN, _detailWidth = double.NaN;
        private FontWeight _valueWeight = FontWeights.SemiBold, _labelWeight = FontWeights.Medium;
        private Visibility _iconVisibility = Visibility.Visible, _shadowVisibility = Visibility.Visible;
        private Visibility _barVisibility = Visibility.Visible, _detailVisibility = Visibility.Collapsed;
        private Visibility _unitVisibility = Visibility.Visible;
        private Effect _iconEffect;
        private Thickness _rowMargin = new Thickness(0, 3, 0, 3);
        private CornerRadius _barRadius = new CornerRadius(2);

        public string Label { get => _label; set => Set(ref _label, value); }
        public string Value { get => _value; set => Set(ref _value, value); }
        public string Unit { get => _unit; set => Set(ref _unit, value); }
        public string Detail
        {
            get => _detail;
            set { if (Set(ref _detail, value)) DetailVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public double Percent
        {
            get => _percent;
            set { value = Math.Max(0, Math.Min(100, value)); if (Set(ref _percent, value)) { _percentRest = 100 - value; Raise(nameof(PercentRest)); } }
        }
        public double PercentRest => _percentRest;

        /// <summary>
        /// 0..1, what the bar actually draws right now. It is bound to a ScaleTransform,
        /// never to a column width, so easing it costs a repaint and not a layout pass.
        /// The widget's single motion timer walks it towards Percent / 100.
        /// </summary>
        public double Fill { get => _fill; set => Set(ref _fill, value); }

        /// <summary>Entrance fade for one row, 0..1.</summary>
        public double RowOpacity { get => _rowOpacity; set => Set(ref _rowOpacity, value); }

        /// <summary>Entrance offset in px for one row. Render transform, so it never moves layout.</summary>
        public double RowShift { get => _rowShift; set => Set(ref _rowShift, value); }

        public Geometry Icon { get => _icon; set => Set(ref _icon, value); }
        public Brush ValueBrush { get => _valueBrush; set => Set(ref _valueBrush, value); }
        public Brush BarBrush { get => _barBrush; set => Set(ref _barBrush, value); }
        public Brush LabelBrush { get => _labelBrush; set => Set(ref _labelBrush, value); }
        public Brush DetailBrush { get => _detailBrush; set => Set(ref _detailBrush, value); }
        public Brush UnitBrush { get => _unitBrush; set => Set(ref _unitBrush, value); }
        public Brush TrackBrush { get => _trackBrush; set => Set(ref _trackBrush, value); }
        public Brush BarMask { get => _barMask; set => Set(ref _barMask, value); }
        public Brush IconStroke { get => _iconStroke; set => Set(ref _iconStroke, value); }
        public Brush IconFill { get => _iconFill; set => Set(ref _iconFill, value); }
        public Brush IconShadowBrush { get => _iconShadowBrush; set => Set(ref _iconShadowBrush, value); }
        public double IconThickness { get => _iconThickness; set => Set(ref _iconThickness, value); }
        public double IconBox { get => _iconBox; set => Set(ref _iconBox, value); }
        public double LabelSize { get => _labelSize; set => Set(ref _labelSize, value); }
        public double ValueSize { get => _valueSize; set => Set(ref _valueSize, value); }
        public double UnitSize { get => _unitSize; set => Set(ref _unitSize, value); }
        public double DetailSize { get => _detailSize; set => Set(ref _detailSize, value); }
        public double ValueMinWidth { get => _valueMinWidth; set => Set(ref _valueMinWidth, value); }

        /// <summary>
        /// Space reserved for the number, measured once from the font. Without it the card
        /// would grow and shrink every second ("9%" is narrower than "100%") and the whole
        /// widget would visibly jump around the screen.
        /// </summary>
        public double ValueWidth { get => _valueWidth; set => Set(ref _valueWidth, value); }
        public double DetailWidth { get => _detailWidth; set => Set(ref _detailWidth, value); }
        public double BarHeight { get => _barHeight; set => Set(ref _barHeight, value); }
        public CornerRadius BarRadius { get => _barRadius; set => Set(ref _barRadius, value); }
        public FontWeight ValueWeight { get => _valueWeight; set => Set(ref _valueWeight, value); }
        public FontWeight LabelWeight { get => _labelWeight; set => Set(ref _labelWeight, value); }
        public Visibility IconVisibility { get => _iconVisibility; set => Set(ref _iconVisibility, value); }
        public Visibility ShadowVisibility { get => _shadowVisibility; set => Set(ref _shadowVisibility, value); }
        public Visibility BarVisibility { get => _barVisibility; set => Set(ref _barVisibility, value); }
        public Visibility DetailVisibility { get => _detailVisibility; set => Set(ref _detailVisibility, value); }
        public Visibility UnitVisibility { get => _unitVisibility; set => Set(ref _unitVisibility, value); }
        public Effect IconEffect { get => _iconEffect; set => Set(ref _iconEffect, value); }
        public Thickness RowMargin { get => _rowMargin; set => Set(ref _rowMargin, value); }
    }

    /// <summary>double -> GridLength(star) : lets a bar fill a percentage without code-behind math.</summary>
    public class StarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = 0;
            if (value is double v) d = v;
            else double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out d);
            if (d < 0.0001) d = 0.0001;
            return new GridLength(d, GridUnitType.Star);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
