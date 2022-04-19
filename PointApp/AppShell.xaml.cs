using PointApp.Views;
using System;
using Xamarin.Forms;

namespace PointApp
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private void UpdateFlyItemVisibility()
        {
            //FlyItem_Login.IsVisible = !Application.Current.Resources.TryGetValue("LoginUserId", out _);
            //FlyItem_Signup.IsVisible = !Application.Current.Resources.TryGetValue("LoginUserId", out _);
        }

        private void FlyItem_Login_Disappearing(object sender, EventArgs e)
        {
            UpdateFlyItemVisibility();
        }

        private void FlyItem_Signup_Disappearing(object sender, EventArgs e)
        {
            UpdateFlyItemVisibility();
        }
    }
}