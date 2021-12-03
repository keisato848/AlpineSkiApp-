using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PointApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SignUpPage : ContentPage
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private async void TapGesture_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                string nickname = Entry_Nickname.Text;
                string mail     = Entry_Mail.Text;
                string pwd      = Entry_Pwd.Text;
                string salt     = DatabaseUtility.GetSalt();
                string pwdHash  = DatabaseUtility.GetSHA256HashString(pwd, salt);

                using (var connection = DatabaseUtility.ConnectDataBase())
                {
                    connection.Open();
                    var time = DateTime.Now.ToString().Replace('/', '-');
                    using (var transaction = connection.BeginTransaction())
                    {
                        var sql = $"INSERT INTO users_table (nickname, mail_address, pass, salt, created_at, updated_at) VALUES('{nickname}', '{mail}', '{pwdHash}', '{salt}', '{time}', '{time}') RETURNING id;";
                        var result = DatabaseUtility.ExecuteScalar (sql,connection);
                        transaction.Commit();
                        LoginSuccess(result.ToString());
                    }
                }
            }
            catch (Exception ex) when (ex is Npgsql.PostgresException postgresEx)
            {
                if (postgresEx.SqlState == "23505")
                {
                    if (postgresEx.ConstraintName == "users_table_nickname_key")
                    {
                        await DisplayAlert("通知", "ニックネームはすでに使用されています。", "OK");
                    }
                    if (postgresEx.ConstraintName == "users_table_mail_address_key")
                    {
                        await DisplayAlert("通知", "メールアドレスはすでに使用されています。", "OK");
                    }
                }
            }
        }

        private async void LoginSuccess(string id)
        {
            Application.Current.Resources.Add("LoginUserId", id);
            await Shell.Current.GoToAsync("//CalcPoint");
            await DisplayAlert("通知", "登録が完了しました。\nログインしました。", "OK");
        }

        async void TapGestureRecognizer_Tapped(System.Object sender, System.EventArgs e)
        {
            await Browser.OpenAsync("https://docs.google.com/document/d/1rIGx2V-0hf7RoVFvrNmp-nVVBQqF0eMBwirXn07d2F8/edit?usp=sharing");
        }
    }
}