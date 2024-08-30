using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RTLolaMo3Vis.Models;
using Xamarin.Essentials;

namespace RTLolaMo3Vis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SpecDetailPage : ContentPage
    {
        public Spec Spec;
        public SpecDetailPage(Spec spec)
        {
            InitializeComponent();
            Spec = spec;
            collectionView.ItemsSource = Spec;
            listTitle.Text = spec.Name;
            NavigationPage.SetIconColor(this, AppInfo.RequestedTheme == AppTheme.Dark ? Color.White : Color.Black);
        }
    }
}