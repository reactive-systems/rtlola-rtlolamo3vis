using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace RTLolaMo3Vis.iOS
{
    public class Application
    {
		// extra build argument necessary: -gcc_flags "-L${ProjectDir} -lrtlola_c_ffi -force_load ${ProjectDir}/librtlola_c_ffi.a"

		// This is the main entry point of the application.
		static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
