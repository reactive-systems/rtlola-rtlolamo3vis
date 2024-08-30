using System;
using System.Collections.Generic;
using RTLolaMo3Vis.Models;
using System.Linq;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace RTLolaMo3Vis.Services
{
    public class TriggerDataStore : IDataStore<SpecTrigger, TriggerGrouping>
    {
        private Dictionary<string, Importance> stringToImp;
        private ObservableCollection<TriggerGrouping> specTriggers;
        private const int lastEmptiedLimit = 180;
        private const double tooOldLimit = 300.0;
        private ViewModels.TriggerHistoryViewModel triggerViewModel = null;


        private long lastEmptied { get; set; }

        public TriggerDataStore()
        {
            stringToImp = new Dictionary<string, Importance>();

            specTriggers = new ObservableCollection<TriggerGrouping>();

            specTriggers.Insert(0, new TriggerGrouping(Importance.ADVISORY));
            specTriggers.Insert(1, new TriggerGrouping(Importance.CAUTION));
            specTriggers.Insert(2, new TriggerGrouping(Importance.WARNING));
            specTriggers.Insert(3, new TriggerGrouping(Importance.ALERT));
            specTriggers.Insert(4, new TriggerGrouping(Importance.ERROR));

            DateTimeOffset dto = new();
            lastEmptied = dto.ToUnixTimeSeconds();
            // for debugging only
            Debug.WriteLine("Hier:" + dto.ToString());
            Debug.WriteLine("in long " + lastEmptied);

            Spec spec = new Spec(1, "testSpec");

            /*
            AddItem(new("test1 actual trigger message", Importance.ADVISORY, 0, spec));
            AddItem(new("test2 actual trigger message", Importance.ADVISORY, 0, spec));
            AddItem(new("test3 actual trigger message", Importance.CAUTION, 0, spec));
            AddItem(new("test4 actual trigger message", Importance.CAUTION, 0, spec));
            AddItem(new("test5 actual trigger message", Importance.WARNING, 0, spec));
            AddItem(new("test6 actual trigger message", Importance.WARNING, 0, spec));
            AddItem(new("test7 actual trigger message", Importance.ALERT, 0, spec));
            AddItem(new("test8 actual trigger message", Importance.ALERT, 0, spec));
            AddItem(new("test9 actual trigger message", Importance.ERROR, 0, spec));
            AddItem(new("test10 actual trigger message", Importance.ERROR, 0, spec)); */

        }

        public void AssignViewmodel(ViewModels.TriggerHistoryViewModel viewModel)
        {
            if (triggerViewModel == null)
            {
                triggerViewModel = viewModel;
            }
        }

        public void updateViewmodel()
        {
            if (triggerViewModel == null)
            {
                return;
            }
            triggerViewModel.SpecTriggers = specTriggers;
            triggerViewModel.Advisorys = specTriggers[(int) Importance.ADVISORY].intoObservable();
            triggerViewModel.Cautions = specTriggers[(int)Importance.CAUTION].intoObservable();
            triggerViewModel.Warnings = specTriggers[(int)Importance.WARNING].intoObservable();
            triggerViewModel.Alerts = specTriggers[(int)Importance.ALERT].intoObservable();
            triggerViewModel.Errors = specTriggers[(int)Importance.ERROR].intoObservable();
        }

        public void AddItem(SpecTrigger item)
        {
            int index = (int) item.Importance;

            DateTimeOffset now = new();
            // TODO: do some bug fixing
            //if (now.ToUnixTimeSeconds() - lastEmptied > lastEmptiedLimit)
            //{
                //DeleteItemsPeriodically();
            //}

            //stringToImp.Add(item.StringId, item.Importance);
            specTriggers.ElementAt(index).Insert(0, item);
            //specTriggers.ElementAt(index).Sort(new Comparison<SpecTrigger>((i1, i2) => i2.Timestamp.CompareTo(i1.Timestamp)));
            updateViewmodel();
        }

        public void DeleteAllItems()
        {
            for(int i = 0; i < specTriggers.Count(); i++)
            {
                specTriggers.ElementAt(i).Clear();
            }
            updateViewmodel();
        }

        public IEnumerable<TriggerGrouping> GetAllItems()
        {
            return specTriggers;
        }

        public SpecTrigger GetItem(byte id)
        {
            throw new NotSupportedException();
        }

        public TriggerGrouping GetByImp(Importance importance)
        {
            return specTriggers.ElementAt((int) importance);
        }

        public SpecTrigger GetItemString(string id)
        {
            Importance imp = stringToImp[id];
            int index = (int) imp;
            
            return specTriggers.ElementAt(index).FirstOrDefault(s => id == s.StringId);
        }

        private void DeleteItemsPeriodically()
        {
            DateTimeOffset dto = new();
            lastEmptied = dto.ToUnixTimeSeconds();
            // for debugging only
            Debug.WriteLine(dto.ToString());
            Debug.WriteLine("in long " + lastEmptied);
            foreach (TriggerGrouping group in specTriggers)
            {
                if (group.Count() != 0)
                {
                    double mostRecTime = group.ElementAt(0).Timestamp;
                    int counter = 0;
                    SpecTrigger elem = group.ElementAt(counter);
                    while (counter < group.Count && mostRecTime - elem.Timestamp < tooOldLimit)
                    {
                        counter++;
                        elem = group.ElementAt(counter);
                    }
                    if (counter != group.Count)
                    {
                        group.RemoveRange(counter, group.Count - counter - 1);
                    }

                }
            }
            updateViewmodel();
        }
    }

    public class TriggerGrouping : List<SpecTrigger>
    {
        public (Importance Importance, Color color) PairImportance { get; }

        public TriggerGrouping(Importance importance)
        {
            bool dark = Application.Current.RequestedTheme == OSAppTheme.Dark;

			switch (importance)
            {
                case Importance.ADVISORY:
                    {
                        PairImportance = (importance, dark ? ViewModels.ViolatedTriggerViewModel.PASTEL_BLUE: ViewModels.ViolatedTriggerViewModel.PASTEL_BLUEDARK);
                        break;
                    }
                case Importance.CAUTION:
                    {
                        PairImportance = (importance, dark ? ViewModels.ViolatedTriggerViewModel.PASTEL_LIGHTGREEN: ViewModels.ViolatedTriggerViewModel.PASTEL_LIGHTGREENDARK);
                        break;
                    }
                case Importance.WARNING:
                    {
                        PairImportance = (importance, AppInfo.RequestedTheme == AppTheme.Dark ? ViewModels.ViolatedTriggerViewModel.PASTEL_YELLOW : ViewModels.ViolatedTriggerViewModel.PASTEL_YELLOWDARKDARK);
                        break;
                    }
                case Importance.ALERT:
                    {
                        PairImportance = (importance, ViewModels.ViolatedTriggerViewModel.PASTEL_GREEN);
                        break;
                    }
                case Importance.ERROR:
                    {
                        PairImportance = (importance, ViewModels.ViolatedTriggerViewModel.PASTEL_RED);
                        break;
                    }

                default:
                    break;
            }
        }

        public ObservableCollection<SpecTrigger> intoObservable()
        {
            return new ObservableCollection<SpecTrigger>(this);
        }
    }
}
