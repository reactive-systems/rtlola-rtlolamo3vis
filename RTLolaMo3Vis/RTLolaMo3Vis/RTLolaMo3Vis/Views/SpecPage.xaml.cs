using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RTLolaMo3Vis.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RTLolaMo3Vis.Views
{
	public partial class SpecPage : ContentPage
    {
		[StructLayout (LayoutKind.Sequential)]
		internal struct GraphLineDescription {
			public byte graph_id;
			public byte line_id;
			public unsafe byte* point;
			public byte length_point;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct LimitDescription {
			public byte graph_id;
			public byte axis;
			public unsafe byte* stream;
			public byte length_stream;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct GraphDescriptions {
			public byte num_lines;
			unsafe public GraphLineDescription* lines;
			public byte num_limits;
			public unsafe LimitDescription* limits;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct MonitorResult {
			public byte error;
			public IntPtr pointer;
			public int lengthError;
			unsafe public byte* errorMessage;
		}


		[DllImport ("__Internal", EntryPoint = "new_monitor")]
		static unsafe extern MonitorResult BuildMonitor (string event_source_cfg,
            string spec_cfg, GraphDescriptions verdict_sink_cfg);

		[DllImport ("__Internal", EntryPoint = "free_monitor")]
		static extern void DropMonitor (IntPtr ptr);

		//public SpecDataStore SpecDataStore => DependencyService.Get<SpecDataStore>();
		public static Color PASTEL_BLUEDARK = Color.FromHex("#3D6463");
        public static Color PASTEL_BLUE = Color.FromHex("#79C7C5");
        public static Color OVERLAY_DARK = Color.FromHex("#4F4F56");
        public static Color OVERLAY_LIGHT = Color.FromHex("#d9d9d9");
        Color checkColor = Application.Current.RequestedTheme == OSAppTheme.Light ? PASTEL_BLUEDARK : PASTEL_BLUE;

        public SpecPage()
        {
            InitializeComponent();
            specFileName.Text = "none";
			clickFrameSpec.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? OVERLAY_DARK : OVERLAY_LIGHT;
		}

		public async void UploadSpec (object sender, EventArgs e) {

			Parser parser = new ();
			String fileName = "none";

			try {
				var result = await FilePicker.PickAsync ();
				if (result != null) {
					var Text = $"File Name: {result.FileName}";
					fileName = result.FileName;

					if (result.FileName.EndsWith ("lola", StringComparison.OrdinalIgnoreCase)) {
						Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
						Stream stream = await result.OpenReadAsync ();
						using var sr = new StreamReader (stream);
						string inputString = sr.ReadToEnd ();
						specEntry.Text = inputString;
						specEntry.HeightRequest = 300;
						monitor.putSpecString (inputString);
						Debug.WriteLine ("spec: " + sr);
					}
				}

			} catch (Exception ex) {
			}
			// removed calls to parser and to update the spec data store
			// spec is no longer parsed just passed on to create the monitor wrapper
			specFileName.Text = fileName;
			clickFrameSpec.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? PASTEL_BLUEDARK : PASTEL_BLUE;
		}

		

		public async void SaveFile(object sender, EventArgs e)
        {
            var fileName = "experiment.json";
            string fileNamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "experiment.json");
            Debug.WriteLine("Path: " + fileNamePath);
            if (File.Exists(fileNamePath))
            {
                Debug.WriteLine("file exists");
            } else
            {
                Debug.WriteLine("file does not exist");
            }
            /*
            if (!File.Exists(path))
            {
                var s = AssetManager.Open(fileName);
                // create a write stream
                FileStream writeStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                // write to the stream
                ReadWriteStream(s, writeStream);
            }*/
        }

		/// <summary>
		/// Build the connection to the DLL
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        public unsafe void MonitorButton(object sender, EventArgs e) {
			Button button = (Button)sender;
			if (button.Text.StartsWith("Build")) {
                Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
				if (monitor.allSet ()) {
					IntPtr currMon = monitor.getPointer ();
					if (currMon != IntPtr.Zero) {
						DropMonitor (currMon);
					}
					SafeGraphLineDescription [] graphLineDescriptions = monitor.getLineDescriptions ();
					List<GraphLineDescription> unsafeGraphLineDescriptions = new();
					foreach (var line in graphLineDescriptions) {
						byte [] asciiBytes = Encoding.UTF8.GetBytes (line.point);
						byte length = (byte) asciiBytes.Length;
						fixed (byte* charPoint = asciiBytes) {
							GraphLineDescription lineDescription = new GraphLineDescription { graph_id = line.graph_id, line_id = line.line_i, point = charPoint, length_point = length };
							unsafeGraphLineDescriptions.Add (lineDescription);
						}
					}

					SafeLimitDescription [] limitDescriptions = monitor.getLimitDescriptions ();
					List<LimitDescription> unsafeLimitDescriptions = new ();
					foreach (var line in limitDescriptions) {
						byte [] asciiBytes = Encoding.UTF8.GetBytes (line.stream);
						byte length = (byte)asciiBytes.Length;
						fixed (byte* charPoint = asciiBytes) {
							LimitDescription limitDescription = new LimitDescription { graph_id = line.graph_id, axis = line.axis, stream = charPoint, length_stream = length };
							unsafeLimitDescriptions.Add (limitDescription);
						}
					}
					byte [] stringBuffer = new byte [100];
					fixed (GraphLineDescription* descrPointer = unsafeGraphLineDescriptions.ToArray ()) {
						fixed (LimitDescription* limPoint = unsafeLimitDescriptions.ToArray()) {
							GraphDescriptions graphDescriptions =
														new GraphDescriptions {
															num_lines = (byte)unsafeGraphLineDescriptions.Count,
															lines = descrPointer,
															num_limits = (byte)unsafeLimitDescriptions.Count,
															limits = limPoint,
														};
							MonitorResult result = BuildMonitor (monitor.getEvSourceString (), monitor.getSpecString (), graphDescriptions);
							Debug.WriteLine ("result error " + result.error);
							if (result.pointer != IntPtr.Zero) {
								IntPtr mon = result.pointer;
								monitor.putPointer (mon);
								Debug.WriteLine ("monitor set");
							} else {
								//TODO: error handling here needed
								// string errorMsg = new string (result.errorMessage, 0, result.lengthError);
								Debug.Assert (result.errorMessage != null);
								string errorMsg = Encoding.ASCII.GetString (result.errorMessage, result.lengthError);
								Debug.Assert (errorMsg.Length == result.lengthError);
								Debug.WriteLine ("error message: " + errorMsg);
								DisplayAlert ("Warning", "Monitor build failed with error: " + errorMsg, "OK");
							}
						}
					}
					
					if (monitor.getPointer () != IntPtr.Zero) {
						button.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? PASTEL_BLUEDARK : PASTEL_BLUE;
						button.Text = "Drop current Monitor";
						return;
					}
				} else {
					Debug.WriteLine ("not all configs set");
					DisplayAlert ("Warning", "You have to set all Specs before a Monitor can be built.", "OK");
				}
			} else if (button.Text.StartsWith ("Drop")) {
				Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
				if (monitor.getPointer () == IntPtr.Zero) {
					Debug.WriteLine ("No monitor to drop");
					DisplayAlert ("Warning", "There is no Monitor to drop, this is most likely caused by a previous error.", "OK");
					return;
				}
				monitor.dropMonitor ();
				Debug.WriteLine ("Monitor dropped");
				button.Text = "Build Monitor with current Specs";
				button.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? OVERLAY_DARK : OVERLAY_LIGHT;
			}
			
		}

		void entryCompleted (Object sender, EventArgs e)
		{
			// TODO: add code necessary to input specs.
			Debug.WriteLine ("entry completed");
			Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
			monitor.dropMonitor ();
			Editor editor = (Editor)sender;
			monitor.putSpecString (editor.Text);
		}
	}
}
