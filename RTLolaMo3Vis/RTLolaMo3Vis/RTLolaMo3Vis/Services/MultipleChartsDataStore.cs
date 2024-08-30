using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using RTLolaMo3Vis.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RTLolaMo3Vis.Services
{
    public class MultipleChartsDataStore : IDataStore<DataPointWrapper, ObservableCollection<ISeries>>
    {
        //one item for every chart
        ObservableCollection<ObservableCollection<ISeries>> seriesList = new ObservableCollection<ObservableCollection<ISeries>>();
        List<(double xMax, double xMin, double yMax, double yMin)> limits = new List<(double xMax, double xMin, double yMax, double yMin)>();
		private double newestTimestamp = 0;
        private const double timelimit = 5.0;
        // maps to each plot that has a highlighted series which serie is currently highlighted and what color it had before highlighting
        private Dictionary<int, (int serie, SolidColorPaint originalColor)> highlightMap = new Dictionary<int, (int, SolidColorPaint)>();
        private Dictionary<int, int> boundaryCountMap = new Dictionary<int, int>();
        private List<DataPointWrapper> temporaryStorage = new List<DataPointWrapper> ();
        private DateTime lastUpdate = DateTime.UtcNow;

        public bool changed = false;

        public List<(string x, string y)> plotAchsisNames = new List<(string, string)>();

        public MultipleChartsDataStore(Dictionary<byte, (string x, string y, List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series)> plots, List<(byte plot, List<(double x, double y)> values, GraphColor color)> boundaries)
        {
            // for each chart create one series ObservableCollection, one values Collection per line wanted in the chart, give name from spec
            // example four charts with 1 line each
            foreach (var plot in plots)
            {
                limits.Add((0.0, 0.0, 0.0, 0.0));
                plotAchsisNames.Add((plot.Value.x, plot.Value.y));
                ObservableCollection<ISeries> series = new();
                foreach (var serie in plot.Value.series)
                {
                    ObservableCollection<DataPointWrapper> values = new();
                    if (serie.connected)
                    {
                        series.Add(new LineSeries<DataPointWrapper>
                        {
                            Values = values,
                            Name = serie.ToString(),
							Mapping = (point, index) => new (point.X, point.Y),
							GeometryStroke = colorTranslation(serie.outerColor),
                            GeometryFill = colorTranslation(serie.innerColor),
                            GeometrySize = 7,
                            Fill = null,
                            LineSmoothness = 0,
                            Stroke = colorTranslation(serie.innerColor)
                        });
                    }
                    else
                    {
                        series.Add(new ScatterSeries<DataPointWrapper>
                        {
                            Values = values,
                            //Mapping = (wrapper, point) =>
                            //{
                             //   point.PrimaryValue = (double)wrapper.Y;
                              //  point.SecondaryValue = (double)wrapper.X;
                            //},
                            Name = serie.ToString(),
                            Stroke = colorTranslation(serie.outerColor),
                            Fill = colorTranslation(serie.innerColor),
                            GeometrySize = 7
                        });
                    }
                }
                seriesList.Add(series);
                // initializing list, note this might be the limit, but things will only be deleted if limits changes
                this.limits.Add ((0.0, 0.0, 0.0, 0.0));
            }


            foreach (var area in boundaries)
            {
                var series = seriesList[area.plot];
                ObservableCollection<DataPointWrapper> values = new();
                series.Add(new LineSeries<DataPointWrapper>
                {
                    Values = values,
					Mapping = (point, index) => new (point.X, point.Y),
					Name = (series.Count - 1).ToString(),
                    LineSmoothness = 0,
                    GeometryFill = null,
                    Stroke = colorTranslation(area.color),
                    GeometryStroke = null,
                    Fill = null,
                });

                int count = 0;
                if (boundaryCountMap.ContainsKey(area.plot))
                {
                    count = boundaryCountMap[area.plot];
                    boundaryCountMap.Remove(area.plot);
                }
                boundaryCountMap.Add(area.plot, count + 1);

                foreach (var point in area.values)
                {
                    // added boundaries in temporary items
                    AddItem(new(series.Count - 1, area.plot, point.x, point.y, 0.0, false));
                }
            }
        }

        internal void Update(Dictionary<byte, (string x, string y, List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series)> plots, List<(byte plot, List<(double x, double y)> values, GraphColor color)> boundaries)
        {
            seriesList = new();
            limits = new();
            plotAchsisNames = new();

            foreach (var plot in plots)
            {
                limits.Add((0.0, 0.0, 0.0, 0.0));
                plotAchsisNames.Add((plot.Value.x, plot.Value.y));
                ObservableCollection<ISeries> series = new();
                foreach (var serie in plot.Value.series)
                {
                    ObservableCollection<DataPointWrapper> values = new();
                    if (serie.connected)
                    {
                        series.Add(new LineSeries<DataPointWrapper>
                        {
                            Values = values,
                            Name = serie.ToString(),
                            Mapping = (point, index) => new(point.X, point.Y),
                            GeometryStroke = colorTranslation(serie.outerColor),
                            GeometryFill = colorTranslation(serie.innerColor),
                            GeometrySize = 7,
                            Fill = null,
                            LineSmoothness = 0,
                            Stroke = colorTranslation(serie.innerColor)
                        });
                    }
                    else
                    {
                        series.Add(new ScatterSeries<DataPointWrapper>
                        {
                            Values = values,
                            Name = serie.ToString(),
							Mapping = (point, index) => new (point.X, point.Y),
							Stroke = colorTranslation(serie.outerColor),
                            Fill = colorTranslation(serie.innerColor),
                            GeometrySize = 7
                        });
                    }
                }
                seriesList.Add(series);
				// initializing list, note this might be the limit, but things will only be deleted if limits changes
				this.limits.Add ((0.0, 0.0, 0.0, 0.0));
			}
            Debug.WriteLine("num plots: " + seriesList.Count);


            foreach (var area in boundaries)
            {
                var series = seriesList[area.plot];
                ObservableCollection<DataPointWrapper> values = new();
                series.Add(new LineSeries<DataPointWrapper>
                {
                    Values = values,
                    Name = (series.Count - 1).ToString(),
					Mapping = (point, index) => new (point.X, point.Y),
					LineSmoothness = 0,
                    GeometryFill = null,
                    Stroke = colorTranslation(area.color),
                    GeometryStroke = null,
                    Fill = null,
                });

                foreach (var point in area.values)
                {
                    AddItem(new(series.Count - 1, area.plot, point.x, point.y, 0.0, false));
                }
            }

            changed = true;
        }

        // add items to temporary storage, only transferred to observable collection eriodically
        public void AddItem(DataPointWrapper item)
        {
            if (item.Plot < 0 || item.Plot >= seriesList.Count)
                throw new ArgumentException("Plot out of bounds");
            if (item.Serie < 0 || item.Serie >= seriesList[item.Plot].Count)
                throw new ArgumentException("Serie out of bounds");

            temporaryStorage.Add (item);
            //Debug.WriteLine ("added item to temporary");
            // call AddItemPeriodical periodically to update the charts
            DateTime now = DateTime.UtcNow;
            TimeSpan delaySinceLastUpdate = TimeSpan.FromSeconds (1.5);
            if (now-lastUpdate > delaySinceLastUpdate)
                Task.Run ((Action)AddItemPeriodical);
        }

        // meant to be executed on another thread periodically, add everything from temporary storage to observable colection
        public void AddItemPeriodical () {
            foreach(var item in temporaryStorage) {
				ObservableCollection<DataPointWrapper> values = (ObservableCollection<DataPointWrapper>)seriesList [item.Plot] [item.Serie].Values;
				values.Add (item);
			}
			// Debug.WriteLine ("temporary storage size: " + temporaryStorage.Count);
			temporaryStorage.Clear ();
            lastUpdate = DateTime.UtcNow;
		}

		public void ChangedLimits(double xMax, double xMin, double yMax, double yMin, int plot)
        {
            limits[plot] = (xMax, xMin, yMax, yMin);
            cleanupBothAxis();
        }

		public void ChangedXLimits (double xMax, double xMin, int plot) {
			(double xMaxOld, double xMinOld, double yMaxOld, double yMinOld) currLimits = limits [plot];
			limits [plot] = (xMax, xMin, currLimits.yMaxOld, currLimits.yMinOld);
			cleanupXaxis (plot);
		}

		public void ChangedYLimits (double yMax, double yMin, int plot) {
			(double xMaxOld, double xMinOld, double yMaxOld, double yMinOld) currLimits = limits [plot];
			limits [plot] = (currLimits.xMaxOld, currLimits.xMinOld, yMax, yMin);
			cleanupYaxis (plot);
		}

		public void DeleteAllItems()
        {
            Debug.WriteLine("Deleting all series");
            for (int i = 0; i < seriesList.Count; i++)
            {
                for (int j = 0; j < seriesList[i].Count; j++)
                {
                    ObservableCollection<DataPointWrapper> values = (ObservableCollection<DataPointWrapper>)seriesList[i][j].Values;
                    for (int k = 0; k < values.Count; k++)
                    {
                        if (values[k].Deletable)
                        {
                            values.RemoveAt(k);
                        }
                    }
                }
            }
        }

        public void UpdateHighlight(int plot, int serie, SolidColorPaint color)
        {
            //plot: in which plot to change the highlighted series
            //serie: which serie to highlight now
            //color: the color the highlighted serie should have, may be null, then choose red as default
            //throws IllegalOperation exception if previous highlight is invalid or if new serie cannot be highlighted

            if (color == null)
            {
                color = new SolidColorPaint(SKColors.Red);
            }
            if (highlightMap.ContainsKey(plot))
            {
                int currHighlightedSerie = highlightMap[plot].serie;
                ISeries currSerie = seriesList[plot][currHighlightedSerie];
                if (currSerie != null && currSerie.GetType() == typeof(LineSeries<DataPointWrapper>))
                {
                    LineSeries<DataPointWrapper> highlightedSerie = (LineSeries<DataPointWrapper>)currSerie;
                    highlightedSerie.Stroke = highlightMap[plot].originalColor;
                    highlightMap.Remove(plot);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            ISeries newSerie = seriesList[plot][serie];
            if (newSerie != null && newSerie.GetType() == typeof(LineSeries<DataPointWrapper>))
            {
                LineSeries<DataPointWrapper> toHighlight = (LineSeries<DataPointWrapper>)newSerie;
                SolidColorPaint originalColor = (SolidColorPaint)toHighlight.Stroke;
                toHighlight.Stroke = color;
                highlightMap.Add(plot, (serie, originalColor));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public int GetFirstBoundary(int plot)
        {
            //plot: the plot of which you want to know what index in the series list the first boundary has
            if (!boundaryCountMap.ContainsKey(plot)) throw new ArgumentException();
            return seriesList[plot].Count - boundaryCountMap[plot];
        }

        public IEnumerable<ObservableCollection<ISeries>> GetAllItems()
        {
            return (IEnumerable<ObservableCollection<ISeries>>)seriesList;
        }

        public DataPointWrapper GetItem(byte id)
        {
            throw new NotImplementedException();
        }

        public DataPointWrapper GetItemString(string id)
        {
            throw new NotImplementedException();
        }

        public ISeries<DataPointWrapper> getSeriesByName(string name)
        {
            throw new NotImplementedException();
        }


        private SolidColorPaint colorTranslation(GraphColor color) => color switch
        {
            GraphColor.RED => new SolidColorPaint(SKColor.FromHsl(18, 100, (float) 72)),
            GraphColor.BLUE => new SolidColorPaint(SKColor.FromHsl(178, (float) 41, (float) 63)),
            GraphColor.GREEN => new SolidColorPaint(SKColor.FromHsl(89, (float) 30, (float) 60)),
            GraphColor.YELLOW => new SolidColorPaint(SKColor.FromHsl(61, (float)90, (float) 77)),
            _ => new SolidColorPaint(SKColors.Transparent),
        };

        private void cleanupBothAxis()
        {
            for (int i = 0; i < seriesList.Count; i++)
            {
                (double xMax, double xMin, double yMax, double yMin) currLimits = limits[i];
                for (int j = 0; j < seriesList[i].Count; j++)
                {
                    ObservableCollection<DataPointWrapper> values = (ObservableCollection<DataPointWrapper>)seriesList[i][j].Values;
                    for (int k = 0; k < values.Count; k++)
                    {
                        if (values[k].X < currLimits.xMin || values[k].X > currLimits.xMax || values[k].Y < currLimits.yMin || values[k].Y > currLimits.yMax)
                        {
                            values.RemoveAt(k);
                        }
                    }
                }
            }
        }

		private void cleanupXaxis (int graphId) {
			(double xMax, double xMin, double yMax, double yMin) currLimits = limits [graphId];
			for (int j = 0; j < seriesList [graphId].Count; j++) {
				ObservableCollection<DataPointWrapper> values = (ObservableCollection<DataPointWrapper>)seriesList [graphId] [j].Values;
				for (int k = 0; k < values.Count; k++) {
					if (values [k].X < currLimits.xMin || values [k].X > currLimits.xMax) {
						Debug.WriteLine ("Removing x:" + values [k].X + ", y: " + values [k].Y);
						values.RemoveAt (k);
					}
				}
			}
		}

		private void cleanupYaxis (int graphId) {
			(double xMax, double xMin, double yMax, double yMin) currLimits = limits [graphId];
			for (int j = 0; j < seriesList [graphId].Count; j++) {
				ObservableCollection<DataPointWrapper> values = (ObservableCollection<DataPointWrapper>)seriesList [graphId] [j].Values;
				for (int k = 0; k < values.Count; k++) {
					if (values [k].Y < currLimits.yMin || values [k].Y > currLimits.yMax) {
                        Debug.WriteLine("Removing x:" + values [k].X + ", y: " + values [k].Y );
						values.RemoveAt (k);
					}
				}
			}
		}
	}
}

