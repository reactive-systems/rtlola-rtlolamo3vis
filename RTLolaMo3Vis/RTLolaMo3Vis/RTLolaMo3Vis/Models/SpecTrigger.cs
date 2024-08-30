using System;
namespace RTLolaMo3Vis.Models
{
    public class SpecTrigger
    {
        public SpecTrigger(string message,
            double timestamp, byte id, Spec spec,
            Importance importance)
        {
            this.message = message;
            this.timestamp = timestamp;
            this.message = message;
            this.id = id;
            this.importance = importance;
            this.spec = spec;
            this.stringId = Guid.NewGuid().ToString();
        }

        public SpecTrigger(string message,
            Importance importance, byte id, Spec spec) : this(message, 0, id,
                spec, importance)
        { }

        public double Timestamp => timestamp;

        public string SpecName => spec.Name;

        public string Message => message;

        public string Explanation => spec.Explanation(id);

        public Importance Importance => importance;

        public string StringId => stringId;

        private readonly string stringId;
        private readonly double timestamp;
        private readonly Spec spec;
        private readonly string message;
        private readonly byte id;
        private readonly Importance importance;
    }

    public enum Importance
    {
        ADVISORY,
        CAUTION,
        WARNING,
        ALERT,
        ERROR,
        NOT_KNOWN
    }
}
