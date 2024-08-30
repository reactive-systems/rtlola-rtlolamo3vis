using System;
using System.ComponentModel;
using RTLolaMo3Vis.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading;
using RTLolaMo3Vis.Services;
using Xamarin.Essentials;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace RTLolaMo3Vis.Views
{
    public partial class ConnectionPage : ContentPage {
        String configName = "none";
        //SpecDataStore specDatastore = null;
        public static int DEFAULT_UDPPORT = 5005;
        public static IPAddress DEFAULT_UDPIP = IPAddress.Any;
        UdpClient listener = null;
        bool udpConnected = false;

		public static Color PASTEL_BLUE = Color.FromHex ("#79C7C5");
		public static Color PASTEL_BLUEDARK = Color.FromHex ("#3D6463");
		public static Color OVERLAY_DARK = Color.FromHex ("#4F4F56");
		public static Color OVERLAY_LIGHT = Color.FromHex ("#d9d9d9");
        public static Color BUTTON_COLOR = Color.FromHex ("#3E3E45");

		public ConnectionPage () {
            InitializeComponent ();
			evSourcFileNameLabel.Text = "none";
			clickFrameEventSource.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? OVERLAY_DARK : OVERLAY_LIGHT;
        }

        protected override void OnAppearing () {
        }

        /// <summary>
        /// initiate the UDP connection according to the parameters set by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectUDP(object sender, EventArgs e)
        {

            /*
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there");
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, "134.96.239.215", 5005);
                Debug.WriteLine("should be sent");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            } */
            Models.ConnectionReceiver connectionReceiver = DependencyService.Get<Models.ConnectionReceiver>();
            if (!udpConnected)
            {
                int portNum = DEFAULT_UDPPORT;
                IPAddress ipAddress = DEFAULT_UDPIP;
                if (portUDP.Text != null)
                {
                    if (Int32.TryParse(portUDP.Text, out int portNumber))
                    {
                        portNum = portNumber;
                        Debug.WriteLine($"Port Num: {portNumber}");
                    }
                }
                if (ipAddressUDP != null)
                {
                    if (IPAddress.TryParse(ipAddressUDP.Text, out IPAddress ip))
                    {
                        ipAddress = ip;
                    }
                }

                // listener = new UdpClient(portNum);

                connectionReceiver.StartListener(portNum, ipAddress);
                udpConnected = true;
                (sender as Button).Text = "Stop Listening";
                (sender as Button).TextColor = Color.White;
                (sender as Button).BackgroundColor = Application.Current.RequestedTheme == OSAppTheme.Light ? OVERLAY_DARK : BUTTON_COLOR;
			}
            else
            {
                connectionReceiver.CloseListener();
                udpConnected = false;
                (sender as Button).Text = "Start Listening";
                (sender as Button).TextColor = Application.Current.RequestedTheme == OSAppTheme.Light ? Color.Black : Color.White;
				(sender as Button).BackgroundColor = Application.Current.RequestedTheme == OSAppTheme.Light ? PASTEL_BLUE : PASTEL_BLUEDARK;
			}
			
		}


		public async void UploadConfig(object sender, EventArgs e)
        {

            Parser parser = new();

            try
            {
                var result = await FilePicker.PickAsync();
                if (result != null)
                {
                    var Text = $"File Name: {result.FileName}";
                    configName = result.FileName;

                    if (result.FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
                    {
                        parser.parse(await result.OpenReadAsync());
                    }
                }

            }
            catch (Exception ex)
            {
            }

            //parser.Specifications.Add(new());
            //fileNameLabel.Text = configName;
            //clickFrame.BackgroundColor = PASTEL_BLUEDARK;


            DependencyService.Get<MultipleChartsDataStore>().Update(parser.Plots, parser.Boundaries);

            //DependencyService.RegisterSingleton<ConnectionReceiver>(new ConnectionReceiver());
        }

		/// <summary>
		/// Start the FilePicker, user picks a file, string of that file is saved in
		/// Monitor object so that it can be used to build the connection to the DLL
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void UploadEvSourcCfg (object sender, EventArgs e) {

			Parser parser = new ();
			String fileName = "none";

			try {
				var result = await FilePicker.PickAsync ();
				if (result != null) {
					var Text = $"File Name: {result.FileName}";
					fileName = result.FileName;

					if (result.FileName.EndsWith ("json", StringComparison.OrdinalIgnoreCase)) {
						Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
						Stream stream = await result.OpenReadAsync ();
						using var sr = new StreamReader (stream);
						monitor.putEvSourceString (sr.ReadToEnd ());
					}
				}

			} catch (Exception ex) {
			}

            // Currently will use hard coded default, thus not needed added here
			/*
            if (fileName == "none") {
                fileName = "default";
				Models.Monitor monitor = DependencyService.Get<Models.Monitor> ();
				monitor.putEvSourceString ("not needed currently");
			}*/

			evSourcFileNameLabel.Text = fileName;
			clickFrameEventSource.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? PASTEL_BLUEDARK : PASTEL_BLUE;
		}

	}
}
