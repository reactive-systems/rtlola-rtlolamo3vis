using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace RTLolaMo3Vis.Models
{
    public class DataPointWrapper
    {
        public int Serie { get; set; }
        public int Plot { get; set; }
        public double Timestamp { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public bool Deletable { get; set; }

        public DataPointWrapper(int serie, int plot, double x, double y, double timestamp, bool deletable)
        {
            this.Serie = serie;
            this.Plot = plot;
            this.Timestamp = timestamp;
            this.X = x;
            this.Y = y;
            this.Deletable = deletable;
        }
    }
}
