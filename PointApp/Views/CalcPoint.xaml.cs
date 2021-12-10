using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using MarcTron.Plugin;
using System.Threading;

namespace PointApp.Views
{
    public partial class CalcPoint : ContentPage
    {
        private List<Player> m_allPlayers;

        private readonly ObservableCollection<Player> m_finishDefPlayers;

        private readonly ObservableCollection<Player> m_startDefPlayers;

        private readonly Tournament m_tournament;

        public CalcPoint()
        {
            InitializeComponent();

            m_tournament = new Tournament();
            m_startDefPlayers = new ObservableCollection<Player>();
            m_finishDefPlayers = new ObservableCollection<Player>();
            m_allPlayers = new List<Player>();
            SetUp();
        }

        private enum ErrorCode
        {
            UserDuplicated = 0,
            OverUserCount = 1,
            LackStartUser = 2,
            LackFinishUser = 3,
            CalcError = 10,
            IdEmpty = 20,
            PwdEmpty = 21,
            TournamentNameEmpty = 22,
        }

        public enum SexType
        {
            Men = 0,
            Women = 1,
        }

        private enum FValue
        {
            SL = 730,
            GS = 1010,
            SG = 1190,
            DH = 1250,
            AC = 1360
        }

        private enum MaximumPoint
        {
            SL = 165,
            GS = 220,
            SG = 270,
            AC = 270,
            DH = 330
        }

        private enum ViewCellRowStyle
        { Height = 49 }

        private void UpdateControl()
        {
            StartTopList.IsVisible = m_startDefPlayers.Count > 0;
        }

        private void AllList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            Player selectedPlayer;
            if (StartAllList.SelectedItem != null)
            {
                if (m_startDefPlayers.Count == 5)
                {
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                    return;
                }
                selectedPlayer = StartAllList.SelectedItem as Player;
                if (selectedPlayer != null && m_startDefPlayers.Count(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)) > 0)
                {
                    DisplayErrorMessage(ErrorCode.UserDuplicated);
                    return;
                }
                var copyPlayer = selectedPlayer.DeepCopy();
                m_startDefPlayers.Add(copyPlayer);
                SetTopListViewLayout(m_startDefPlayers, StartTopList, StartPlayerEntry);
                StartTopList.ScrollTo(copyPlayer, ScrollToPosition.Center, true);
            }
            else if (FinishAllList.SelectedItem != null)
            {
                if (m_finishDefPlayers.Count == 10)
                {
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                }
                selectedPlayer = FinishAllList.SelectedItem as Player;
                if (selectedPlayer != null && m_finishDefPlayers.Count(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)) > 0)
                {
                    DisplayErrorMessage(ErrorCode.UserDuplicated);
                    return;
                }
                var copyPlayer = selectedPlayer.DeepCopy();
                m_finishDefPlayers.Add(copyPlayer);
                SetTopListViewLayout(m_finishDefPlayers, FinishTopList, FinishPlayerEntry);
                FinishTopList.ScrollTo(copyPlayer, ScrollToPosition.Center, true);
            }
        }

        private async void Switch_Share_Toggled(object sender, ToggledEventArgs e)
        {
            if (sender is Switch switchToggle)
            {
                Application.Current.Resources.TryGetValue("LoginUserId", out object loginUserId);
                if (loginUserId != null)
                {
                    PopupLayout_Share.IsVisible = switchToggle.IsToggled;
                }
                else
                {
                    switchToggle.IsToggled = false;
                    await DisplayAlert("通知", "共有にはログインが必要です。\nログインページに移動します。", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
        }

        private async void Btn_Calc_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (m_startDefPlayers.Count() < 5)
                {
                    DisplayErrorMessage(ErrorCode.LackStartUser);
                    return;
                }
                if (m_finishDefPlayers.Count() < 5)
                {
                    DisplayErrorMessage(ErrorCode.LackFinishUser);
                    return;
                }
                if (Switch_Share.IsToggled && string.IsNullOrEmpty(Entry_TournamentName.Text))
                {
                    DisplayErrorMessage(ErrorCode.TournamentNameEmpty);
                }
                (var fisPenalty, var sajPenalty) = GetPenaltyPoints();
                if (fisPenalty != null && sajPenalty != null)
                {
                    string tournamentName = Entry_TournamentName.Text;
                    string fisPoint = fisPenalty.ToString();
                    string sajPoint = sajPenalty.ToString();
                    string userFisPoint = null;
                    string userSajPoint = null;
                    var winner = m_finishDefPlayers.OrderBy(player => player.Time).First();
                    if (Switch_Share.IsToggled)
                    {
                        var perSecRacePoint = GetRacePoint(new Player { Time = winner.Time + 1.0 }, winner);
                        RegisterTournament(tournamentName, fisPoint, sajPoint, perSecRacePoint.ToString());
                    }
                    if (!string.IsNullOrWhiteSpace(Entry_TargetTime.Text))
                    {
                        var racePoint = GetRacePoint(new Player { Time = double.Parse(Entry_TargetTime.Text) }, winner);
                        if (!double.IsNaN(racePoint))
                        {
                            userFisPoint = (double.Parse(fisPoint) + racePoint).ToString();
                            userSajPoint = (double.Parse(sajPoint) + racePoint).ToString();
                        }
                    }
                    var resultPage = new ResultPage(fisPoint, sajPoint, userFisPoint, userSajPoint);
                    await Navigation.PushModalAsync(resultPage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        private void RegisterTournament(string tournament_name, string fis_penalty, string saj_penalty, string per_sec_race_point)
        {
            try
            {
                string user_id = Application.Current.Resources["LoginUserId"].ToString();
                if (!string.IsNullOrEmpty(user_id))
                {
                    using (var connection = DatabaseUtility.ConnectDataBase())
                    {
                        connection.Open();
                        var time = DateTime.Now.ToString().Replace('/', '-');
                        using (var transaction = connection.BeginTransaction())
                        {
                            var sql = $"INSERT INTO tournaments_table (tournament_name, fis_penarty, saj_penarty, per_sec_race_point, user_id, created_at, updated_at) VALUES('{tournament_name}', '{fis_penalty}', '{saj_penalty}', '{per_sec_race_point}', '{user_id}', '{time}', '{time}');";
                            DatabaseUtility.ExecuteSqlNonquery(sql, connection);
                            transaction.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private double? CalcPenaltyPoint(double sumFinishFivePenaltyPoint, double sumStartFivePenaltyPoint, double sumFinishFiveRacePoint)
        {
            var penaltyPoint = Math.Round((Convert.ToInt32(sumFinishFivePenaltyPoint * 100) + Convert.ToInt32(sumStartFivePenaltyPoint * 100) - Convert.ToInt32(sumFinishFiveRacePoint * 100)) * 0.001, 2, MidpointRounding.AwayFromZero);
            switch (m_tournament.Types)
            {
                case Tournament.EventTypes.SG:
                    if (penaltyPoint > (double)MaximumPoint.SG) { penaltyPoint = (double)MaximumPoint.SG; }
                    break;

                case Tournament.EventTypes.GS:
                    if (penaltyPoint > (double)MaximumPoint.GS) { penaltyPoint = (double)MaximumPoint.GS; }
                    break;

                case Tournament.EventTypes.SL:
                    if (penaltyPoint > (double)MaximumPoint.SL) { penaltyPoint = (double)MaximumPoint.SL; }
                    break;
            }
            return penaltyPoint;
        }

        private string ConvKanaFromHiragana(string str)
        {
            StringBuilder sb = new StringBuilder();
            char[] target = str.ToCharArray();
            char c;
            for (int i = 0; i < target.Length; i++)
            {
                c = target[i];
                if (c >= 'ぁ' && c <= 'ん')
                {
                    c = (char)(c - 'ぁ' + 'ァ');  //-> 変換
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private async void DisplayErrorMessage(ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.UserDuplicated:
                    await DisplayAlert("通知", "選手が重複しています。", "OK");
                    break;

                case ErrorCode.OverUserCount:
                    await DisplayAlert("通知", "これ以上選択できません。", "OK");
                    break;

                case ErrorCode.LackStartUser:
                    await DisplayAlert("通知", "項番３は５人選択してください。", "OK");
                    break;

                case ErrorCode.LackFinishUser:
                    await DisplayAlert("通知", "項番４は５人以上選択してください。", "OK");
                    break;

                case ErrorCode.CalcError:
                    await DisplayAlert("通知", "管理者に報告してください。", "OK");
                    break;

                case ErrorCode.IdEmpty:
                    await DisplayAlert("通知", "IDを入力してください。", "OK");
                    break;

                case ErrorCode.PwdEmpty:
                    await DisplayAlert("通知", "パスワードを入力してください。", "OK");
                    break;

                case ErrorCode.TournamentNameEmpty:
                    await DisplayAlert("通知", "大会名を入力してください", "OK");
                    break;
            }
        }

        private void EventSex_RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton?.ClassId != null)
            {
                m_tournament.Sex = (SexType)Enum.ToObject(typeof(SexType), Convert.ToInt32(radioButton.ClassId));
                SetUp();
            }
        }

        private (double?, double?) GetPenaltyPoints()
        {
            //   A + B - C
            // （A)上位10名の中のポイントトップ5の所持ポイントの合計
            // （B)スタート時のトップ5の所持ポイント合計
            //  (C)上位10名の中のポイントトップ5 のレースポイントの合計　/ 10

            if (m_tournament.Types == Tournament.EventTypes.NONE || m_startDefPlayers.Count != 5 || m_finishDefPlayers.Count == 10)
            {
                return (null, null);
            }

            double fisA, sajA, fisB, sajB, fisC, sajC;
            IEnumerable<Player> fisFinishTopfive = Enumerable.Empty<Player>();
            IEnumerable<Player> sajFinishTopfive = Enumerable.Empty<Player>();
            IEnumerable<Player> fisStartTopfive = Enumerable.Empty<Player>();
            IEnumerable<Player> sajStartTopfive = Enumerable.Empty<Player>();

            switch (m_tournament.Types)
            {
                case Tournament.EventTypes.SG:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisSg select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajSg select player).Take(5);
                    fisStartTopfive = (from player in m_startDefPlayers orderby player.FisSg select player).Take(5);
                    sajStartTopfive = (from player in m_startDefPlayers orderby player.SajSg select player).Take(5);
                    break;

                case Tournament.EventTypes.GS:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisGs select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajGs select player).Take(5);
                    fisStartTopfive = (from player in m_startDefPlayers orderby player.FisGs select player).Take(5);
                    sajStartTopfive = (from player in m_startDefPlayers orderby player.SajGs select player).Take(5);
                    break;

                case Tournament.EventTypes.SL:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisSl select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajSl select player).Take(5);
                    fisStartTopfive = (from player in m_startDefPlayers orderby player.FisSl select player).Take(5);
                    sajStartTopfive = (from player in m_startDefPlayers orderby player.SajSl select player).Take(5);
                    break;
            }

            var winner = m_finishDefPlayers.OrderBy(player => player.Time).First();

            (fisA, sajA) = SumPoints(fisFinishTopfive);
            (fisB, sajB) = SumPoints(fisStartTopfive);

            fisC = SumRacePoint(fisFinishTopfive, winner);
            sajC = SumRacePoint(sajFinishTopfive, winner);

            if (double.IsNaN(fisA) && double.IsNaN(fisB) && double.IsNaN(fisC) && double.IsNaN(sajA) && double.IsNaN(sajB) && double.IsNaN(sajC))
            {
                return (null, null);
            }

            return (CalcPenaltyPoint(fisA, fisB, fisC), CalcPenaltyPoint(sajA, sajB, sajC));
        }

        private ObservableCollection<Player> GetPlayersFromKana(string kana)
        {
            return new ObservableCollection<Player>(from player in m_allPlayers
                                                    where player.KanaName.StartsWith(kana)
                                                    orderby player.FisGs
                                                    select player);
        }

        private ObservableCollection<Player> GetPlayersFromKanji(string kanji)
        {
            return new ObservableCollection<Player>(from player in m_allPlayers
                                                    where player.JapaneseName.StartsWith(kanji)
                                                    select player);
        }

        private (double?, double?) GetPoints(Player player)
        {
            if (m_tournament.Types != Tournament.EventTypes.NONE)
            {
                switch (m_tournament.Types)
                {
                    case Tournament.EventTypes.DH:
                        return (player.FisDh, player.SajDh);

                    case Tournament.EventTypes.SG:
                        return (player.FisSg, player.SajSg);

                    case Tournament.EventTypes.GS:
                        return (player.FisGs, player.SajGs);

                    case Tournament.EventTypes.SL:
                        return (player.FisSl, player.SajSl);

                    default:
                        return (null, null);
                }
            }
            return (null, null);
        }

        private double GetRacePoint(Player targetPlayer, Player winner)
        {
            // Ｐ＝（Ｆ値×当該選手のタイム）÷ラップライム －Ｆ値 またはＰ＝（当該選手のタイム÷ラップタイム－１）×Ｆ値
            int fValue = 0;
            switch (m_tournament.Types)
            {
                case Tournament.EventTypes.SG:
                    fValue = (int)FValue.SG;
                    break;

                case Tournament.EventTypes.GS:
                    fValue = (int)FValue.GS;
                    break;

                case Tournament.EventTypes.SL:
                    fValue = (int)FValue.SL;
                    break;

                default:
                    break;
            }
            return ((fValue * 100 * Convert.ToInt32(targetPlayer.Time * 100) / Convert.ToInt32(winner.Time * 100)) - fValue * 100) * 0.01;
        }

        private bool IsHiragana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsHiragana}*$");
        }

        private bool IsKatakana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsKatakana}*$");
        }

        private async void PlayerEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var matchedPlayers = new ObservableCollection<Player>();
            ListView allList = null;

            if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
            {
                string str = entry.Text;

                if (IsHiragana(str))
                {
                    var kana = ConvKanaFromHiragana(entry.Text);
                    if (kana != null)
                    {
                        matchedPlayers = GetPlayersFromKana(kana);
                    }
                }
                else if (IsKatakana(str))
                {
                    matchedPlayers = GetPlayersFromKana(str);
                }
                else
                {
                    matchedPlayers = GetPlayersFromKanji(str);
                }
            }

            if (sender == StartPlayerEntry) allList = StartAllList;
            if (sender == FinishPlayerEntry) allList = FinishAllList;
            if (matchedPlayers.Count > 0)
            {
                allList.ItemsSource = matchedPlayers;
                allList.IsVisible = true;
                await ScrollView_Main.ScrollToAsync(allList.X, allList.Y, false);
            }
            else
            {
                allList.IsVisible = false;
            }
            allList.SelectedItem = null;
        }

        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton checkedButton)
            {
                if (checkedButton.Content is string checkedValue)
                {
                    switch (checkedValue)
                    {
                        case "ＤＨ":
                            m_tournament.Types = Tournament.EventTypes.DH;
                            break;

                        case "ＳＧ":
                            m_tournament.Types = Tournament.EventTypes.SG;
                            break;

                        case "ＧＳ":
                            m_tournament.Types = Tournament.EventTypes.GS;
                            break;

                        case "ＳＬ":
                            m_tournament.Types = Tournament.EventTypes.SL;
                            break;
                    }
                }
            }
        }

        private string ReadPointList(SexType sex)
        {
            string strJson = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonResource = assembly.GetManifestResourceNames();
                if (jsonResource != null)
                {
                    var file = m_tournament.Sex == SexType.Men
                        ? assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.PointList_M.json")))
                        : assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.PointList_L.json")));
                    if (file != null)
                    {
                        using (var sr = new StreamReader(file))
                        {
                            strJson = sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return strJson;
        }

        private void RemoveEmpty(List<Player> listPlayer)
        {
            var listPlayerTemp = new List<Player>(listPlayer);
            if (listPlayerTemp == null)
            {
                return;
            }

            foreach (var player in listPlayerTemp)
            {
                player.JapaneseName = Regex.Replace(player.JapaneseName, @"\s", "");
            }
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var player = button.BindingContext as Player;
            if (m_startDefPlayers.Contains(player))
            {
                m_startDefPlayers.Remove(player);
                SetTopListViewLayout(m_startDefPlayers, StartTopList, StartPlayerEntry);
            }
            else
            {
                m_finishDefPlayers.Remove(player);
                SetTopListViewLayout(m_finishDefPlayers, FinishTopList, FinishPlayerEntry);
            }
        }

        private void SetTopListViewLayout(ObservableCollection<Player> players, ListView listView, Entry entry)
        {
            listView.IsVisible = players.Count > 0;
            listView.ItemsSource = players;
            listView.HeightRequest = players.Count * (int)ViewCellRowStyle.Height;
            listView.SelectedItem = null;
            entry.Text = string.Empty;
            if (entry.Equals(StartPlayerEntry))
            {
                StartPlayerEntry.IsVisible = m_startDefPlayers.Count() < 5;
            }
            else
            {
                FinishPlayerEntry.IsVisible = m_finishDefPlayers.Count() < 10;
            }
        }

        private void SetUp()
        {
            m_startDefPlayers.Clear();
            m_finishDefPlayers.Clear();
            StartTopList.HeightRequest = 0;
            FinishTopList.HeightRequest = 0;
            var strJson = ReadPointList(m_tournament.Sex);
            if (strJson is string)
            {
                try
                {
                    var listPlayers = JsonSerializer.Deserialize<List<Player>>(strJson);
                    if (listPlayers is List<Player>)
                    {
                        RemoveEmpty(listPlayers);
                        m_allPlayers = listPlayers;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }
        }

        private (double, double) SumPoints(IEnumerable<Player> listPlayer)
        {
            int sumFisPoint = 0;
            int sumSajPoint = 0;
            foreach (var player in listPlayer)
            {
                var points = GetPoints(player);
                if (points.Item1 != null && points.Item2 != null)
                {
                    sumFisPoint += Convert.ToInt32(points.Item1 * 100);
                    sumSajPoint += Convert.ToInt32(points.Item2 * 100);
                }
            }
            return (sumFisPoint * 0.01, sumSajPoint * 0.01);
        }

        private double SumRacePoint(IEnumerable<Player> listPlayer, Player winner)
        {
            double sumRacePoint = 0;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoint(player, winner);
            }
            return sumRacePoint;
        }

        public class Player
        {
            public double FisDh
            {
                get
                {
                    double dValue = 330.00;
                    if (!string.IsNullOrWhiteSpace(StrFisDh))
                    {
                        string strValue = StrFisDh.Insert(StrFisDh.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double FisGs
            {
                get
                {
                    double dValue = 220.00;
                    if (!string.IsNullOrWhiteSpace(StrFisGs))
                    {
                        string strValue = StrFisGs.Insert(StrFisGs.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double FisSc
            {
                get
                {
                    double dValue = 270.00;
                    if (!string.IsNullOrWhiteSpace(StrFisSc))
                    {
                        string strValue = StrFisSc.Insert(StrFisSc.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double FisSg
            {
                get
                {
                    double dValue = 270.00;
                    if (!string.IsNullOrWhiteSpace(StrFisSg))
                    {
                        string strValue = StrFisSg.Insert(StrFisSg.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double FisSl
            {
                get
                {
                    double dValue = 165.00;
                    if (!string.IsNullOrWhiteSpace(StrFisSl))
                    {
                        string strValue = StrFisSl.Insert(StrFisSl.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string JapaneseName { get; set; } = string.Empty;

            public string KanaName { get; set; } = string.Empty;

            public double SajDh
            {
                get
                {
                    double dValue = 330.00;
                    if (!string.IsNullOrWhiteSpace(StrSajDh))
                    {
                        string strValue = StrSajDh.Insert(StrSajDh.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double SajGs
            {
                get
                {
                    double dValue = 220.00;
                    if (!string.IsNullOrWhiteSpace(StrSajGs))
                    {
                        string strValue = StrSajGs.Insert(StrSajGs.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double SajSc
            {
                get
                {
                    double dValue = 270.00;
                    if (!string.IsNullOrWhiteSpace(StrSajSc))
                    {
                        string strValue = StrSajSc.Insert(StrSajSc.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double SajSg
            {
                get
                {
                    double dValue = 270.00;
                    if (!string.IsNullOrWhiteSpace(StrSajSg))
                    {
                        string strValue = StrSajSg.Insert(StrSajSg.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public double SajSl
            {
                get
                {
                    double dValue = 165.00;
                    if (!string.IsNullOrWhiteSpace(StrSajSl))
                    {
                        string strValue = StrSajSl.Insert(StrSajSl.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string StrFisDh { get; set; } = string.Empty;
            public string StrFisSl { get; set; } = string.Empty;

            public string StrFisGs { get; set; } = string.Empty;

            public string StrFisSc { get; set; } = string.Empty;

            public string StrFisSg { get; set; } = string.Empty;


            public string StrSajDh { get; set; } = string.Empty;

            public string StrSajGs { get; set; } = string.Empty;

            public string StrSajSc { get; set; } = string.Empty;

            public string StrSajSg { get; set; } = string.Empty;

            public string StrSajSl { get; set; } = string.Empty;

            public double Time { get; set; } = 120.00;

            public string StrTime
            {
                get => Time.ToString("N");
                set => Time = Convert.ToDouble(value);
            }

            public Player DeepCopy()
            {
                var player = MemberwiseClone() as Player;
                return player;
            }
        }

        public class Tournament
        {
            public enum EventTypes
            { NONE, DH, SG, GS, SL }

            public string Name { get; set; } = string.Empty;
            public SexType Sex { get; set; } = SexType.Men;
            public EventTypes Types { get; set; } = EventTypes.SG;
        }
    }
}