using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PointApp.Models;
using PointApp.Utilities;


namespace PointApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LiveTimingPage : TabbedPage
    {
        public LiveTimingPage()
        {
            InitializeComponent();
            m_Competition = new CompetitionInfo();
            m_startDefPlayers = new ObservableCollection<PlayerInfo>();
            m_finishDefPlayers = new ObservableCollection<PlayerInfo>();
            m_allPlayers = new List<PlayerInfo>();
            SetUpCalcPoint();
#if DEBUG
            Site_SearchBar.Text = "http://www.sports-event-is.com/php/pc/rank.php?GTID=6681&kumi=00&nendo=2022&kubun=1&Gpg=0&Gpg2=50";
#endif
        }

        private List<PlayerInfo> m_allPlayers;

        private readonly ObservableCollection<PlayerInfo> m_finishDefPlayers;

        private readonly ObservableCollection<PlayerInfo> m_startDefPlayers;

        private readonly CompetitionInfo m_Competition;

        private static readonly HttpClient m_httpClient = new HttpClient();

        private const int VIEW_CELL_ROW_HEIGHT = 53;

        #region イベントハンドラー

        #region 計算

        /// <summary>
        /// 性別選択時の処理
        /// </summary>
        private void EventSex_RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton?.ClassId != null)
            {
                m_Competition.Sex = (CompetitionInfo.SexType)Enum.ToObject(typeof(CompetitionInfo.SexType), Convert.ToInt32(radioButton.ClassId));
                SetUpCalcPoint();
            }
        }

        /// <summary>
        /// 種目選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompetitionType_RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton checkedButton)
            {
                if (checkedButton.Content is string checkedValue)
                {
                    switch (checkedValue)
                    {
                        case "ＤＨ":
                            m_Competition.Type = CompetitionInfo.EventType.DH;
                            break;

                        case "ＳＧ":
                            m_Competition.Type = CompetitionInfo.EventType.SG;
                            break;

                        case "ＧＳ":
                            m_Competition.Type = CompetitionInfo.EventType.GS;
                            break;

                        case "ＳＬ":
                            m_Competition.Type = CompetitionInfo.EventType.SL;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 選手選択時の処理
        /// </summary>
        private void AllList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (sender == StartAllList)
            {
                if (m_startDefPlayers.Count == 5)
                {
                    NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.OverUserCount);
                    return;
                }
                if (StartAllList.SelectedItem is PlayerInfo selectedPlayer)
                {
                    if (selectedPlayer != null && m_startDefPlayers.Any(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)))
                    {
                        NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.UserDuplicated);
                        return;
                    }
                    var copyPlayer = selectedPlayer.DeepCopy();
                    m_startDefPlayers.Add(copyPlayer);
                }
                SetTopListViewLayout(true);
                StartAllList.IsVisible = false;
            }
            else if (sender == FinishAllList)
            {
                if (m_finishDefPlayers.Count == 10)
                {
                    NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.OverUserCount);
                }
                if (FinishAllList.SelectedItem is PlayerInfo selectedPlayer)
                {
                    if (m_finishDefPlayers.Any(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)))
                    {
                        NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.UserDuplicated);
                        return;
                    }
                    var copyPlayer = selectedPlayer.DeepCopy();
                    m_finishDefPlayers.Add(copyPlayer);
                }
                SetTopListViewLayout(false);
                FinishAllList.IsVisible = false;
            }
        }

        /// <summary>
        /// 選手選択解除時の処理
        /// </summary>
        private void ButtonUnselect_Clicked(object sender, EventArgs e)
        {
            var button = sender as Xamarin.Forms.Button;
            var player = button.BindingContext as PlayerInfo;
            if (m_startDefPlayers.Contains(player))
            {
                m_startDefPlayers.Remove(player);
                SetTopListViewLayout(true);
            }
            else
            {
                m_finishDefPlayers.Remove(player);
                SetTopListViewLayout(false);
            }
        }

        private void SetTopListViewLayout(bool isStart)
        {
            Xamarin.Forms.ListView listView = isStart ? StartTopList : FinishTopList;
            ObservableCollection<PlayerInfo> players = isStart ? m_startDefPlayers : m_finishDefPlayers;
            SearchBar searchBar = isStart ? StartPlayerSearchBar : FinishPlayerSearchBar;

            listView.IsVisible = players.Count > 0;
            listView.ItemsSource = players;
            listView.HeightRequest = players.Count * VIEW_CELL_ROW_HEIGHT;
            listView.SelectedItem = null;
            searchBar.Text = string.Empty;
            if (isStart)
            {
                //StartExpander.ForceUpdateSize();
            }
            else
            {
                //FinishExpander.ForceUpdateSize();
            }
            StartPlayerDisplaySwitch.IsVisible = StartTopList.ItemsSource != null;
            FinishPlayerDisplaySwitch.IsVisible = FinishTopList.ItemsSource != null;
        }

        #endregion 計算

        #region 速報

        /// <summary>
        /// 速報更新時の処理
        /// </summary>
        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            Refresh_Indicator.IsRunning = true;
            if (Utilities.DeviceUtility.IsNetworkConnect())
            {
                var url = Site_SearchBar.Text;
                if (!string.IsNullOrEmpty(url))
                {
                    var resultInfo = await GetResultAsync(url);
                    if (resultInfo != null)
                    {
                        LiveTimingResult.ItemsSource = resultInfo.ListResultPlayerInfos;
                        LiveTimingResult.IsVisible = true;
                        DisplayPoint(resultInfo);
                    }

                }
            }
            else
            {
                NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.InvalidNetwork);
            }
            Refresh_Indicator.IsRunning = false;
        }

        private void DisplayPoint(ResultInfo resultInfo)
        {
            RaceNameLabel.Text = resultInfo.Name;
            FISPenaltyPointLabel.Text = resultInfo.FISPenaltyPoint.ToString();
            SAJPenaltyPointLabel.Text = resultInfo.SAJPenaltyPoint.ToString();
            RacePointLabel.Text = resultInfo.RacePointPerSec.ToString();
        }

        /// <summary>
        /// 計算ボタンクリック時の処理
        /// </summary>
        private async void Btn_Calc_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (m_startDefPlayers.Count < 5)
                {
                    NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.LackStartUser);
                    return;
                }
                if (m_finishDefPlayers.Count < 5)
                {
                    NotifyUtility.DisplayErrorMessage(this, NotifyUtility.ErrorCode.LackFinishUser);
                    return;
                }
                (var fisPenalty, var sajPenalty) = PointUtility.GetPenaltyPoints(m_Competition.Type, m_startDefPlayers.ToList(), m_finishDefPlayers.ToList());
                if (fisPenalty != null && sajPenalty != null)
                {
                    //string CompetitionName = Entry_CompetitionName.Text;
                    string fisPoint = fisPenalty.ToString();
                    string sajPoint = sajPenalty.ToString();
                    string userFisPoint = null;
                    string userSajPoint = null;
                    var winner = m_finishDefPlayers.OrderBy(player => player.Time).First();
                    if (!string.IsNullOrWhiteSpace(Entry_TargetTime.Text))
                    {
                        var racePoint = PointUtility.GetRacePoint(m_Competition.Type, new PlayerInfo { Time = double.Parse(Entry_TargetTime.Text) }, winner);
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
            }
        }

        #endregion 速報

        #endregion イベントハンドラー

        #region 内部関数

        #region 速報

        private async Task<ResultInfo> GetResultAsync(string url)
        {
            IHtmlCollection<IElement> title;
            IHtmlCollection<IElement> tableDatas;
            IHtmlCollection<IElement> startTableDatas;
            try
            {
                var uri = new Uri(url);
                var htmlResult = await m_httpClient.GetAsync(uri);
                var platform = DeviceInfo.Platform;
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding encoding = platform == DevicePlatform.Android ? encoding = Portable.Text.Encoding.GetEncoding("Shift_JIS") : encoding = Encoding.GetEncoding("Shift_JIS");

                using (var stream = await htmlResult.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, encoding, true) as TextReader)
                {
                    var htmlDoc = await reader.ReadToEndAsync();
                    var parser = new HtmlParser();
                    var doc = await parser.ParseDocumentAsync(htmlDoc);
                    title = doc.Body.QuerySelectorAll("h2");
                    tableDatas = doc.Body.QuerySelectorAll("td");
                }

                var resultInfo = new ResultInfo();
                {
                    //大会名、性別の取得
                    {
                        var list = title.ToList();
                        string info = list[0].TextContent;
                        var splitInfo = info.Split(' ');
                        resultInfo.Name = splitInfo[2];
                        resultInfo.Sex = info.Contains("男子") ? CompetitionInfo.SexType.Men : CompetitionInfo.SexType.Women;
                        resultInfo.IsFis = info.ToLower().Contains("fis");
                    }
                    // 種目の判定
                    {
                        if (resultInfo.Name.Contains("SL"))
                        {
                            resultInfo.Type = CompetitionInfo.EventType.SL;
                        }
                        else if (resultInfo.Name.Contains("GS"))
                        {
                            resultInfo.Type = CompetitionInfo.EventType.GS;
                        }
                        else if (resultInfo.Name.Contains("SG"))
                        {
                            resultInfo.Type = CompetitionInfo.EventType.SG;
                        }
                        else if (resultInfo.Name.Contains("DH"))
                        {
                            resultInfo.Type = CompetitionInfo.EventType.DH;
                        }
                        else
                        {
                            resultInfo.Type = CompetitionInfo.EventType.NONE;
                        }
                    }
                    // 速報データの取得
                    var results = new List<ResultPlayerInfo>();
                    {
                        var splitSize = 10;
                        List<IEnumerable<IElement>> chunks = tableDatas.Select((v, i) => new { v, i })
                            .GroupBy(x => x.i / splitSize)
                            .Select(g => g.Select(x => x.v)).ToList();
                        var pointList = PointUtility.GetPointList(resultInfo.Sex);

                        foreach (var chunk in chunks)
                        {
                            var result = AdjustResultInfo(chunk);
                            GetPlayerInfo(pointList, result);
                            results.Add(result);
                        }

                        // 速報の場合、1本目 DF の選手が表示されないので、1本目リストから取得する。
                        if (url.Contains("rank"))
                        {
                            url = url.Replace("rank", "1stst");
                            uri = new Uri(url);
                            htmlResult = await m_httpClient.GetAsync(uri);
                            using (var stream = await htmlResult.Content.ReadAsStreamAsync())
                            using (var reader = new StreamReader(stream, encoding, true) as TextReader)
                            {
                                var htmlDoc = await reader.ReadToEndAsync();
                                var parser = new HtmlParser();
                                var doc = await parser.ParseDocumentAsync(htmlDoc);
                                startTableDatas = doc.Body.QuerySelectorAll("td");
                            }
                            splitSize = 6;
                            chunks = null;
                            chunks = startTableDatas.Select((v, i) => new { v, i })
                                    .GroupBy(x => x.i / splitSize)
                                    .Select(g => g.Select(x => x.v)).ToList();

                            foreach (var chunk in chunks)
                            {
                                var result = AdjustStartInfo(chunk);
                                var target = results.FirstOrDefault(r => r.PlayerName.Equals(result.PlayerName));
                                if (target == null)
                                {
                                    GetPlayerInfo(pointList, result);
                                    result.PlayerInfo.Time = null;
                                    results.Add(result);
                                }
                            }
                        }
                    }
                    // レースポイント、ペナルティポイントの計算
                    {
                        List<PlayerInfo> resultStartDefPlayers = results.Where(result => result.PlayerInfo != null).Select(result => result.PlayerInfo).ToList();
                        List<PlayerInfo> resultFinishDefPlayers = results.Where(result => result.PlayerInfo != null && result.PlayerInfo.Time != null).Select(result => result.PlayerInfo).ToList();
                        if (results != null)
                        {
                            (var fisPenalty, var sajPenalty) = PointUtility.GetPenaltyPoints(resultInfo.Type, resultStartDefPlayers, resultFinishDefPlayers);
                            if (fisPenalty != null && sajPenalty != null)
                            {
                                var winner = resultFinishDefPlayers.OrderBy(player => player.Time).First();
                                foreach (var result in results)
                                {
                                    var targetPlayerInfo = result.PlayerInfo;
                                    if (targetPlayerInfo == null || targetPlayerInfo.Time == null || targetPlayerInfo.Time < 0)
                                    {
                                        continue;
                                    }
                                    var racePoint = PointUtility.GetRacePoint(resultInfo.Type, targetPlayerInfo, winner);
                                    if (racePoint >= 0)
                                    {
                                        targetPlayerInfo.ResultSajPoint = sajPenalty + racePoint;
                                        targetPlayerInfo.ResultFisPoint = fisPenalty + racePoint;
                                    }

                                    // ペナルティポイントの設定
                                    if (results.IndexOf(result) == 5)
                                    {
                                        resultInfo.RacePointPerSec = (double)Math.Round(racePoint / ((double)targetPlayerInfo.Time - (double)winner.Time), 2);
                                        resultInfo.FISPenaltyPoint = (double)fisPenalty;
                                        resultInfo.SAJPenaltyPoint = (double)sajPenalty;
                                    }

                                    // タイム差の設定
                                    if (targetPlayerInfo.Time != null && winner.Time != null)
                                    {
                                        targetPlayerInfo.Diff = targetPlayerInfo.Time - winner.Time;
                                    }
                                }
                            }
                        }
                    }
                    resultInfo.ListResultPlayerInfos = results;
                    return resultInfo;
                }
            }
            catch
            {
                return null;
            }
        }

        private ResultPlayerInfo AdjustResultInfo(IEnumerable<IElement> elements)
        {
            var list = elements.ToList();
            var result = new ResultPlayerInfo();
            {
                result.Rank = list[0].TextContent;
                result.StartNum = list[1].TextContent;
                result.PlayerName = list[2].TextContent;
                result.Affiliation = list[3].TextContent;
                result.FirstTime = list[4].TextContent;
                result.FirstRank = list[5].TextContent;
                result.SecondTime = list[6].TextContent;
                result.SecondRank = list[7].TextContent;
                result.TotalTime = list[8].TextContent;
                result.RacePoint = list[9].TextContent;
            }
            return result;
        }

        private ResultPlayerInfo AdjustStartInfo(IEnumerable<IElement> elements)
        {
            var list = elements.ToList();
            var result = new ResultPlayerInfo();
            {
                result.StartNum = list[1].TextContent;
                result.PlayerName = list[2].TextContent;
                result.Affiliation = list[5].TextContent;
            }
            return result;
        }

        private void GetPlayerInfo(List<PlayerInfo> playerInfos, ResultPlayerInfo targetResult)
        {
            string name = targetResult.PlayerName.Replace(" ", string.Empty);
            var matchedPlayers = playerInfos.Where(player => player.JapaneseName.Equals(name));
            if (matchedPlayers != null && matchedPlayers.Any())
            {
                if (matchedPlayers.Any())
                {
                    targetResult.PlayerInfo = matchedPlayers.FirstOrDefault();
                    string strTime = targetResult.TotalTime;
                    if (!string.IsNullOrEmpty(strTime) && !strTime.Contains("DF"))
                    {
                        double time = -1;
                        {
                            // 01:37.23 形式を double に変換
                            //strTime = targetResult.TotalTime.TrimStart('0');
                            strTime = strTime.Replace(":", "."); //
                            var splitTime = strTime.Split('.');
                            splitTime[0] = splitTime[0].TrimStart('0');
                            splitTime[1] = splitTime[1].TrimStart('0');
                            splitTime[2] = splitTime[2].TrimStart('0');
                            int seconds = int.Parse(splitTime[0]) * 60 + int.Parse(splitTime[1]);
                            double decminal = int.Parse(splitTime[2]) * 0.01;
                            time = seconds + decminal;
                        }
                        if (time > 0)
                        {
                            targetResult.PlayerInfo.Time = time;
                        }
                    }
                    else
                    {
                        targetResult.PlayerInfo.Time = null;
                    }
                }
            }
        }

        #endregion 速報

        #region 計算

        private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
        {
            var matchedPlayers = new ObservableCollection<PlayerInfo>();

            if (sender is SearchBar searchBar && searchBar.Text is string searchText && !string.IsNullOrEmpty(searchText))
            {
                searchBar.Text = string.Empty;
                if (PointUtility.IsHiragana(searchText))
                {
                    searchText = PointUtility.ConvKanaFromHiragana(searchText);
                }
                matchedPlayers = PointUtility.IsKatakana(searchText) ? PointUtility.GetPlayersFromKana(searchText, m_allPlayers) : PointUtility.GetPlayersFromKanji(searchText, m_allPlayers);
            }

            Xamarin.Forms.ListView allList = (sender == StartPlayerSearchBar) ? StartAllList : FinishAllList;
            if (matchedPlayers.Count > 0)
            {
                allList.ItemsSource = matchedPlayers;
                allList.IsVisible = true;
            }
            else
            {
                allList.IsVisible = false;
            }
            allList.ItemSelected -= AllList_ItemSelected;
            allList.SelectedItem = null;
            allList.ItemSelected += AllList_ItemSelected;
        }

        private List<PlayerInfo> OrderByPoint(CompetitionInfo.EventType type, bool isFisPoint, IEnumerable<PlayerInfo> listPlayerInfos)
        {
            if (listPlayerInfos == null)
            {
                return null;
            }
            switch (type)
            {
                case CompetitionInfo.EventType.SL:
                    return isFisPoint
                        ? listPlayerInfos.OrderBy(playerInfo => playerInfo.FisSl).ToList()
                        : listPlayerInfos.OrderBy(playerInfo => playerInfo.SajSl).ToList();

                case CompetitionInfo.EventType.GS:
                    return isFisPoint
                        ? listPlayerInfos.OrderBy(playerInfo => playerInfo.FisGs).ToList()
                        : listPlayerInfos.OrderBy(playerInfo => playerInfo.SajGs).ToList();

                case CompetitionInfo.EventType.SG:
                    return isFisPoint
                        ? listPlayerInfos.OrderBy(playerInfo => playerInfo.FisSg).ToList()
                        : listPlayerInfos.OrderBy(playerInfo => playerInfo.SajSg).ToList();

                case CompetitionInfo.EventType.DH:
                    return isFisPoint
                        ? listPlayerInfos.OrderBy(playerInfo => playerInfo.FisDh).ToList()
                        : listPlayerInfos.OrderBy(playerInfo => playerInfo.SajDh).ToList();

                case CompetitionInfo.EventType.NONE:
                    return null;

                default:
                    return null;
            }
        }

        private List<PlayerInfo> OrderByTime(IEnumerable<PlayerInfo> listPlayerInfos)
        {
            if (listPlayerInfos == null)
            {
                return null;
            }
            return listPlayerInfos.Where(playerInfo => playerInfo.Time != null).OrderBy(playerInfo => playerInfo.Time).ToList();
        }


        private void SetUpCalcPoint()
        {
            m_startDefPlayers.Clear();
            m_finishDefPlayers.Clear();
            StartTopList.HeightRequest = 0;
            FinishTopList.HeightRequest = 0;
            m_allPlayers = PointUtility.GetPointList(m_Competition.Sex);
        }

        #endregion 計算

        #endregion 内部関数

        #region クラス定義


        public class ResultInfo : CompetitionInfo
        {
            public bool IsFis { get; set; } = false;
            public double SAJPenaltyPoint { get; set; } = 0;
            public double FISPenaltyPoint { get; set; } = 0;
            public double RacePointPerSec { get; set; } = 0;
            public List<ResultPlayerInfo> ListResultPlayerInfos { get; set; } = new List<ResultPlayerInfo>();
        }

        public class ResultPlayerInfo : INotifyPropertyChanged
        {
            public PlayerInfo PlayerInfo { get; set; } = null;
            public string Rank { get; set; } = string.Empty;
            public string StartNum { get; set; } = string.Empty;
            public string PlayerName { get; set; } = string.Empty;
            public string Affiliation { get; set; } = string.Empty;
            public string FirstTime { get; set; } = string.Empty;
            public string FirstRank { get; set; } = string.Empty;
            public string SecondTime { get; set; } = string.Empty;
            public string SecondRank { get; set; } = string.Empty;
            public string TotalTime { get; set; } = string.Empty;
            public string RacePoint { get; set; } = string.Empty;

            private bool _IsExpanded = false;

            public bool IsExpanded
            {
                get => _IsExpanded;
                set
                {
                    if (_IsExpanded != value)
                    {
                        _IsExpanded = value;
                        RaisePropertyChanged(nameof(IsExpanded));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion クラス定義

        private void TabbedPage_CurrentPageChanged(object sender, EventArgs e)
        {
            TabbedPageMain.Title = TabbedPageMain.CurrentPage.Title;
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            if (sender is StackLayout stackLayout)
            {
                var children = stackLayout.Children;
                if (children.FirstOrDefault(child => child is Xamarin.CommunityToolkit.UI.Views.Expander) is Xamarin.CommunityToolkit.UI.Views.Expander expander)
                {
                    expander.IsExpanded = !expander.IsExpanded;
                }
            }
        }
    }
}