using System;
using System.Collections.Generic;
using RTLolaMo3Vis.Services;
using RTLolaMo3Vis.Models;
using Xamarin.Forms;
using System.Diagnostics;
using System.Linq;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RTLolaMo3Vis.ViewModels
{
    public class SpecsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Spec> specs { get; set; }
        public ObservableCollection<Spec> Specs
        {
            get => specs;
            set
            {
                specs = value;
                OnPropertyChanged("Specs");
            }
        }

        public Command RefreshCommand { get; }

        bool isRefreshing;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        void ExecuteRefreshCommand()
        {
            Debug.WriteLine("Refreshing");
            SpecDataStore specDataStore = DependencyService.Get<SpecDataStore>();
            IEnumerable<Spec> specsEnums = specDataStore.GetAllItems();
            Specs.Clear();
            foreach (var spec in specsEnums)
            {
                Specs.Add(spec);
            }
            Debug.WriteLine("Specs: " + Specs.Count);
            // Stop refreshing
            IsRefreshing = false;
        }
        public SpecsViewModel ()
        {
            SpecDataStore specDataStore = DependencyService.Get<SpecDataStore>();
            specs = new ObservableCollection<Spec>(specDataStore.GetAllItems());
            RefreshCommand = new Command(ExecuteRefreshCommand);
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
