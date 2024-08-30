using System;
using System.Collections.Generic;

namespace RTLolaMo3Vis.Models
{
    public class Spec : List<SpecTrigger>
    {
        public Spec(byte id, string name ,bool inUse)
        {
            this.id = id;
            this.inUse = inUse;
            this.triggerInformation = new();
            this.plotInformation = new();
            this.name = name;
        }

        public Spec(byte id, string name) : this(id, name, true) { }

        //default spec 
        public Spec() : this(0, "", true)
        {
            triggerInformation.Add(0, ("", "", Importance.NOT_KNOWN));
        }

        public bool Useable
        {
            get => inUse;
            set
            {
                inUse = value;
            }
        }

        public byte Id => id;

        public string stringId => id.ToString();

        public string Name
        {
            get => name;
        }


        public string Explanation(byte triggerId) => triggerInformation[triggerId].explanation;

        public string Triggername(byte triggerId) => triggerInformation[triggerId].name;

        private bool inUse;
        private readonly byte id;
        private readonly Dictionary<byte, (string name, string explanation, Importance importance)> triggerInformation;
        private readonly string name;

        private readonly Dictionary<byte, (string x, string y, Dictionary<byte, int> series)> plotInformation;

        public Dictionary<byte, (string x, string y, Dictionary<byte, int> series)> Plots => plotInformation;

        public void AddTriggerInformation(byte triggerId, string name, Importance importance, string explanation)
        {
            triggerInformation.Add(triggerId, (name, explanation, importance));
        }

        public void AddPlot(byte plot, string x, string y)
        {
            plotInformation.Add(plot, (x, y, new()));
        }

        public bool AddSerie(byte plot, byte serie)
        {
            if (!plotInformation.ContainsKey(plot)) plotInformation.Add(plot, new());
            if (plotInformation[plot].series.ContainsKey(serie)) return false;
            plotInformation[plot].series.Add(serie, -1);
            return true;
        }

        public bool AddSerie(byte plot, byte serie, int trigger)
        {
            if (!plotInformation.ContainsKey(plot)) plotInformation.Add(plot, new());
            if (plotInformation[plot].series.ContainsKey(serie)) return false;
            plotInformation[plot].series.Add(serie, trigger);
            return true;
        }

        public bool ExistsTrigger(byte triggerId)
        {
            return triggerInformation.ContainsKey(triggerId);
        }

        public bool ExitsPlot(byte plotId)
        {
            return plotInformation.ContainsKey(plotId);
        }
    }

    
}
