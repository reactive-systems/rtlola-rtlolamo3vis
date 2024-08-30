using System;
namespace RTLolaMo3Vis.Models
{
	public class TriggerDef
	{
		public string Explanation { get; set; }
		public int ID { get; set; }
		public Importance Importance { get; set; }
		public string Name { get; set; }

		public TriggerDef(int ID, string name, string explanation, Importance importance)
		{
			this.ID = ID;
			this.Name = name;
			this.Importance = importance;
			this.Explanation = explanation;
		}
	}
}

