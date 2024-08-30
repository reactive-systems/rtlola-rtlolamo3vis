using Xamarin.Forms;
using Xamarin;
using Xamarin.Essentials;
using RTLolaMo3Vis.Models;
using RTLolaMo3Vis.Services;
using RTLolaMo3Vis.Views;
using System.Reflection;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.Defaults;

namespace RTLolaMo3Vis {
    public partial class App : Application
    {
        AppShell mainpage;

        public App()
        {
			InitializeComponent();
            DependencyService.RegisterSingleton<TriggerDataStore>(new TriggerDataStore());
            DependencyService.RegisterSingleton<Monitor> (new Monitor ());
			// currently initializing multiplecharts data store without any pre-configured charts
			DependencyService.RegisterSingleton<MultipleChartsDataStore> (new MultipleChartsDataStore (new (), new ()));
			DependencyService.RegisterSingleton<ConnectionReceiver> (new ConnectionReceiver ());
			Routing.RegisterRoute("TriggerDetailPage", typeof(TriggerDetailPage));
            MainPage = mainpage = new AppShell();

        }

        protected async override void OnStart()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames();


            //await mainpage.LoadConfig();

            

            /*
            Parser parser = new();
            parser.parse(file);

            DependencyService.RegisterSingleton<SpecDataStore>(new SpecDataStore(parser.Specifications));
            DependencyService.RegisterSingleton<MultipleChartsDataStore>(new MultipleChartsDataStore(parser.Plots, parser.Boundaries));

            DependencyService.RegisterSingleton<ConnectionReceiver>(new ConnectionReceiver());
            */
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
