using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RTLolaMo3Vis.ViewModels;
using Xamarin.Essentials;


namespace RTLolaMo3Vis.Views
{
    // brauch ich den Compile Hinweis?
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TriggerDetailPage : ContentPage
    {
        public TriggerDetailPage()
        {
            InitializeComponent();
            BindingContext = new TriggerDetailViewModel();
            NavigationPage.SetIconColor(this, AppInfo.RequestedTheme == AppTheme.Dark ? Color.White : Color.Black);
        }
    }
}
