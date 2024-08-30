using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTLolaMo3Vis.ViewModels;
using RTLolaMo3Vis.Services;
using System.Collections.ObjectModel;
using LiveChartsCore.SkiaSharpView;
using Xamarin.Forms;
using LiveChartsCore;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Diagnostics;
using LiveChartsCore.SkiaSharpView.Painting;
using Xamarin.Essentials;
using RTLolaMo3Vis.Models;
using System.IO;

namespace RTLolaMo3Vis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatsPage : ContentPage
    {
        Frame triggerFramesaved { get; set; }

        bool horizontal = false;

        public static Color PASTEL_GREEN = Color.FromHex("#708B75");
        public static Color PASTEL_RED = Color.FromHex("#FF9B71");
        public static Color WHITE = Color.White;
        public static Color BLACK = Color.FromHex("#212529");
        public static Color PASTEL_BLUE = Color.FromHex("#79C7C5");
        public static Color LIGHTGREY = Color.FromHex("#B9B9BB");
        public static Color PASTEL_YELLOW = Color.FromHex("#F8F991");
        public static Color PASTEL_LIGHTGREEN = Color.FromHex("#9AB87A");
        public static Color PASTEL_BLUEDARK = Color.FromHex("#3D6463");
        public static Color BACKGROUND_DARK = Color.FromHex("#2C2C34");
        public static Color BACKGROUND_LIGHT = Color.FromHex("#000000");
        public static Color OVERLAY_DARK = Color.FromHex("#4F4F56");
        public static Color OVERLAY_LIGHT = Color.FromHex("#d9d9d9");
        public static Color TEXT_LIGHT = Color.FromHex("#2C2C34");
        public static Color TEXT_DARK = Color.FromHex("#B9B9BB");

        protected override void OnAppearing()
        {
            if (MultipleChartsDataStore.changed)
            {
                outerStack.Children.Clear();
                // outerStack.SetAppThemeColor(StackLayout.BackgroundColorProperty, BACKGROUND_DARK, BACKGROUND_LIGHT);
                // outerStack.BackgroundColor = PASTEL_RED;
                outerStack.BackgroundColor = Color.Transparent;
                List<(string x, string y)> plotAchsisNames = MultipleChartsDataStore.plotAchsisNames;

                SeriesList = new ObservableCollection<ObservableCollection<ISeries>>(MultipleChartsDataStore.GetAllItems());

                StackLayout mainStack1 = new();
                
                StackLayout mainStack2 = new();
                
                fillStacks(outerStack, mainStack1, mainStack2, SeriesList, plotAchsisNames);

                Content = outerStack;
            }

			Monitor mon = DependencyService.Get<Monitor> ();
            if (mon.getVerdSink () && horizontal) outerStack.Orientation = StackOrientation.Horizontal;
			base.OnAppearing();
        }

        public ObservableCollection<ObservableCollection<ISeries>> SeriesList { get; set; }
        public MultipleChartsDataStore MultipleChartsDataStore => DependencyService.Get<MultipleChartsDataStore>();

        Button deleteAllButton = new Button
        {
            Text = "deleteAll",
            ImageSource = "delete",
            BackgroundColor = Color.Transparent,
            TextColor = Color.White,
        };


        Grid TitleGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(6, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) },
            }
        };

        private double width = 0;
        private double height = 0;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width != this.width || height != this.height)
            {
                this.width = width;
                this.height = height;
                Monitor mon = DependencyService.Get<Monitor> ();
                if (width > height)
                {
                    if (mon.getVerdSink ())
                    outerStack.Orientation = StackOrientation.Horizontal;
                    horizontal = true;
                    
                }
                else
                {
                    outerStack.Orientation = StackOrientation.Vertical;

                }
            }
        }

        public StatsPage()
        {
            InitializeComponent();
			verdSinkFileNameLabel.Text = "none";
			clickFrameVerdictSink.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? OVERLAY_DARK : OVERLAY_LIGHT;

			this.triggerFramesaved = triggerFrame;
            List<(string x, string y)> plotAchsisNames = MultipleChartsDataStore.plotAchsisNames;
            deleteAllButton.Clicked += deleteAll;
            AppTheme appTheme = AppInfo.RequestedTheme;
            TitleGrid.Children.Add(new Label
            {
                Text = "Charts",
                FontSize = 28,
                FontFamily = "MontserraBold",
                VerticalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = WHITE,
            }, 0, 0);
            //TitleGrid.Children.Add(deleteAllButton, 1, 0);
            Shell.SetTitleView(this, TitleGrid);
            
            outerStack.Spacing = 20;

            outerStack.Padding = 10;

            SeriesList = new ObservableCollection<ObservableCollection<ISeries>>(MultipleChartsDataStore.GetAllItems());

			fillStacks (outerStack, mainStack1, mainStack2, SeriesList, plotAchsisNames);
		}

        private void fillStacks(StackLayout outerStack, StackLayout mainStack1,  StackLayout mainStack2, ObservableCollection<ObservableCollection<ISeries>> SeriesList,
            List<(string x, string y)> plotAchsisNames)
        {
            mainStack1.BackgroundColor = Color.Transparent;
            mainStack2.BackgroundColor = Color.Transparent;
            mainStack1.WidthRequest = 1000;
            mainStack2.WidthRequest = 700;
            mainStack2.VerticalOptions = LayoutOptions.Center;
            int secondHalf = 0;
            if (SeriesList.Count % 2 != 0)
            {
                secondHalf = SeriesList.Count / 2 + 1;
            } else
            {
                secondHalf = SeriesList.Count / 2;
            }

            Color background = AppInfo.RequestedTheme == AppTheme.Dark ? OVERLAY_DARK : OVERLAY_LIGHT;
            bool dark = AppInfo.RequestedTheme == AppTheme.Dark;
			//SolidColorPaint labelNamePaint = AppInfo.RequestedTheme == AppTheme.Dark ? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255))
                //: new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0));

			for (int i = 0; i < secondHalf  && i < SeriesList.Count; i++)
            {
                mainStack1.Children.Add(new Frame
                {
                    Content = new LiveChartsCore.SkiaSharpView.Xamarin.Forms.CartesianChart
                    {
                        BackgroundColor = background,
                        Series = SeriesList[i],
                        XAxes = new Axis[] {
                        new Axis
                        {
                            Name = plotAchsisNames[i].x,
							TextSize = 8,
							NameTextSize = 10,
							LabelsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                            NamePaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                            ShowSeparatorLines = false,
                            SeparatorsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0))
						}
                    },
                        YAxes = new Axis[] {
                        new Axis
                        {
                            Name = plotAchsisNames[i].y,
							TextSize = 8,
                            NameTextSize = 10,
							LabelsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                            NamePaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                            ShowSeparatorLines = false,
                            SeparatorsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0))
						}
                    }
                    },
                    CornerRadius = 10,
                    BackgroundColor = background
                });

                mainStack1.Children[i].HeightRequest = 500;
            }

            for (int i = secondHalf ; i < SeriesList.Count; i++)
            {
                mainStack2.Children.Insert(0, new Frame {
                    Content = new LiveChartsCore.SkiaSharpView.Xamarin.Forms.CartesianChart
                    {
                        BackgroundColor = background,
                        Series = SeriesList[i],
                        XAxes = new Axis[] {
                            new Axis
                            {
                                Name = plotAchsisNames[i].x,
								TextSize = 8,
								NameTextSize = 10,
								LabelsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                                NamePaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                                ShowSeparatorLines = false,
                            }
                        },
                        YAxes = new Axis[] {
                            new Axis
                            {
                                Name = plotAchsisNames[i].y,
								TextSize = 8,
								NameTextSize = 10,
								LabelsPaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                                NamePaint = dark? new SolidColorPaint (new SkiaSharp.SKColor (255, 255, 255)) : new SolidColorPaint (new SkiaSharp.SKColor (0, 0, 0)),
                                ShowSeparatorLines = false
                            }
                        }
                    },
                    CornerRadius = 10,
                    BackgroundColor = background
                });

                mainStack2.Children[i - (secondHalf)].HeightRequest = 500;
            }
            outerStack.Children.Add(mainStack1);
            outerStack.Children.Add(mainStack2);
            //outerStack.Children.Add(triggerFramesaved);
        }

        public void deleteAll(object sender, EventArgs e)
        {
            var dataStore = DependencyService.Get<MultipleChartsDataStore>();

            dataStore.DeleteAllItems();
        }

		public async void UploadVerdictSinkCfg (object sender, EventArgs e) {

			Parser parser = new ();
			String fileName = "none";

			try {
				var result = await FilePicker.PickAsync ();
				if (result != null) {
					var Text = $"File Name: {result.FileName}";
					fileName = result.FileName;

					if (result.FileName.EndsWith ("json", StringComparison.OrdinalIgnoreCase)) {
						Monitor monitor = DependencyService.Get<Monitor> ();
						Stream stream = await result.OpenReadAsync ();
                        parser.parseWithoutSpecs (stream);
                        // TODO: plots need to be updated and monitor needs other data structure
                        MultipleChartsDataStore chartsStore = DependencyService.Get<MultipleChartsDataStore> ();
                        chartsStore.Update (parser.Plots, parser.Boundaries);
						monitor.putLineDescriptions (parser.MonitorPlotInfo.graphLineDescriptions.ToArray());
						monitor.putLimitDescriptions (parser.MonitorPlotInfo.limitDescriptions.ToArray ());
						//foreach (var item in parser.MonitorPlotInfo) {
                        //    Debug.WriteLine ("plot: " + item.graph_id + ", series: " + item.line_i + ", stream: " + item.point);
                        //}
					}
				}

			} catch (Exception ex) {
                Debug.WriteLine ("exception caught: " + ex);
			}

			// again: call to update SpecDataStore removed
			verdSinkFileNameLabel.Text = fileName;
			clickFrameVerdictSink.BackgroundColor = AppInfo.RequestedTheme == AppTheme.Dark ? PASTEL_BLUEDARK : PASTEL_BLUE;

			if (verdSinkFileNameLabel.Text == "none") {
				verdSinkFileNameLabel.Text = "default";
				Monitor monitor = DependencyService.Get<Monitor> ();
				monitor.putVerdSinkString ("currently not needed");

				// TODO: need to parse the VerdictSink in some capacity to update the charts
				// currently also not using any Boundaries so Boundaries omitted here
				// TODO: currently just hardcoded plots for specifica experiment
				Dictionary<byte, (string x, string y, List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series)> plots = new ();
				List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series0 = new()
				{
					(0, true, GraphColor.BLUE, GraphColor.BLUE),
					(1, true, GraphColor.GREEN, GraphColor.GREEN)
				};
				plots.Add (0, ("vel_acc_x", "vel_acc_y", series0));
				List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series1 = new()
				{
					(0, true, GraphColor.BLUE, GraphColor.BLUE)
				};
				plots.Add (1, ("latitude", "longitude", series1));
				DependencyService.Get<MultipleChartsDataStore> ().Update (plots, new ());
				Debug.WriteLine ("ChartsDatastore updated");

			} else {
				// TODO: what to do with actual configs here (i.e. parsing)
			}
		}

        public async void UpdateButton (object sender, EventArgs e) {
            Monitor mon = DependencyService.Get<Monitor> ();
            if (mon.getVerdSink()) {
                OnAppearing ();
                
            } else {
				await DisplayAlert ("Warning", "The Verdict SInk Config is not set yet.", "OK");
			}
        }
	}
}