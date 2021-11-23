﻿using Xamarin.Essentials;
using System.Windows.Input;
using Xamarin.Forms;

namespace PointApp.Views
{
    public class ContactPage : ContentPage
    {
        public  ContactPage()
        {
            OpenWeb();
        }
        private async void OpenWeb()
        {
            await Browser.OpenAsync("https://forms.gle/w8AN33mY2B3dLrW27");
            await Shell.Current.GoToAsync("//CalcPoint");
        }
        public ICommand OpenWebCommand { get; }
    }
}