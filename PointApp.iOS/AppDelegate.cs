using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using Google.MobileAds;
using MarcTron;
using MarcTron.Plugin;
using Xamarin;
using Xamarin.Forms;
using AppTrackingTransparency;

namespace PointApp.iOS
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
            LoadApplication(new App());
            
            MobileAds.SharedInstance.Start(CompletionHandler);
            return base.FinishedLaunching(app, options);
        }
        private void CompletionHandler(InitializationStatus status) { }

        /// <summary>
        /// アプリが再びアクティブになった場合
        /// </summary>
        /// <param name="uiApplication"></param>
        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);

            //IDFAの使用要求
            //iOS15 以降ではOnActivatedイベントで実行しないとダイアログが表示されない
            this.RequestIDFA();
        }

        /// <summary>
        /// IDFAの使用要求
        /// </summary>
        private void RequestIDFA()
        {
            ATTrackingManager.RequestTrackingAuthorization((status) =>
            {
                // Tracking authorization completed. Start loading ads here.
                // loadAd();
            });
        }
    }
}
