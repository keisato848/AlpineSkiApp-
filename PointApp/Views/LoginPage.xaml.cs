using PointApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PointApp.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage
	{
		public LoginPage()
		{
			InitializeComponent();
		}

        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
			await Shell.Current.GoToAsync("//SignUpPage");
		}

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                string mail     = Entry_Mail.Text;
                string inputPwd = Entry_Pwd.Text;
                string id       = null;
                string pass     = null;
                string salt     = null;
                string pwdHash  = null;
                using (var connection = DatabaseUtility.ConnectDataBase())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var sql = $"SELECT id, pass, salt FROM users_table WHERE mail_address = '{mail}'";
                        using (var reader = DatabaseUtility.ExecuteSql(sql, connection))
                        {
                            if (reader.Read())
                            {
                                id   = reader.GetValue(0).ToString();
                                pass = reader.GetValue(1).ToString();
                                salt = reader.GetValue(2).ToString();
                            }
                        }
                        transaction.Commit();
                    }
                }
                if (!string.IsNullOrEmpty(pass) && !string.IsNullOrEmpty(salt))
                {
                    pwdHash = DatabaseUtility.GetSHA256HashString(inputPwd, salt);
                    if (!string.IsNullOrEmpty(pwdHash) && pass.Equals(pwdHash))
                    {
                        Application.Current.Resources.Add("LoginUserId", id);
                        await Shell.Current.GoToAsync("//CalcPoint");
                        await DisplayAlert("通知", "ログインしました。", "OK");
                    }
                    else
                    {
                        await DisplayAlert("エラー", "パスワードが一致しませんでした。\nメールアドレスとパスワードを確認してください。", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("エラー", "メールアドレスは登録されていません。", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}