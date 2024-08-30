using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.CommunityToolkit;
using Xamarin.CommunityToolkit.UI.Views;
using System.Diagnostics;

namespace RTLolaMo3Vis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedHistoryPage : ContentPage
    {
        public static Color PASTEL_GREEN = Color.FromHex("#708B75");
        public static Color PASTEL_RED = Color.FromHex("#FF9B71");
        public static Color WHITE = Color.White;
        public static Color BLACK = Color.FromHex("#212529");
        public static Color PASTEL_BLUE = Color.FromHex("#79C7C5");
        public static Color LIGHTGREY = Color.FromHex("#B9B9BB");
        public static Color PASTEL_YELLOW = Color.FromHex("#F8F991");
        public static Color PASTEL_LIGHTGREEN = Color.FromHex("#9AB87A");
        public static Color PASTEL_BLUEDARK = Color.FromHex("#3D6463");
        public static Color BACKGROUND_DARK = Color.FromHex("#2C2C34");
        public static Color BACKGROUND_LIGHT = Color.FromHex("#000000");
        public static Color OVERLAY_DARK = Color.FromHex("#4F4F56");
        public static Color OVERLAY_LIGHT = Color.FromHex("#d9d9d9");
        public static Color TEXT_LIGHT = Color.FromHex("#2C2C34");
        public static Color TEXT_DARK = Color.FromHex("#B9B9BB");

        public TabbedHistoryPage()
        {
            InitializeComponent();
            HighlightColor = Color.Blue;
        }

        public Color HighlightColor { get; set; }

        public void SelectionChanged(object sender, TabSelectionChangedEventArgs e)
        {
            int index = ((TabView) sender).SelectedIndex;
            List<Color> colors = new List<Color> { Color.White, Color.FromHex("#79C7C5"), PASTEL_LIGHTGREEN, PASTEL_YELLOW, PASTEL_GREEN, PASTEL_RED};
            Debug.WriteLine("curr index: " + index);
            HighlightColor = colors[index];
        }
    }
}