using System;
using RTLolaMo3Vis.Services;
using System.ComponentModel;
using Xamarin.Forms;
using System.Diagnostics;
using RTLolaMo3Vis.Models;

namespace RTLolaMo3Vis.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class TriggerDetailViewModel : INotifyPropertyChanged
    {
        private string itemId;
        private string explanation;
        private string specName;
        private string message;
        private Importance importance;

        public string Explanation
        {
            get => explanation;
            set
            {
                explanation = value;
                OnPropertyChanged("Explanation");
            }
        }

        public string SpecName
        {
            get => specName;
            set
            {
                specName = value;
                OnPropertyChanged("SpecName");
            }
        }

        public Importance Importance
        {
            get => importance;
            set
            {
                importance = value;
                OnPropertyChanged("Importance");
            }
        }

        public string Message
        {
            get => message;
            set 
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }


        public string ItemId
        {
            get
            {
                return itemId;
            }
            set
            {
                itemId = value;
                LoadItemId(value);
            }
        }

        public TriggerDataStore TriggerDataStore => DependencyService.Get<TriggerDataStore>();

        public void LoadItemId(string itemId)
        {
            try
            {
                Models.SpecTrigger triggerer = TriggerDataStore.GetItemString(itemId);
                try
                {
                    this.Explanation = triggerer.Explanation;
                } catch(Exception)
                {
                    this.Explanation = "no Explanation found";
                }
                this.SpecName = triggerer.SpecName;
                this.Message = triggerer.Message;
            } catch(Exception)
            {
                Debug.WriteLine("Trigger could not be loaded");
            }
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
