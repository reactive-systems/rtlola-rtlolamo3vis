using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using RTLolaMo3Vis.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RTLolaMo3Vis.ViewModels
{
    public class ViolatedTriggerViewModel : INotifyPropertyChanged
    {
        Color defaultColor = Application.Current.RequestedTheme == OSAppTheme.Light ? OVERLAY_LIGHT : OVERLAY_DARK;
        Color textColor = Application.Current.RequestedTheme == OSAppTheme.Light ? TEXT_LIGHT : TEXT_DARK;
        public ViolatedTriggerViewModel()
        {
            
            ConnectionReceiver connectionReceiver =
                DependencyService.Get<ConnectionReceiver>();
            properties = new Dictionary<Importance, (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor)>(5)
            {
                { Importance.ADVISORY, (defaultColor, CreateInitialTrigger(Importance.ADVISORY), textColor, defaultColor) },
                { Importance.CAUTION, (defaultColor, CreateInitialTrigger(Importance.CAUTION), textColor, defaultColor) },
                { Importance.WARNING, (defaultColor, CreateInitialTrigger(Importance.WARNING), textColor, defaultColor) },
                { Importance.ALERT, (defaultColor, CreateInitialTrigger(Importance.ALERT), textColor, defaultColor) },
                { Importance.ERROR, (defaultColor, CreateInitialTrigger(Importance.ERROR), textColor, defaultColor) }
            };

            counters = new Dictionary<Importance, (int pre, int post)>(5)
            {
                { Importance.ADVISORY, (0, 0) },
                { Importance.CAUTION, (0, 0) },
                { Importance.WARNING, (0, 0) },
                { Importance.ALERT, (0, 0) },
                { Importance.ERROR, (0, 0) }
            };
            connectionReceiver.AssignForListening(this);
        }

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Advisory
        {
            get => properties[Importance.ADVISORY];
            set
            {
                properties[Importance.ADVISORY] = value;
                OnPropertyChanged("Advisory");
            }
        }

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Caution
        {
            get => properties[Importance.CAUTION];
            set
            {
                properties[Importance.CAUTION] = value;
                OnPropertyChanged("Caution");
            }
        }

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Warning
        {
            get => properties[Importance.WARNING];
            set
            {
                properties[Importance.WARNING] = value;
                OnPropertyChanged("Warning");
            }
        }

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Alert
        {
            get => properties[Importance.ALERT];
            set
            {
                properties[Importance.ALERT] = value;
                OnPropertyChanged("Alert");
            }
        }

        public (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor) Error
        {
            get => properties[Importance.ERROR];
            set
            {
                properties[Importance.ERROR] = value;
                OnPropertyChanged("Error");
            }
        }

        public async void TriggerThrown(SpecTrigger trigger)
        {
            SetTrigger(true, trigger, TEXT_DARK, WHITE);
            IncrementCounter(trigger.Importance);
            await Task.Delay(10000);
            IncrementDone(trigger.Importance);
            var (pre, post) = counters[trigger.Importance];
            if (pre == post)
            {
                SetTrigger(true, CreateInitialTrigger(trigger.Importance), TEXT_LIGHT, OVERLAY_DARK);
            }
        }

        private SpecTrigger CreateInitialTrigger(Importance importance) => importance switch
        {
            Importance.ALERT => new("No alert", Importance.ALERT, 0, defaultSpec),
            Importance.ERROR => new("No error", Importance.ERROR, 0, defaultSpec),
            Importance.WARNING => new("No warning", Importance.WARNING, 0, defaultSpec),
            Importance.CAUTION => new("No caution", Importance.CAUTION, 0, defaultSpec),
            Importance.ADVISORY => new("No advice", Importance.ADVISORY, 0, defaultSpec),
            _ => new(" ", Importance.NOT_KNOWN, 0, defaultSpec)
        };
        


        private void SetTrigger(bool triggered, SpecTrigger trigger, Color textcolor, Color bordercolor)
        {
            switch (trigger.Importance)
            {
                case Importance.ADVISORY:
                    {
                        Advisory = (triggered ? PASTEL_BLUE : defaultColor, trigger, textColor, defaultColor);
                        break;
                    }
                case Importance.CAUTION:
                    {
                        Caution = (triggered ? PASTEL_LIGHTGREEN : defaultColor, trigger, textColor, defaultColor);
                        break;
                    }
                case Importance.WARNING:
                    {
                        Warning = (triggered ? PASTEL_YELLOW : defaultColor, trigger, textColor, defaultColor);
                        break;
                    }
                case Importance.ALERT:
                    {
                        Alert = (triggered ? PASTEL_GREEN : defaultColor, trigger, textColor, defaultColor);
                        break;
                    }
                case Importance.ERROR:
                    {
                        Error = (triggered ? PASTEL_RED : defaultColor, trigger, textColor, defaultColor);
                        break;
                    }

                default:
                    break;
            }
        }


        private void IncrementCounter(Importance importance)
        {
            counters[importance] = (counters[importance].pre + 1,
                counters[importance].post);
        }

        private void IncrementDone(Importance importance) =>
            counters[importance] = (counters[importance].pre,
                counters[importance].post + 1);

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly Dictionary<Importance, (int pre, int post)> counters;

        private readonly Dictionary<Importance, (Color color, SpecTrigger trigger, Color textcolor, Color bordercolor)>
            properties;

        private readonly Spec defaultSpec = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public static Color PASTEL_GREEN = Color.FromHex("#708B75");
        public static Color PASTEL_RED = Color.FromHex("#FF9B71");
        public static Color WHITE = Color.White;
        public static Color BLACK = Color.FromHex("#212529");
        public static Color PASTEL_BLUE = Color.FromHex("#79C7C5");
        public static Color OVERLAY_DARK = Color.FromHex("#4F4F56");
        public static Color OVERLAY_LIGHT = Color.FromHex("#d9d9d9");
        public static Color PASTEL_YELLOW = Color.FromHex("#F8F991");
        public static Color PASTEL_YELLOWDARK = Color.FromHex("#ECD282");
        public static Color PASTEL_YELLOWDARKDARK = Color.FromHex ("#b59a4a");
        public static Color PASTEL_LIGHTGREEN = Color.FromHex("#9AB87A");
        public static Color PASTEL_LIGHTGREENDARK = Color.FromHex ("#6F9447");
        public static Color PASTEL_BLUEDARK = Color.FromHex("#3D6463");
        public static Color TEXT_LIGHT = Color.FromHex("#2C2C34");
        public static Color TEXT_DARK = Color.FromHex("#B9B9BB");
    }
}
