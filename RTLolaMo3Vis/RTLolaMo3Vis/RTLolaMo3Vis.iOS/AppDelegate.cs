﻿using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
//using KeyboardOverlap.Forms.Plugin.iOSUnified;
using LocalNotifications.iOS;
using UIKit;
using UserNotifications;

namespace RTLolaMo3Vis.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();

            var notificationSettings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound, null);
            app.RegisterUserNotificationSettings(notificationSettings);

            LoadApplication(new App());
            //KeyboardOverlapRenderer.Init();
            return base.FinishedLaunching(app, options);
        }
    }
}
