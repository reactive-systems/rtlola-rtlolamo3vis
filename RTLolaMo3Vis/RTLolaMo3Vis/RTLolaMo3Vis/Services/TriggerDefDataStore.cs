using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RTLolaMo3Vis.Models;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RTLolaMo3Vis.Services
{
	public class SpecDataStore : IDataStore<Spec, Spec>
	{
		public void AddItem(Spec item)
		{
			throw new NotImplementedException();
		}

		public void DeleteAllItems()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<Spec> GetAllItems()
		{
			throw new NotImplementedException();
		}

		public Spec GetItem(byte id)
		{
			throw new NotImplementedException();
		}

		public Spec GetItemString(string id)
		{
			throw new NotImplementedException();
		}
	}
}
