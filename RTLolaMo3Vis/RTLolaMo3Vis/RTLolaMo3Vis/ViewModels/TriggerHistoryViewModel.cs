using RTLolaMo3Vis.Models;
using System.Collections.ObjectModel;
using RTLolaMo3Vis.Services;
using Xamarin.Forms;
using System.ComponentModel;
using RTLolaMo3Vis.Views;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RTLolaMo3Vis.ViewModels
{
    public class TriggerHistoryViewModel : INotifyPropertyChanged
    { 

        private SpecTrigger selectionAll { get; set; }
        public SpecTrigger SelectionAll
        {
            get => selectionAll;
            set
            {
                selectionAll = value;
                if (selectionAll == null)
                {
                    OnPropertyChanged("SelectionAll");
                    return;
                }

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionAll.StringId}");
                OnPropertyChanged("SelectionAll");
                SelectionAll = null;
            }
        }

        private SpecTrigger selectionError { get; set; }
        public SpecTrigger SelectionError
        {
            get => selectionError;
            set
            {
                selectionError = value;
                if (selectionError == null)
                {
                    OnPropertyChanged("SelectionError");
                    return;
                }

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionError.StringId}");
                OnPropertyChanged("SelectionError");
                SelectionError = null;
            }
        }

        private SpecTrigger selectionCaution { get; set; }
        public SpecTrigger SelectionCaution
        {
            get => selectionCaution;
            set
            {
                selectionCaution = value;
                if (selectionCaution == null)
                {
                    OnPropertyChanged("SelectionCaution");
                    return;
                }

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionCaution.StringId}");
                OnPropertyChanged("SelectionCaution");
                SelectionCaution = null;
            }
        }

        private SpecTrigger selectionWarning { get; set; }
        public SpecTrigger SelectionWarning
        {
            get => selectionWarning;
            set
            {
                selectionWarning = value;
                if (selectionWarning == null)
                {
                    OnPropertyChanged("SelectionWarning");
                    return;
                } 

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionWarning.StringId}");
                OnPropertyChanged("SelectionWarning");
                SelectionWarning = null;
            }
        }

        private SpecTrigger selectionAdvi { get; set; }
        public SpecTrigger SelectionAdvi
        {
            get => selectionAdvi;
            set
            {
                selectionAdvi = value;
                if (selectionAdvi == null)
                {
                    OnPropertyChanged("SelectionAdvi");
                    return;
                }

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionAdvi.StringId}");
                OnPropertyChanged("SelectionAdvi");
                SelectionAdvi = null;
            }
        }

        private SpecTrigger selectionAlert { get; set; }
        public SpecTrigger SelectionAlert
        {
            get => selectionAlert;
            set
            {
                selectionAlert = value;
                if (selectionAlert == null)
                {
                    OnPropertyChanged("SelectionAlert");
                    return;
                }

                Shell.Current.GoToAsync($"{nameof(TriggerDetailPage)}?{nameof(TriggerDetailViewModel.ItemId)}={selectionAlert.StringId}");
                OnPropertyChanged("SelectionAlert");
                SelectionAlert = null;
            }
        }

        public Command DeleteItemsCommand { get; }

        public Command LoadItemsCommand { get; }

        private bool isBusy = false;
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        public ObservableCollection<TriggerGrouping> SpecTriggers
        {
            get => triggerGroups;
            set
            {
                triggerGroups = value;
                OnPropertyChanged("SpecTriggers");
            }
        }

        private ObservableCollection<SpecTrigger> errors { get; set; }

        public ObservableCollection<SpecTrigger> Errors
        {
            get => errors;
            set
            {
                errors = value;
                OnPropertyChanged("Errors");
            }
        }

        private ObservableCollection<SpecTrigger> warnings { get; set; }

        public ObservableCollection<SpecTrigger> Warnings
        {
            get => warnings;
            set
            {
                warnings = value;
                OnPropertyChanged("Warnings");
            }
        }

        private ObservableCollection<SpecTrigger> cautions { get; set; }

        public ObservableCollection<SpecTrigger> Cautions
        {
            get => cautions;
            set
            {
                cautions = value;
                OnPropertyChanged("Cautions");
            }
        }

        private ObservableCollection<SpecTrigger> advisorys { get; set; }

        public ObservableCollection<SpecTrigger> Advisorys
        {
            get => advisorys;
            set
            {
                advisorys = value;
                OnPropertyChanged("Advisorys");
            }
        }

        private ObservableCollection<SpecTrigger> alerts { get; set; }

        public ObservableCollection<SpecTrigger> Alerts
        {
            get => alerts;
            set
            {
                alerts = value;
                OnPropertyChanged("ALerts");
            }
        }

        private ObservableCollection<TriggerGrouping> triggerGroups { get; set; }

        public TriggerDataStore TriggerDataStore => DependencyService.Get<TriggerDataStore>();

        public TriggerHistoryViewModel()
        {
            DeleteItemsCommand = new Command(OnDeleteItems);
            LoadItemsCommand = new Command(ExecuteLoadItemsCommand);

            triggerGroups = new ObservableCollection<TriggerGrouping>(TriggerDataStore.GetAllItems());
            advisorys = triggerGroups[(int) Importance.ADVISORY].intoObservable();
            cautions = triggerGroups[(int)Importance.CAUTION].intoObservable();
            warnings = triggerGroups[(int)Importance.WARNING].intoObservable();
            alerts = triggerGroups[(int)Importance.ALERT].intoObservable();
            errors = triggerGroups[(int)Importance.ERROR].intoObservable();
            TriggerDataStore.AssignViewmodel(this);
        }

        async void ExecuteLoadItemsCommand()
        {
            
            IsBusy = true;
            SpecTriggers = await Task.Run(() => (ObservableCollection<TriggerGrouping>)TriggerDataStore.GetAllItems());
            Advisorys = SpecTriggers[(int)Importance.ADVISORY].intoObservable();
            Cautions = SpecTriggers[(int)Importance.CAUTION].intoObservable();
            Warnings = SpecTriggers[(int)Importance.WARNING].intoObservable();
            Alerts = SpecTriggers[(int)Importance.ALERT].intoObservable();
            Errors = SpecTriggers[(int)Importance.ERROR].intoObservable();
            IsBusy = false;
        }

        void OnDeleteItems()
        {
            // TODO: displayalert unterbringen
            TriggerDataStore.DeleteAllItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
            {
                return;
            }
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
