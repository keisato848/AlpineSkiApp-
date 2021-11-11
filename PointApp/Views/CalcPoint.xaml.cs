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
        public enum PlayersCount { DEFAULT = 5 }
        public enum ViewCellRowStyle { Height = 70 }

        public enum Association { FIS = 0, SAJ = 1 }

        public static Tournament m_tournament;
        public Player m_user;
        public ObservableCollection<Player> m_startDefPlayers;
        public ObservableCollection<Player> m_finishDefPlayers;
        public static List<Player> m_allPlayers;
        
        private bool m_isEditing { get; set; }

        public CalcPoint()
        {
            InitializeComponent();

            m_tournament = new Tournament();
            m_startDefPlayers = new ObservableCollection<Player>();
            m_finishDefPlayers = new ObservableCollection<Player>();
            m_allPlayers = new List<Player>();
            m_isEditing = false;

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

            public string VenueName { get; set; }
            public EventTypes Types { get; set; }
            public int HeightDh { get; set; }
            public int HeightSg { get; set; }
            public int HeightGs { get; set; }
            public int HeightSl { get; set; }

            public Tournament()
            {
                VenueName = null;
                Types = EventTypes.NONE;
                HeightDh = int.MinValue;
                HeightSg = int.MinValue;
                HeightGs = int.MinValue;
                HeightSl = int.MinValue;
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
                allList.HeightRequest = 100;
                allList.IsVisible = true;
            }
            else
            {
                allList.HeightRequest = 0;
                allList.IsVisible = false;
            }
            allList.SelectedItem = null;
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

        private void Button_Clicked(object sender, EventArgs e)
        {
            Player selectedPlayer = new Player();
            if (StartAllList.SelectedItem != null)
            {
                selectedPlayer = StartAllList.SelectedItem as Player;
                m_startDefPlayers.Add(selectedPlayer);
                SetStartDefPlayers();
            }
            if (FinishAllList.SelectedItem != null)
            {
                selectedPlayer = FinishAllList.SelectedItem as Player;
                m_finishDefPlayers.Add(selectedPlayer);
                SetFinishDefPlayers();
            }
        }

        private void StartAllList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            Player selectedPlayer = new Player();
            if (StartAllList.SelectedItem != null)
            {
                selectedPlayer = StartAllList.SelectedItem as Player;
                m_startDefPlayers.Add(selectedPlayer);
                SetStartDefPlayers();
            }
            if (FinishAllList.SelectedItem != null)
            {
                selectedPlayer = FinishAllList.SelectedItem as Player;
                m_finishDefPlayers.Add(selectedPlayer);
                SetFinishDefPlayers();
            }
        }
        private void SetStartDefPlayers()
        {
            SetDisplayPoint(m_tournament.Types, m_startDefPlayers);
            StartTopList.IsVisible = m_startDefPlayers.Count > 0 ? true : false;
            StartTopList.ItemsSource = m_startDefPlayers;
            StartTopList.HeightRequest = m_startDefPlayers.Count * (int)ViewCellRowStyle.Height;
            StartPlayerEntry.Text = string.Empty;
        }

        private void SetFinishDefPlayers()
        {
            SetDisplayPoint(m_tournament.Types, m_finishDefPlayers);
            FinishTopList.IsVisible = m_finishDefPlayers.Count > 0 ? true : false;
            FinishTopList.ItemsSource = m_finishDefPlayers;
            FinishTopList.HeightRequest = m_finishDefPlayers.Count * (int)ViewCellRowStyle.Height;
            FinishPlayerEntry.Text = string.Empty;
        }

        private void SetDisplayPoint(Tournament.EventTypes eventType, ObservableCollection<Player> players)
        {
            if (players.Count > 0)
            {
                int ID = 1;
                foreach (var player in players)
                {
                    switch (m_tournament.Types)
                    {
                        case Tournament.EventTypes.NONE:
                            player.DisplayPoint = player.JapaneseName;
                            break;
                        case Tournament.EventTypes.DH:
                            player.DisplayPoint = $"{ID}. {player.JapaneseName}__FIS:{player.FisDh}  SAJ:{player.SajDh}";
                            break;

                        case Tournament.EventTypes.SG:
                            player.DisplayPoint = $"{ID}. {player.JapaneseName}__FIS:{player.FisSg}  SAJ:{player.SajSg}";
                            break;

                        case Tournament.EventTypes.GS:
                            player.DisplayPoint = $"{ID}. {player.JapaneseName}__FIS:{player.FisGs}  SAJ:{player.SajGs}";
                            break;

                        case Tournament.EventTypes.SL:
                            player.DisplayPoint = $"{ID}. {player.JapaneseName}__FIS:{player.FisSl}  SAJ:{player.SajSl}";
                            break;
                    }
                    ++ID;
                }
            }
        }

        private void Btn_Calc_Clicked(object sender, EventArgs e)
        {
            double[] penaltyPoints = GetPenaltyPoints();
        }

        private double GetRacePoints(Player targetPlayer, Player winner)
        {
            // Ｐ＝（Ｆ値×当該選手のタイム）÷ラップライム －Ｆ値 またはＰ＝（当該選手のタイム÷ラップタイム－１）×Ｆ値
            int fValue = -1;
            if (m_tournament.Types != Tournament.EventTypes.NONE)
            {
                switch (m_tournament.Types)
                {
                    case Tournament.EventTypes.DH:
                        //fValue = FValue;
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

            }
            return fValue * targetPlayer.Time / winner.Time - fValue;
        }

        private double[] GetPenaltyPoints()
        {
            //  A + B - C
            // （A)上位10名の中のポイントトップ5の所持ポイントの合計
            // （B)スタート時のトップ5の所持ポイント合計
            //  (C)上位10名の中のポイントトップ5 のレースポイントの合計　/ 10

            if (m_tournament.Types == Tournament.EventTypes.NONE || m_startDefPlayers.Count != 5 || m_finishDefPlayers.Count == 10)
            {
                return null;
            }

            
            var penaltyPoints = new double[] { Enum.GetNames(typeof(Association)).Length };
            double fisA = double.MinValue;
            double sajA = double.MinValue;
            double fisB = double.MinValue;
            double sajB = double.MinValue;
            double fisC = double.MinValue;
            double sajC = double.MinValue;

            foreach (var player in m_startDefPlayers)
            {
                switch (m_tournament.Types)
                {
                    case Tournament.EventTypes.GS:
                        fisB += player.FisGs;
                        break;

                    case Tournament.EventTypes.SL:
                        fisB += player.FisSl;
                        sajB += player.SajSl;
                        break;
                }
            }


            var fisFinishTopfive = (from player in m_finishDefPlayers
                              orderby player.FisSl
                              select player).Take(5);

            var sajFinishTopfive = (from player in m_finishDefPlayers
                              orderby player.SajSl
                              select player).Take(5);

            var fisStartTopfive = (from player in m_startDefPlayers
                                    orderby player.FisSl
                                    select player).Take(5);

            var sajStartTopfive = (from player in m_startDefPlayers
                                    orderby player.SajSl
                                    select player).Take(5);

            var winner = m_finishDefPlayers.OrderBy(player => player.Time).First();

            var finishTopFivePoints = SumPoints(fisFinishTopfive);
            var startTopFivePoints = SumPoints(fisStartTopfive);

            fisA = finishTopFivePoints[(int)Association.FIS];
            sajA = finishTopFivePoints[(int)Association.SAJ];

            fisB = startTopFivePoints[(int)Association.FIS];
            sajB = startTopFivePoints[(int)Association.SAJ];

            fisC = SumRacePoint(fisFinishTopfive, winner);
            sajC = SumRacePoint(sajFinishTopfive, winner);

            penaltyPoints[(int)Association.FIS] = CalcPenaltyPoint(fisA, fisB, fisC);
            penaltyPoints[(int)Association.SAJ] = CalcPenaltyPoint(sajA, sajB, sajC);

            return penaltyPoints;
        }

        private double CalcPenaltyPoint(double sumFinishFivePenaltyPoint, double sumStartFivePenaltyPoint, double sumFinishFiveRacePoint)
        {
            return (sumFinishFivePenaltyPoint + sumStartFivePenaltyPoint - sumFinishFiveRacePoint) * 0.1;
        }

        private double SumRacePoint(IEnumerable<Player> listPlayer, Player winner)
        {
            double sumRacePoint = double.MaxValue;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoints(player, winner);
            }
            return sumRacePoint;
        }

        private double[] SumPoints(IEnumerable<Player> listPlayer)
        {
            double[] sumPoints = new double[] { Enum.GetNames(typeof(Association)).Length };
            foreach (var player in listPlayer)
            {
                var points = GetPoints(player);
                sumPoints[(int)Association.FIS] += points[(int)Association.FIS];
                sumPoints[(int)Association.SAJ] += points[(int)Association.SAJ];
            }
            return sumPoints;
        }

        private double[] GetPoints(Player player)
        {
            double[] points = new double[] { Enum.GetNames(typeof(Association)).Length };
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

        //private double SumPoints(IEnumerable<double> points)
        //{
        //    double sumPoints = double.MinValue;
        //    foreach (var point in points)
        //    {
        //        sumPoints += point;
        //    }
        //    return sumPoints;
        //}

        public enum FValue
        {
            SL = 730,
            GS = 1010,
            SG = 1190
        }

        public enum MaxValue
        {
            SL = 165,
            GS = 220,
            SG = 270
        }
    }
}