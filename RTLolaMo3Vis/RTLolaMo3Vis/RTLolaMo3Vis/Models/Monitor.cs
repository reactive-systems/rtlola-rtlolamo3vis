using System;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace RTLolaMo3Vis.Models
{
	public struct SafeGraphLineDescription {
		public byte graph_id;
		public byte line_i;
		public string point;

		public SafeGraphLineDescription(byte graph_id, byte line_i, string point)
		{
			this.graph_id = graph_id;
			this.line_i = line_i;
			this.point = point;
		}
	}

	public struct SafeLimitDescription {
		public byte graph_id;
		public byte axis;
		public string stream;

		public SafeLimitDescription(byte graph_id, byte axis, string stream)
		{
			this.graph_id = graph_id;
			this.axis = axis;
			this.stream = stream;
		}
	}

	public class Monitor {
		IntPtr monitorPointer;

		string specString;
		string evSourceCfgString;
		string verdSinkCfgString;
		SafeGraphLineDescription [] lineDescriptions;
		SafeLimitDescription [] limitDescriptions;

		bool specSet = false;
		bool verdSinkSet = false;
		bool evSourceSet = false;


		public Monitor () {
			this.monitorPointer = IntPtr.Zero;
		}

		public void putPointer (IntPtr pointer) {
			this.monitorPointer = pointer;
		}

		public void dropMonitor() {
			this.monitorPointer = IntPtr.Zero;
		}

		public IntPtr getPointer () {
			return this.monitorPointer;
		}

		public void putSpecString (string str) {
			this.specString = str;
			this.specSet = true;
			Debug.WriteLine ("put spec");
		}

		public string getSpecString () {
			return this.specString;
		}

		public void putEvSourceString (string str) { 
			this.evSourceCfgString = str;
			this.evSourceSet = true;
			Debug.WriteLine ("put evsource");
		}

		public string getEvSourceString () {
			return this.evSourceCfgString;
		}

		public void putVerdSinkString (string str) {
			this.verdSinkCfgString = str;
			this.verdSinkSet = true;
			Debug.WriteLine ("put verdsink");
		}

		public string getVerdSinkString () {
			return this.verdSinkCfgString;
		}

		public void putLineDescriptions(SafeGraphLineDescription[] values)
		{
			this.lineDescriptions = values;
			this.verdSinkSet = true;
		}

		public SafeGraphLineDescription [] getLineDescriptions () {
			return this.lineDescriptions;
		}

		public void putLimitDescriptions (SafeLimitDescription [] values) {
			this.limitDescriptions = values;
			this.verdSinkSet = true;
		}

		public SafeLimitDescription [] getLimitDescriptions () {
			return this.limitDescriptions;
		}

		public bool allSet() {
			return (this.evSourceSet && this.specSet && this.verdSinkSet);
		}

		public bool getVerdSink() {
			return this.verdSinkSet;
		}
	}
}

