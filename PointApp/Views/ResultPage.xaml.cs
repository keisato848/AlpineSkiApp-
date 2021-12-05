using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PointApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ResultPage : ContentPage
    {
        public ResultPage(string fisPoint, string sajPoint, string userFisPoint = "", string userSajPoint = "")
        {
            InitializeComponent();
            Label_FisPoint.Text = fisPoint;
            Label_SajPoint.Text = sajPoint;
            if (!string.IsNullOrEmpty(userFisPoint) && !string.IsNullOrEmpty(userSajPoint))
            {
                Label_UsersFisPoint.Text = userFisPoint;
                Label_UsersSajPoint.Text = userSajPoint;
                Gird_UsersPoint.IsVisible = true;
            }
        }

        private async void DismissButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}