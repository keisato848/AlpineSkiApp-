using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
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

        private void Button_Clicked(object sender, EventArgs e)
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
                        var sql = $"INSERT INTO users_table (nickname, mail_address, pass, salt, created_at, updated_at) VALUES('{nickname}', '{mail}', '{pwdHash}', '{salt}', '{time}', '{time}');";
                        DatabaseUtility.ExecuteSqlNonquery(sql,connection);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}