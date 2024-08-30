using System;
using System.ComponentModel;
using System.Threading.Tasks;
using RTLolaMo3Vis.Models;
using Xamarin.Forms;

namespace RTLolaMo3Vis.ViewModels
{
    public class StatsPageViewModel : INotifyPropertyChanged
    {
        Color defaultColor = Application.Current.RequestedTheme == OSAppTheme.Light ? OVERLAY_LIGHT : OVERLAY_DARK;
        Color defaultTextColor = Application.Current.RequestedTheme == OSAppTheme.Light ? BLACK : WHITE;
        public StatsPageViewModel()
        {
            ConnectionReceiver connectionReceiver =
                DependencyService.Get<ConnectionReceiver>();

            connectionReceiver.AssignForListening(this);

            Trigger = (defaultColor, new("No trigger", Importance.NOT_KNOWN, 0, defaultSpec), defaultTextColor, LIGHTGREY);
        }

        public async void TriggerThrown(SpecTrigger trigger)
        {
            Trigger = (RED, new(trigger.Importance.ToString(), Importance.NOT_KNOWN, 0, defaultSpec), WHITE, WHITE);
            counter.pre++;
            await Task.Delay(10000);
            counter.post++;
            if (counter.pre == counter.post)
            {
                Trigger = (defaultColor, new("No trigger", Importance.NOT_KNOWN, 0, defaultSpec), defaultTextColor, LIGHTGREY);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        private (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) property;

        private readonly Spec defaultSpec = new();

        private (int pre, int post) counter = (0, 0);

        public static Color GREEN = Color.LimeGreen.MultiplyAlpha(0.85);
        public static Color RED = Color.FromHex("#FC3F26");
        public static Color WHITE = Color.White;
        public static Color BLACK = Color.FromHex("#212529");
        public static Color BLUE = Color.FromHex("#0d6efd");
        public static Color LIGHTGREY = Color.FromHex("#A3A8B5");
        public static Color OVERLAY_LIGHT = Color.FromHex("d9d9d9");
        public static Color OVERLAY_DARK = Color.FromHex("4F4F56");

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Trigger
        {
            get => property;
            set
            {
                property = value;
                OnPropertyChanged("Trigger");
            }
        }
    }
}
