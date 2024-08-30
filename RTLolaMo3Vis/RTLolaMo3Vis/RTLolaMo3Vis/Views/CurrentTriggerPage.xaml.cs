using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace RTLolaMo3Vis.Views
{
    public partial class CurrentTriggerPage : ContentPage
    {
        public CurrentTriggerPage()
        {
            InitializeComponent();
        }

        private double width = 0;
        private double height = 0;
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width != this.width || height != this.height)
            {
                this.width = width;
                this.height = height;
                if (width > height)
                {
                    outerStack.Orientation = StackOrientation.Horizontal;
                }
                else
                {
                    outerStack.Orientation = StackOrientation.Vertical;
                }
            }
        }
    }
}
