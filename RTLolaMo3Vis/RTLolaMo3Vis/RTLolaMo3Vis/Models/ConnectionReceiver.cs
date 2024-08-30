using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RTLolaMo3Vis.Services;
using RTLolaMo3Vis.ViewModels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xamarin.Forms;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Collections.Generic;
using static SkiaSharp.HarfBuzz.SKShaper;
using LiveChartsCore;

namespace RTLolaMo3Vis.Models
{
    public class ConnectionReceiver
    {
        const int TIMEOUT = 3500;

        [DllImport ("__Internal", EntryPoint = "accept_event")]
        static unsafe extern GraphVerdicts* AcceptEvent (IntPtr pointer, byte* input, ulong len, FFI_Duration ts);

		[StructLayout (LayoutKind.Sequential)]
        internal struct FFI_Trigger {
            public ulong timestamp;
            public int length_msg;
            unsafe public byte* msg;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Log {
			public byte length_msg;
			unsafe public byte* msg;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct GraphVerdict {
			public byte graph_id;
			public byte line_id;
			public double x_coord;
			public double y_coord;
			public bool triggered;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Limits {
			public byte graph_id;
			public double min;
			public double max;
			public bool is_x;
		}

		[StructLayout (LayoutKind.Sequential)]
        internal struct GraphVerdicts {
            public ulong num_parsed_bytes;
            public byte num_verdict;
            unsafe public GraphVerdict* verds;
            public byte num_trigger;
            unsafe public FFI_Trigger* triggers;
            public byte num_logs;
            unsafe public Log* logs;
            public byte num_limits;
            unsafe public Limits* limits;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct FFI_Duration {
            public double time;
		}

		

		// INotificationManager notificationManager;

		// save byte arrays that could not be parsed
		List<byte> leftOverBytes = new List<byte>();

        Spec defaultSpec = new (0, "No Spec given");

		public ConnectionReceiver(string username, string password,
            string hostname, string queuename, int port)
        {
            this.username = username;
            this.password = password;
            this.hostname = hostname;
            this.queuename = queuename;
            this.port = port;
            listener = null;
            /*notificationManager = DependencyService.Get<INotificationManager>();
            notificationManager.NotificationReceived += (sender, eventArgs) =>
            {
                var evtData = (NotificationEventArgs)eventArgs;
                ShowNotification(evtData.Title, evtData.Message);
            +
            }; */
        }

        internal void AssignForListening(StatsPageViewModel trialPageViewModel)
        {
            if (this.trialPageViewModel == null)
            {
                this.trialPageViewModel = trialPageViewModel;
            }
        }

        //mac mini -> 127.0.0.1  10.0.2.2
        public ConnectionReceiver() : this("cartlola", "cartlola", "134.96.239.215",
            "RTLola_OUT", 5672)
        { }

        public ConnectionReceiver(string hostname) : this("cartlola", "cartlola", hostname,
            "RTLola_OUT", 5672)
        { }

        public void AssignForListening(ViewModels.ViolatedTriggerViewModel viewModel)
        {
            if (triggerViewModel == null)
            {
                triggerViewModel = viewModel;
            }
        }

        private void MessageHandling(Message message)
        {
            if (message.Type == Message.Kind.TRIGGER)
            {
                Spec spec = new();
                if (spec.Useable)
                {
                    SpecTrigger trigger = new(message.Net_Message, message.Timestamp, message.TriggerId, spec, message.Importance);

                    if (trigger.Importance == Importance.NOT_KNOWN || trigger.Importance == Importance.NOT_KNOWN) return;

                    TriggerDataStore.AddItem(trigger);
                    triggerViewModel?.TriggerThrown(trigger);
                    trialPageViewModel?.TriggerThrown(trigger);
                    //notificationManager.SendNotification(trigger.SpecName, trigger.Message);
                }
            }
            else
            {
                Debug.WriteLine("Plot parsed");
                Debug.WriteLine(message);
                var dataStore = DependencyService.Get<MultipleChartsDataStore>();

                if (message.ChangedBoundaries)
                {
                    dataStore.ChangedLimits(message.XMax, message.XMin, message.YMax, message.YMin, message.PlotId);
                }

                foreach (var point in message.ChangedSeries)
                {
                    if (point.triggered)
                    {
                        Spec spec = new();
                        //get corresponding serie
                        int serie = spec.Plots[message.PlotId].series[point.serie];
                        dataStore.AddItem(new(serie, message.PlotId, point.x, point.y, message.Timestamp, true));
                    }
                    else
                    {
                        Debug.WriteLine("Added point: (plot, serie, x, y, timestamp)" + point.plot + "," + point.serie + "," + point.x + "," + point.y + "," + message.Timestamp);
                        dataStore.AddItem(new(point.serie, point.plot, point.x, point.y, message.Timestamp, true));
                    }
                }
            }
        }

		private unsafe void HandleByteArray(byte [] bytes) {
            try {
                // message.parse();
                Monitor monitor = DependencyService.Get<Monitor> ();
                IntPtr currMon = monitor.getPointer ();

                // TODO: simply just returning if monitor not build yet, should probably save bytes
                if (currMon == IntPtr.Zero) {
                    //leftOverBytes.AddRange (bytes);
                    return;
                }
				//leftOverBytes.AddRange (bytes);
				ulong len = (ulong)bytes.Length;

                byte [] allBytes = bytes;
				fixed (byte* b = allBytes) {
                    double time = DateTimeOffset.UtcNow.ToUnixTimeSeconds ();
                    FFI_Duration dur = new FFI_Duration { time = time };
                    // Debug.WriteLine ("sending bytes event, length: " + leftOverBytes.Count);
					GraphVerdicts* verdicts = AcceptEvent (currMon, b, len,
                       dur);
                    //leftOverBytes.Clear ();
                    // Debug.WriteLine ("accept event done");
					ulong parsed_bytes = verdicts->num_parsed_bytes;
                    int num_verdicts = verdicts->num_verdict;

                    if (parsed_bytes == 0 && num_verdicts == 0) {
                        // TODO: Potentially handle if no bytes were parsed, current handling: do nothing since bytes are already added to leftover bytes
						Debug.WriteLine ("no bytes parsed, added to list of leftover bytes");
                        HandleGraphVerdicts (verdicts);
					} else {
						HandleGraphVerdicts (verdicts);
						//leftOverBytes.RemoveRange (0, (int)parsed_bytes);
					}
				}
			} catch (Exception e) {
				Debug.WriteLine ($"Exception during byte handling: {e}");
			}
		}

		private unsafe void HandleGraphVerdicts (GraphVerdicts* verdicts) {
			// Debug.WriteLine ("Verdict received");
			var dataStore = DependencyService.Get<MultipleChartsDataStore> ();
			//Debug.WriteLine ("verdicts:" + verdicts);
			// Debug.WriteLine ("num verdicts:" + verdicts->num_verdict);

			var triggerDataStore = DependencyService.Get<TriggerDataStore> ();

			// Debug.WriteLine ("num points: " + verdicts->num_verdict);
			for (UInt64 i = 0; i < verdicts->num_verdict; i++) {
                GraphVerdict verd = verdicts->verds [i];

                if (verd.triggered) {
                    
                    // triggered points are at uneven indexes
                    var serieID = verd.line_id * 2 + 1;
					// Debug.WriteLine ("triggered verdict plot: "+ verd.graph_id + ", serie: " + serieID);
					dataStore.AddItem (new (serieID, verd.graph_id, verd.x_coord, verd.y_coord, 0.0, true));
				} else {
                    // untriggered points are at even indexes
					var serieID = verd.line_id * 2;
					dataStore.AddItem (new (serieID, verd.graph_id, verd.x_coord, verd.y_coord, 0.0, true));
				}
			}

			// Debug.WriteLine ("num trigs: " + verdicts->num_trigger);
			for (UInt64 i = 0; i < verdicts->num_trigger; i++) {
				FFI_Trigger trig = verdicts->triggers [i];
				string msg = Encoding.ASCII.GetString (trig.msg, trig.length_msg);

                Importance imp = Importance.NOT_KNOWN;
                if (msg.StartsWith("ALERT")) {
                    imp = Importance.ALERT;
                } else if (msg.StartsWith ("CAUTION")) {
                    imp = Importance.CAUTION;
                } else if (msg.StartsWith ("ERROR")) {
                    imp = Importance.ERROR;
				} else if (msg.StartsWith ("WARNING")) {
                    imp = Importance.WARNING;
				} else {
                    imp = Importance.ADVISORY;
                }
                // also specs not relevant anymore
				SpecTrigger specTrigger = new (msg, trig.timestamp, 0, defaultSpec, imp);
				triggerDataStore.AddItem (specTrigger);
				// Debug.WriteLine ("trigger added");
			}

            // Debug.WriteLine ("num logs: " + verdicts->num_logs);
			for (UInt64 i = 0; i < verdicts->num_logs; i++) {
				Log log = verdicts->logs [i];
                // Debug.WriteLine ("message length log: " + log.length_msg);
				string msg = Encoding.ASCII.GetString (log.msg, log.length_msg);

				// also specs not relevant anymore
				SpecTrigger specTrigger = new (msg, 0.0, 0, defaultSpec, Importance.ERROR);
				triggerDataStore.AddItem (specTrigger);
				// Debug.WriteLine ("log added");
			}

			// Debug.WriteLine ("num limits: " + verdicts->num_limits);
			for (UInt64 i = 0; i < verdicts->num_limits; i++) {
				Limits lim = verdicts->limits [i];

				// also specs not relevant anymore
				// Debug.WriteLine ("graph: " + lim.graph_id + ", min: " + lim.min + ", max: " + lim.max);
				if (lim.is_x) {
					dataStore.ChangedXLimits (lim.max, lim.min, lim.graph_id);
                } else {
					dataStore.ChangedYLimits (lim.max, lim.min, lim.graph_id);
				}
			}
		}

		public async Task HandleMessage(object channel, BasicDeliverEventArgs ea)
        {
            Message message = new(ea.Body.ToArray());
            message.parse();

            MessageHandling(message);
        }

		public async Task HandleMessageToBytes(object channel, BasicDeliverEventArgs ea)
		{
            HandleByteArray (ea.Body.ToArray ());
		}

		private void ShowNotification(string title, string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var msg = new Label()
                {
                    Text = $"Notification Received:\nTitle: {title}\nMessage: {message}"
                };
            });
        }

        private Services.TriggerDataStore TriggerDataStore => DependencyService.Get<Services.TriggerDataStore>();

        public ConnectionStatus ConnectStatus => connectStatus;

        private ConnectionStatus connectStatus = ConnectionStatus.NOT_INITIALIZED;

        private string username;
        private string password;
        private string hostname;
        private string queuename;
        private int port;
        private UdpClient listener;

        public string Username
        {
            get => username;
            set => username = value;
        }

        public string Password
        {
            get => password;
            set => password = value;
        }

        public string Hostname
        {
            get => hostname;
            set => hostname = value;
        }

        public string Queuename
        {
            get => queuename;
            set => queuename = value;
        }

        public int Port
        {
            get => port;
            set => port = value;
        }


        private string tag;


        private ViewModels.ViolatedTriggerViewModel triggerViewModel = null;
        private StatsPageViewModel trialPageViewModel = null;

        public async void StartListener(int listenPort, IPAddress iPAddress)
        {
            /*
            IPEndPoint groupEP = new IPEndPoint(iPAddress, listenPort);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);

                    Console.WriteLine($"Received broadcast from {groupEP} :");
                    Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            } */
            listener = new UdpClient(listenPort);
            try
            {
                listener.BeginReceive(new AsyncCallback(recv), null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            void recv(IAsyncResult res)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
                try
                {
                    byte[] received = listener.EndReceive(res, ref RemoteIpEndPoint);
                    listener.BeginReceive(new AsyncCallback(recv), null);

                    // Debug.WriteLine("received message");
                    // Message message = new(received);

                    HandleByteArray (received);

                    // MessageHandling(message);
                    
                    //Console.WriteLine($"Received broadcast:");
                    //Console.WriteLine($" {Encoding.ASCII.GetString(received, 0, received.Length)}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return;
                }


                //Process code

            }
        }

        public void CloseListener()
        {
            listener?.Close();
        }
    }

    public enum ConnectionStatus
    {
        CONNECTED,
        DISCONNECTED,
        TIMEOUT,
        UNREACHABLE,
        NOT_INITIALIZED,
        CONNECTION_IS_NULL
    }
}
