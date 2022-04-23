using PointApp.Services;
using PointApp.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PointApp
{
	public partial class App : Application
	{

		public App()
		{
			//Register Syncfusion license  
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NjIzMDg4QDMyMzAyZTMxMmUzMFJacnh4c2pjZmNwL0cyVlZ0YU9yYUhId2ZPb0hMcUNIUWE5K2NTdUxzWXM9");
			InitializeComponent();

			DependencyService.Register<MockDataStore>();
			MainPage = new AppShell();
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}
	}
}
