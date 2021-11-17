using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace PointApp.Views
{
    public partial class CalcPoint : ContentPage
    {
        private enum ViewCellRowStyle { Height = 80 }

        private enum Association { FIS = 0, SAJ = 1 }

        private enum FValue
        {
            SL = 730,
            GS = 1010,
            SG = 1190
        }

        private enum MaximumPoint
        {
            SL = 165,
            GS = 220,
            SG = 270
        }

        private enum ErrorCode
        {
            UserDuplicated = 0,
            OverUserCount  = 1,
            LackStartUser = 2,
            LackFinishUser = 3,
            CalcError = 10
        }

        private Tournament m_tournament;
        private Player m_user;
        private ObservableCollection<Player> m_startDefPlayers;
        private ObservableCollection<Player> m_finishDefPlayers;
        private List<Player> m_allPlayers;
        
        public CalcPoint()
        {
            InitializeComponent();

            m_tournament       = new Tournament();
            m_startDefPlayers  = new ObservableCollection<Player>();
            m_finishDefPlayers = new ObservableCollection<Player>();
            m_allPlayers       = new List<Player>();

            SetUp();
        }

        private void SetUp()
        {
            var strJson = ReadPointList();
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

        private string ReadPointList()
        {
            // リソースを一覧取得
            var assembly = Assembly.GetExecutingAssembly();
            var jsonResource = from res in assembly.GetManifestResourceNames()
                               where res.EndsWith("json")
                               select res;
            if (jsonResource is null)
            {
                return string.Empty;
            }

            var file = assembly.GetManifestResourceStream(jsonResource.First());
            string strJson;
            using (var sr = new StreamReader(file))
            {
                strJson = sr.ReadToEnd();
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

        public void UpdateControl()
        {
            //PlayerEntry.IsVisible = m_isEditing;
            StartTopList.IsVisible = m_startDefPlayers.Count > 0 ? true : false;
        }

        private void EventSex_RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.ClassId != null)
            {
                m_tournament.Sex = int.Parse(radioButton.ClassId);
            }
        }

        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value)
            {
                return;
            }

            var checkedButton = sender as RadioButton;
            if (checkedButton is null)
            {
                return;
            }

            var checkedValue = checkedButton.Content as string;
            if (checkedValue is null)
            {
                return;
            }

            switch (checkedValue)
            {
                case "DH":
                    m_tournament.Types = Tournament.EventTypes.DH;
                    break;

                case "SG":
                    m_tournament.Types = Tournament.EventTypes.SG;
                    break;

                case "GS":
                    m_tournament.Types = Tournament.EventTypes.GS;
                    break;

                case "SL":
                    m_tournament.Types = Tournament.EventTypes.SL;
                    break;
            }
            SetStartDefPlayers();
        }

        private void PlayerEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var matchedPlayers = new ObservableCollection<Player>();
            var textCell = sender as Entry;

            if (textCell != null && !string.IsNullOrEmpty(textCell.Text))
            {
                string str = textCell.Text;

                if (IsHiragana(str))
                {
                    var kana = ConvKanaFromHiragana(textCell.Text);
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
            var allList = (sender == StartPlayerEntry) ? StartAllList : FinishAllList;
            if (matchedPlayers.Count > 0)
            {
                allList.ItemsSource = matchedPlayers;
                allList.IsVisible = true;
            }
            else
            {
                allList.IsVisible = false;
            }
            allList.SelectedItem = null;
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
                if (selectedPlayer != null && m_startDefPlayers.Contains(selectedPlayer))
                {
                    DisplayErrorMessage(ErrorCode.UserDuplicated);
                    return;
                }
                m_startDefPlayers.Add(selectedPlayer);
                SetStartDefPlayers();
            }
            else if (FinishAllList.SelectedItem != null)
            {
                if (m_finishDefPlayers.Count == 10)
                {
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                }
                selectedPlayer = FinishAllList.SelectedItem as Player;
                if (selectedPlayer != null && m_finishDefPlayers.Contains(selectedPlayer))
                {
                    DisplayErrorMessage(ErrorCode.UserDuplicated);
                    return;
                }
                m_finishDefPlayers.Add(selectedPlayer);
                SetFinishDefPlayers();
            }
        }

        private void Btn_Calc_Clicked(object sender, EventArgs e)
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

                double[] penaltyPoints = GetPenaltyPoints();
                if (penaltyPoints != null && double.IsNaN(penaltyPoints[(int)Association.FIS]) && double.IsNaN(penaltyPoints[(int)Association.SAJ]))
                {
                    FisResult_Label.Text = penaltyPoints[(int)Association.FIS].ToString();
                    SajResult_Label.Text = penaltyPoints[(int)Association.SAJ].ToString();
                    PopupLayout.IsVisible = true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        // FIXME 用途を要検討
        private bool hasKanji(string str)
        {
            return Regex.IsMatch(str,
                    @"[\p{IsCJKUnifiedIdeographs}" +
                    @"\p{IsCJKCompatibilityIdeographs}" +
                    @"\p{IsCJKUnifiedIdeographsExtensionA}]|" +
                    @"[\uD840-\uD869][\uDC00-\uDFFF]|\uD869[\uDC00-\uDEDF]");
        }

        private bool IsHiragana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsHiragana}*$");
        }

        private bool IsKatakana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsKatakana}*$");
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

        private ObservableCollection<Player> GetPlayersFromKanji(string kanji)
        {
            return new ObservableCollection<Player>(from player in m_allPlayers
                                                    where player.JapaneseName.StartsWith(kanji)
                                                    select player);
        }

        private ObservableCollection<Player> GetPlayersFromKana(string kana)
        {
            return new ObservableCollection<Player>(from player in m_allPlayers
                                                    where player.KanaName.StartsWith(kana)
                                                    select player);
        }


        private void SetStartDefPlayers()
        {
            SetDisplayPoint(m_tournament.Types, m_startDefPlayers);
            StartTopList.IsVisible = m_startDefPlayers.Count > 0;
            StartTopList.ItemsSource = m_startDefPlayers;
            StartTopList.HeightRequest = m_startDefPlayers.Count * (int)ViewCellRowStyle.Height;
            StartPlayerEntry.Text = string.Empty;
            StartAllList.SelectedItem = null;
        }

        private void SetFinishDefPlayers()
        {
            SetDisplayPoint(m_tournament.Types, m_finishDefPlayers);
            FinishTopList.IsVisible = m_finishDefPlayers.Count > 0 ? true : false;
            FinishTopList.ItemsSource = m_finishDefPlayers;
            FinishTopList.HeightRequest = m_finishDefPlayers.Count * (int)ViewCellRowStyle.Height;
            FinishPlayerEntry.Text = string.Empty;
            StartAllList.SelectedItem = null;
        }

        private void SetDisplayPoint(Tournament.EventTypes eventType, ObservableCollection<Player> players)
        {
            if (players.Count <= 0) { return; }
            foreach (var player in players)
            {
                switch (m_tournament.Types)
                {
                    case Tournament.EventTypes.NONE:
                        player.DisplayPoint = player.JapaneseName;
                        break;
                    case Tournament.EventTypes.DH:
                        player.DisplayPoint = $"{player.JapaneseName} FIS:{player.FisDh}  SAJ:{player.SajDh}";
                        break;

                    case Tournament.EventTypes.SG:
                        player.DisplayPoint = $"{player.JapaneseName} FIS:{player.FisSg}  SAJ:{player.SajSg}";
                        break;

                    case Tournament.EventTypes.GS:
                        player.DisplayPoint = $"{player.JapaneseName} FIS:{player.FisGs}  SAJ:{player.SajGs}";
                        break;

                    case Tournament.EventTypes.SL:
                        player.DisplayPoint = $"{player.JapaneseName} FIS:{player.FisSl}  SAJ:{player.SajSl}";
                        break;
                }
            }
        }


        private double[] GetPoints(Player player)
        {
            double[] points = new double[Enum.GetNames(typeof(Association)).Length];
            if (m_tournament.Types != Tournament.EventTypes.NONE)
            {
                switch (m_tournament.Types)
                {
                    case Tournament.EventTypes.DH:
                        points[(int)Association.FIS] = player.FisDh;
                        points[(int)Association.SAJ] = player.SajDh;
                        break;

                    case Tournament.EventTypes.SG:
                        points[(int)Association.FIS] = player.FisSg;
                        points[(int)Association.SAJ] = player.SajSg;
                        break;

                    case Tournament.EventTypes.GS:
                        points[(int)Association.FIS] = player.FisGs;
                        points[(int)Association.SAJ] = player.SajGs;
                        break;

                    case Tournament.EventTypes.SL:
                        points[(int)Association.FIS] = player.FisSl;
                        points[(int)Association.SAJ] = player.SajSl;
                        break;
                }
            }
            return points;
        }

        private double[] SumPoints(IEnumerable<Player> listPlayer)
        {
            double[] sumPoints = new double[Enum.GetNames(typeof(Association)).Length];
            foreach (var player in listPlayer)
            {
                var points = GetPoints(player);
                sumPoints[(int)Association.FIS] += points[(int)Association.FIS];
                sumPoints[(int)Association.SAJ] += points[(int)Association.SAJ];
            }
            return sumPoints;
        }

        private double GetRacePoints(Player targetPlayer, Player winner)
        {
            // Ｐ＝（Ｆ値×当該選手のタイム）÷ラップライム －Ｆ値 またはＰ＝（当該選手のタイム÷ラップタイム－１）×Ｆ値
            int fValue = 0;
            switch (m_tournament.Types)
            {
                case Tournament.EventTypes.DH:
                    break;

                case Tournament.EventTypes.SG:
                    fValue = (int)FValue.SG;
                    break;

                case Tournament.EventTypes.GS:
                    fValue = (int)FValue.GS;
                    break;

                case Tournament.EventTypes.SL:
                    fValue = (int)FValue.SL;
                    break;
            }
            return fValue * targetPlayer.Time / winner.Time - fValue;
        }
        
        private double SumRacePoint(IEnumerable<Player> listPlayer, Player winner)
        {
            double sumRacePoint = 0;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoints(player, winner);
            }
            return sumRacePoint;
        }

        private double CalcPenaltyPoint(double sumFinishFivePenaltyPoint, double sumStartFivePenaltyPoint, double sumFinishFiveRacePoint)
        {
            var penaltyPoint = (sumFinishFivePenaltyPoint + sumStartFivePenaltyPoint - sumFinishFiveRacePoint) * 0.1;
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

        private double[] GetPenaltyPoints()
        {
            //   A + B - C
            // （A)上位10名の中のポイントトップ5の所持ポイントの合計
            // （B)スタート時のトップ5の所持ポイント合計
            //  (C)上位10名の中のポイントトップ5 のレースポイントの合計　/ 10

            if (m_tournament.Types == Tournament.EventTypes.NONE || m_startDefPlayers.Count != 5 || m_finishDefPlayers.Count == 10)
            {
                return null;
            }

            
            var penaltyPoints = new double[Enum.GetNames(typeof(Association)).Length];
            double fisA, sajA, fisB, sajB, fisC, sajC;
            IEnumerable<Player> fisFinishTopfive = Enumerable.Empty<Player>();
            IEnumerable<Player> sajFinishTopfive = Enumerable.Empty<Player>();
            IEnumerable<Player> fisStartTopfive  = Enumerable.Empty<Player>();
            IEnumerable<Player> sajStartTopfive  = Enumerable.Empty<Player>();

            switch (m_tournament.Types)
            {
                case Tournament.EventTypes.SG:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisSg select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajSg select player).Take(5);
                    fisStartTopfive  = (from player in m_startDefPlayers orderby player.FisSg select player).Take(5);
                    sajStartTopfive  = (from player in m_startDefPlayers orderby player.SajSg select player).Take(5);
                    break;

                case Tournament.EventTypes.GS:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisGs select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajGs select player).Take(5);
                    fisStartTopfive  = (from player in m_startDefPlayers orderby player.FisGs select player).Take(5);
                    sajStartTopfive  = (from player in m_startDefPlayers orderby player.SajGs select player).Take(5);
                    break;

                case Tournament.EventTypes.SL:
                    fisFinishTopfive = (from player in m_finishDefPlayers orderby player.FisSl select player).Take(5);
                    sajFinishTopfive = (from player in m_finishDefPlayers orderby player.SajSl select player).Take(5);
                    fisStartTopfive  = (from player in m_startDefPlayers orderby player.FisSl select player).Take(5);
                    sajStartTopfive  = (from player in m_startDefPlayers orderby player.SajSl select player).Take(5);
                    break;
            }

            var winner = m_finishDefPlayers.OrderBy(player => player.Time).First();

            var finishTopFivePoints = SumPoints(fisFinishTopfive);
            var startTopFivePoints = SumPoints(fisStartTopfive);

            fisA = finishTopFivePoints[(int)Association.FIS];
            sajA = finishTopFivePoints[(int)Association.SAJ];

            fisB = startTopFivePoints[(int)Association.FIS];
            sajB = startTopFivePoints[(int)Association.SAJ];

            fisC = SumRacePoint(fisFinishTopfive, winner);
            sajC = SumRacePoint(sajFinishTopfive, winner);

            if (double.IsNaN(fisA) && double.IsNaN(fisB) && double.IsNaN(fisC) && double.IsNaN(sajA) && double.IsNaN(sajB) && double.IsNaN(sajC))
            {
                return penaltyPoints;
            }

            penaltyPoints[(int)Association.FIS] = CalcPenaltyPoint(fisA, fisB, fisC);
            penaltyPoints[(int)Association.SAJ] = CalcPenaltyPoint(sajA, sajB, sajC);

            return penaltyPoints;
        }

        private async void DisplayErrorMessage(ErrorCode errorCode)
        {
            switch(errorCode)
            {
                case ErrorCode.UserDuplicated:
                    await DisplayAlert("エラー", "選手が重複しています。", "OK");
                    break;

                case ErrorCode.OverUserCount:
                    await DisplayAlert("エラー", "これ以上選択できません。", "OK");
                    break;

                case ErrorCode.LackStartUser:
                    await DisplayAlert("エラー", "項番２は５人選択してください。", "OK");
                    break;

                case ErrorCode.LackFinishUser:
                    await DisplayAlert("エラー", "項番３は５人以上選択してください。", "OK");
                    break;

                case ErrorCode.CalcError:
                    await DisplayAlert("計算エラー", "管理者に報告してください。", "OK");
                    break;
            }
        }

        public class Player : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public string SajNo { get; set; } = string.Empty;
            public string FisNo { get; set; } = string.Empty;
            public string EnglishName { get; set; } = string.Empty;
            public string JapaneseName { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string Prefecture { get; set; } = string.Empty;
            public double Time { get; set; } = 120.00;
            public double FisDh
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strFisDh))
                    {
                        string strValue = strFisDh.Insert(strFisDh.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strFisDh { get; set; } = string.Empty;
            public double SajDh
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strSajDh))
                    {
                        string strValue = strSajDh.Insert(strSajDh.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strSajDh { get; set; } = string.Empty;
            public double FisSg
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strFisSg))
                    {
                        string strValue = strFisSg.Insert(strFisSg.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strFisSg { get; set; } = string.Empty;
            public double SajSg
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strSajSg))
                    {
                        string strValue = strSajSg.Insert(strSajSg.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strSajSg { get; set; } = string.Empty;
            public double FisSc
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strFisSc))
                    {
                        string strValue = strFisSc.Insert(strFisSc.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strFisSc { get; set; } = string.Empty;
            public double SajSc
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strSajSc))
                    {
                        string strValue = strSajSc.Insert(strSajSc.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }
            public string strSajSc { get; set; } = string.Empty;

            public double FisGs
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strFisGs))
                    {
                        string strValue = strFisGs.Insert(strFisGs.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string strFisGs { get; set; } = string.Empty;

            public double SajGs
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strSajGs))
                    {
                        string strValue = strSajGs.Insert(strSajGs.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string strSajGs { get; set; } = string.Empty;

            public double FisSl
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strFisSl))
                    {
                        string strValue = strFisSl.Insert(strFisSl.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string strFisSl { get; set; } = string.Empty;

            public double SajSl
            {
                get
                {
                    double dValue = 999.99;
                    if (!string.IsNullOrEmpty(strSajSl))
                    {
                        string strValue = strSajSl.Insert(strSajSl.Length - 2, ".");
                        dValue = double.Parse(strValue);
                    }
                    return dValue;
                }
            }

            public string strSajSl { get; set; } = string.Empty;
            private string _DisplayPoint { get; set; }

            public string DisplayPoint
            {
                get => _DisplayPoint;
                set
                {
                    _DisplayPoint = value;
                    NotifyPropertyChanged(nameof(DisplayPoint));
                }
            }

            public string TeamName { get; set; } = string.Empty;

            public string BirthOfDate { get; set; } = string.Empty;
            public string KanaName { get; set; } = string.Empty;

            public Player()
            {
            }
        }

        public class Tournament
        {
            public enum EventTypes { NONE, DH, SG, GS, SL }

            public string Name { get; set; } = string.Empty;
            public int Sex { get; set; } = 0; // 男子 = 0, 女子 = 1
            public EventTypes Types { get; set; } = EventTypes.NONE;
        }
    }
}