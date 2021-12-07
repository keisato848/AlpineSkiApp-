using System;
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
                string mail = Entry_Mail.Text;
                string inputPwd = Entry_Pwd.Text;
                string id = null;
                string pass = null;
                string salt = null;
                string pwdHash = null;

                if (string.IsNullOrWhiteSpace(mail))
                {
                    await DisplayAlert("通知", "メールアドレスを入力してください。", "OK");
                    return;
                }
                else if(string.IsNullOrWhiteSpace(inputPwd))
                {
                    await DisplayAlert("通知", "パスワードを入力してください。", "OK");
                    return;
                }

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
                                id = reader.GetValue(0).ToString();
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
                        await DisplayAlert("通知", "ログインしました。", "OK");
                        await Shell.Current.GoToAsync("//CalcPoint");
                    }
                    else
                    {
                        await DisplayAlert("エラー", "パスワードが一致しませんでした。\nメールアドレスとパスワードを確認してください。", "OK");
                        return;
                    }
                }
                else
                {
                    await DisplayAlert("エラー", "メールアドレスは登録されていません。", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("エラー", "予期せぬエラーが発生しました。", "OK");
            }
        }
    }
}