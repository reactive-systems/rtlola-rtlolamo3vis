using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RTLolaMo3Vis.Models;
using static RTLolaMo3Vis.Models.ConnectionReceiver;

namespace RTLolaMo3Vis.Models
{
    public class Parser
    {
        public Parser()
        {
            specifications = new();
            ids = new();
            boundaries = new();
            plotInformation = new();
			monitorPlotInfo = (new (), new());
		}

        public void parse(Stream file)
        {
            using var sr = new StreamReader(file);
            var json = sr.ReadToEnd();
            var j = JObject.Parse(json);
            IList<JToken> specs = j.ContainsKey("Specs") ? j["Specs"].Children().ToList() : new();

            foreach (var spec in specs)
            {
                var stringId = spec.Value<string>("Id");
                var name = spec.Value<string>("Name");
                if (stringId == null || !Byte.TryParse(stringId, out byte id)
                    || name == null || ids.Contains(id))
                {
                    Debug.WriteLine("Could not read Spec Info");
                    continue;
                }
                Spec specification = new(id, name);
                specifications.Add(specification);
                ids.Add(id);
                parseTrigger(specification, spec);
                parsePlots(specification, spec);
            }

        }

        public void parseWithoutSpecs (Stream file) {
			using var sr = new StreamReader (file);
			var json = sr.ReadToEnd ();
			JObject j = JObject.Parse (json);
            // TODO: currently not parsing triggers
			// parseTriggerWithoutSpecs (j);
            parsePlotsWithoutSpecs (j);
		}

        private void parseTriggerWithoutSpecs (JObject j) {
			var triggers = j.ContainsKey ("Triggers") ? j ["Triggers"].Children ().ToList () : new ();

			foreach (var trigger in triggers) {
				var triggerStringId = trigger.Value<string> ("Id");
				var explanation = trigger.Value<string> ("Explanation");
				var importanceStringId = trigger.Value<string> ("Importance");
				if (explanation == null || !Byte.TryParse (triggerStringId, out byte triggerId)
					|| !Byte.TryParse (importanceStringId, out byte importanceId)) {
					Debug.WriteLine ("Could not read Trigger Info");
					continue;
				}
				var triggerName = trigger.Value<string> ("Name") ?? ("Trigger " + triggerId);
				var importance = (importanceId > 5 || importanceId < 0) ? Importance.NOT_KNOWN : (Importance)importanceId;
                // TODO: normally add to TriggerDefDataStore here, but unnecessary?
			}
		}

        private void parseTrigger(Spec specification, JToken specToken)
        {
            var triggers = (specToken["Triggers"] != null) ? specToken["Triggers"].Children().ToList() : new();
            foreach (var trigger in triggers)
            {
                var triggerStringId = trigger.Value<string>("Id");
                var explanation = trigger.Value<string>("Explanation");
                var importanceStringId = trigger.Value<string>("Importance");
                if (explanation == null || !Byte.TryParse(triggerStringId, out byte triggerId)
                    || !Byte.TryParse(importanceStringId, out byte importanceId)
                    || specification.ExistsTrigger(triggerId))
                {
                    Debug.WriteLine("Could not read Trigger Info");
                    continue;
                }
                var triggerName = trigger.Value<string>("Name") ?? ("Trigger " + triggerId);
                var importance = (importanceId > 5 || importanceId < 0) ? Importance.NOT_KNOWN : (Importance)importanceId;
                specification.Add(new  SpecTrigger(explanation, importance, triggerId, specification));
                specification.AddTriggerInformation(triggerId, triggerName, importance, explanation);
            }
        }

        private void parsePlotsWithoutSpecs(JObject j) {
			var plots = j.ContainsKey ("Plots") ? j ["Plots"].Children ().ToList () : new ();
            

			foreach (var plot in plots) {
				var plotIdString = plot.Value<string> ("Id");

				var x = plot.Value<string> ("X");
				var y = plot.Value<string> ("Y");

				if (x == null || y == null || !Byte.TryParse (plotIdString, out byte plotId)) continue;

				plotInformation.Add (plotId, (x, y, new ()));

                if (plot ["Limit"] != null) {
                    Debug.WriteLine ("limit exists");
                    var lim = plot ["Limit"].ToObject<JObject> ();

                    var limits = lim.Properties ().ToList ();
                    foreach (var limit in limits) {
                        if (limit.Name == "x" || limit.Name=="X") {
							monitorPlotInfo.limitDescriptions.Add (new (plotId, (byte)'x', limit.Value.ToString()));
						}
						if (limit.Name == "y" || limit.Name == "Y") {
							monitorPlotInfo.limitDescriptions.Add (new (plotId, (byte)'y', limit.Value.ToString()));
						}
					}
				}

				var series = (plot ["Series"] != null) ? plot ["Series"].Children ().ToList () : new ();

				byte id = 0;
				int triggercount = 0;

				List<(byte plot, byte serie, GraphColor innerColor, GraphColor outerColor)> triggers = new ();

				foreach (var serie in series) {
					GraphColor color = GraphColor.BLUE;
                    bool connected = false;

					if (serie.Value<String> ("Color") != null) color = getColor (serie.Value<String> ("Color"));

                    // int triggerIndex = -1;

                    GraphColor triggerColor = GraphColor.RED;
					if (serie.Value<String> ("TriggerColor") != null) {
					    triggerColor = getColor (serie.Value<String> ("TriggerColor"));
						// triggers.Add ((plotId, id, triggerColor, color));
						// triggerIndex = series.Count + triggercount;
						// triggercount++;
					}

                    if (serie.Value<String> ("Connected") != null) {
                        var connectedString = serie.Value<string> ("Connected");
                        if (connectedString == "True")
                            connected = true;
                    }

                    if (serie.Value<String> ("Stream") != null ){
                        // put normal plot at even id number
                        // put triggered series at uneven number
                        var streamName = serie.Value<String> ("Stream");
                        if (connected) {
                            plotInformation [plotId].series.Add ((id, true, color, color));
                        } else {
                            plotInformation [plotId].series.Add ((id, false, color, GraphColor.NONE));
                        }
						plotInformation [plotId].series.Add ((id, false, triggerColor, GraphColor.NONE));
						monitorPlotInfo.graphLineDescriptions.Add (new(plotId, id, streamName));
						id++;
                    }
				}

                // TODO: no triggers parsed currently
				// foreach (var trigger in triggers) {
					// plotInformation [trigger.plot].series.Add ((id, false, trigger.innerColor, trigger.outerColor));
					// id++;
				// }

				var boundariesList = (plot ["Boundaries"] != null) ? plot ["Boundaries"].Children ().ToList () : new ();

				foreach (var area in boundariesList) {
					GraphColor color = GraphColor.RED;

					if (area.Value<String> ("Color") != null) color = getColor (area.Value<String> ("Color"));

					var values = (area ["Values"] != null) ? area ["Values"].Children ().ToList () : new ();

					List<(double x, double y)> coordinates = new ();

					foreach (var point in values) {
						var xCoordinate = point.Value<string> ("X");
						var yCoordinate = point.Value<string> ("Y");

						if (x == null || y == null || !Double.TryParse (xCoordinate, out double XCoordinate) || !Double.TryParse (yCoordinate, out double YCoordinate)) continue;

						coordinates.Add ((XCoordinate, YCoordinate));
					}

					boundaries.Add ((plotId, coordinates, color));
				}
			}
		}

        private void parsePlots(Spec specification, JToken specToken)
        {
            var plots = (specToken["Plots"] != null) ? specToken["Plots"].Children().ToList() : new();
            foreach (var plot in plots)
            {
                var plotIdString = plot.Value<string>("Id");

                var x = plot.Value<string>("X");
                var y = plot.Value<string>("Y");

                if (x == null || y == null || !Byte.TryParse(plotIdString, out byte plotId) || specification.ExitsPlot(plotId)) continue;

                specification.AddPlot(plotId, x, y);

                plotInformation.Add(plotId, (x, y, new()));

                var series = (plot["Series"] != null) ? plot["Series"].Children().ToList() : new();

                byte id = 0;
                int triggercount = 0;

                List<(byte plot, byte serie, GraphColor innerColor, GraphColor outerColor)> triggers = new();

                foreach (var serie in series)
                {
                    GraphColor color = GraphColor.BLUE;

                    if (serie.Value<String>("Color") != null) color = getColor(serie.Value<String>("Color"));

                    int triggerIndex = -1;

                    if (serie.Value<String>("TriggerColor") != null)
                    {
                        GraphColor triggerColor = getColor(serie.Value<String>("TriggerColor"));
                        triggers.Add((plotId, id, triggerColor, color));
                        triggerIndex = series.Count + triggercount;
                        triggercount++;
                    }
                    if (!specification.AddSerie(plotId, id, triggerIndex))
                    {
                        Debug.WriteLine("Could not read Serie Info");
                        break;
                    }
                    if (serie.Value<String>("Connected") != null)
                    {
                        /*
                        if (!Boolean.TryParse(serie.Value<String>("Connected"), out bool Connected))
                        {
                            Debug.WriteLine("Could not read Serie info at Connected");
                            //IllegalArgumentExcepetion werfen?
                            break;
                        } */
                        var connected = serie.Value<string>("Connected");
                        //Boolean.TryParse(connected, out bool Connected);
                        if (connected == "True")
                            plotInformation[plotId].series.Add((id, true, color, color));
                        else plotInformation[plotId].series.Add((id, false, color, GraphColor.NONE));
                        id++;
                    }
                    else
                    {
                        plotInformation[plotId].series.Add((id, false, color, GraphColor.NONE));
                        id++;
                    }
                }

                foreach (var trigger in triggers)
                {
                    plotInformation[trigger.plot].series.Add((id, false, trigger.innerColor, trigger.outerColor));
                    id++;

                }

                var boundariesList = (plot["Boundaries"] != null) ? plot["Boundaries"].Children().ToList() : new();

                foreach (var area in boundariesList)
                {
                    GraphColor color = GraphColor.RED;

                    if (area.Value<String>("Color") != null) color = getColor(area.Value<String>("Color"));

                    var values = (area["Values"] != null) ? area["Values"].Children().ToList() : new();

                    List<(double x, double y)> coordinates = new();

                    foreach (var point in values)
                    {
                        var xCoordinate = point.Value<string>("X");
                        var yCoordinate = point.Value<string>("Y");

                        if (x == null || y == null || !Double.TryParse(xCoordinate, out double XCoordinate) || !Double.TryParse(yCoordinate, out double YCoordinate)) continue;

                        coordinates.Add((XCoordinate, YCoordinate));
                    }

                    boundaries.Add((plotId, coordinates, color));
                }
            }
        }

        private List<Spec> specifications;
        private List<int> ids;

        private readonly Dictionary<byte, (string x, string y, List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series)> plotInformation;

        public Dictionary<byte, (string x, string y, List<(byte id, bool connected, GraphColor innerColor, GraphColor outerColor)> series)> Plots => plotInformation;

        private readonly (List<SafeGraphLineDescription> graphLineDescriptions, List<SafeLimitDescription> limitDescriptions) monitorPlotInfo;

		public (List<SafeGraphLineDescription> graphLineDescriptions, List<SafeLimitDescription> limitDescriptions) MonitorPlotInfo => monitorPlotInfo;

        private readonly List<(byte plot, List<(double x, double y)> values, GraphColor color)> boundaries;

        public List<(byte plot, List<(double x, double y)> values, GraphColor color)> Boundaries => boundaries;

        public List<Spec> Specifications => specifications;

        private GraphColor getColor(string name) => name switch
        {
            "Red" => GraphColor.RED,
            "Blue" => GraphColor.BLUE,
            "Green" => GraphColor.GREEN,
            "Yellow" => GraphColor.YELLOW,
            _ => GraphColor.NONE
        };
    }

    public enum GraphColor
    {
        RED,
        BLUE,
        GREEN,
        YELLOW,
        NONE
    }

    public enum GraphType
    {
        DOTS,
        LINE
    }
}

