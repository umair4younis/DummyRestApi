using System.Collections.ObjectModel;

namespace Puma.MDE.Data
{
    public class SwapAlertColor
    {
        public SwapAlertColor(object col) { xColor = col; }
        public SwapAlertColor(int argb)
        {
        }
        public object xColor { get; set; }
        public int ARgbValue { get => 1; }
        public string ColorName { get => xColor.ToString(); }

    }

    public class SwapAlertColorPalette
    {
        public ObservableCollection<SwapAlertColor> Palette { get; set; } = new ObservableCollection<SwapAlertColor>();
        public SwapAlertColorPalette()
        {
        }
    }
}
