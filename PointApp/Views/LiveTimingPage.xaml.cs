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
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;

namespace PointApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LiveTimingPage : Xamarin.Forms.TabbedPage
    {
        public LiveTimingPage()
        {
            InitializeComponent();
            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom).SetIsSmoothScrollEnabled(true);
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

        private ObservableCollection<PlayerInfo> m_finishDefPlayers;

        private ObservableCollection<PlayerInfo> m_startDefPlayers;

        private readonly CompetitionInfo m_Competition;

        private static readonly HttpClient m_httpClient = new HttpClient();

        private enum ErrorCode
        {
            UserDuplicated = 0,
            OverUserCount = 1,
            LackStartUser = 2,
            LackFinishUser = 3,
            CalcError = 4,
            IdEmpty = 5,
            PwdEmpty = 6,
            CompetitionNameEmpty = 7,
            InvalidNetwork = 8,
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
        { Height = 53 }

        private bool IsConnective()
        {
            return Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet
                    ? true : false;
        }

        #region イベントハンドラー

        #region 計算

        /// <summary>
        /// 性別選択時の処理
        /// </summary>
        private void EventSex_RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton?.ClassId != null)
            {
                m_Competition.Sex = (SexType)Enum.ToObject(typeof(SexType), Convert.ToInt32(radioButton.ClassId));
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
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                    return;
                }
                if (StartAllList.SelectedItem is PlayerInfo selectedPlayer)
                {
                    if (selectedPlayer != null && m_startDefPlayers.Count(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)) > 0)
                    {
                        DisplayErrorMessage(ErrorCode.UserDuplicated);
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
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                }
                if (FinishAllList.SelectedItem is PlayerInfo selectedPlayer)
                {
                    if (m_finishDefPlayers.Count(player => player.JapaneseName.Equals(selectedPlayer.JapaneseName)) > 0)
                    {
                        DisplayErrorMessage(ErrorCode.UserDuplicated);
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
            listView.HeightRequest = players.Count * (int)ViewCellRowStyle.Height;
            listView.SelectedItem = null;
            searchBar.Text = string.Empty;
            if (isStart)
            {
                StartExpander.ForceUpdateSize();
            }
            else
            {
                FinishExpander.ForceUpdateSize();
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
            if (IsConnective())
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
                DisplayErrorMessage(ErrorCode.InvalidNetwork);
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
                (var fisPenalty, var sajPenalty) = GetPenaltyPoints(m_Competition.Type, m_startDefPlayers.ToList(), m_finishDefPlayers.ToList());
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
                        var racePoint = GetRacePoint(m_Competition.Type, new PlayerInfo { Time = double.Parse(Entry_TargetTime.Text) }, winner);
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
                        resultInfo.Sex = info.Contains("男子") ? SexType.Men : SexType.Women;
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
                        var pointList = GetPointList(resultInfo.Sex);

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
                            (var fisPenalty, var sajPenalty) = GetPenaltyPoints(resultInfo.Type, resultStartDefPlayers, resultFinishDefPlayers);
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
                                    var racePoint = GetRacePoint(resultInfo.Type, targetPlayerInfo, winner);
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
            if (matchedPlayers != null && matchedPlayers.Count() > 0)
            {
                if (matchedPlayers.Count() > 0)
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

        private double? CalcPenaltyPoint(CompetitionInfo.EventType type, double sumFinishFivePenaltyPoint, double sumStartFivePenaltyPoint, double sumFinishFiveRacePoint)
        {
            decimal def = decimal.Add(Convert.ToDecimal(sumFinishFivePenaltyPoint), Convert.ToDecimal(sumStartFivePenaltyPoint));
            decimal def2 = decimal.Subtract(def, Convert.ToDecimal(sumFinishFiveRacePoint));

            var penaltyPoint = Math.Round((Convert.ToInt32(sumFinishFivePenaltyPoint * 100) + Convert.ToInt32(sumStartFivePenaltyPoint * 100) - Convert.ToInt32(sumFinishFiveRacePoint * 100)) * 0.001, 2, MidpointRounding.AwayFromZero);
            switch (type)
            {
                case CompetitionInfo.EventType.SG:
                    if (penaltyPoint > (double)MaximumPoint.SG) { penaltyPoint = (double)MaximumPoint.SG; }
                    break;

                case CompetitionInfo.EventType.GS:
                    if (penaltyPoint > (double)MaximumPoint.GS) { penaltyPoint = (double)MaximumPoint.GS; }
                    break;

                case CompetitionInfo.EventType.SL:
                    if (penaltyPoint > (double)MaximumPoint.SL) { penaltyPoint = (double)MaximumPoint.SL; }
                    break;
            }
            return penaltyPoint + 3;
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

                case ErrorCode.CompetitionNameEmpty:
                    await DisplayAlert("通知", "大会名を入力してください。", "OK");
                    break;

                case ErrorCode.InvalidNetwork:
                    await DisplayAlert("通知", "ネットワークに接続されていません。", "OK");
                    break;

                default:
                    break;
            }
        }

        private ObservableCollection<PlayerInfo> GetPlayersFromKana(string kana)
        {
            return new ObservableCollection<PlayerInfo>(from player in m_allPlayers
                                                        where player.KanaName.StartsWith(kana)
                                                        orderby player.FisGs
                                                        select player);
        }

        private ObservableCollection<PlayerInfo> GetPlayersFromKanji(string kanji)
        {
            return new ObservableCollection<PlayerInfo>(from player in m_allPlayers
                                                        where player.JapaneseName.StartsWith(kanji)
                                                        select player);
        }

        private double GetPoint(CompetitionInfo.EventType type, bool isFIS, PlayerInfo player)
        {
            switch (type)
            {
                case CompetitionInfo.EventType.DH:
                    return isFIS ? player.FisDh : player.SajDh;

                case CompetitionInfo.EventType.SG:
                    return isFIS ? player.FisSg : player.SajSg;

                case CompetitionInfo.EventType.GS:
                    return isFIS ? player.FisGs : player.SajGs;

                case CompetitionInfo.EventType.SL:
                    return isFIS ? player.FisSl : player.SajSl;

                default:
                    return double.NaN;
            }
        }

        private double GetRacePoint(CompetitionInfo.EventType type, PlayerInfo targetPlayer, PlayerInfo winner)
        {
            // Ｐ＝（Ｆ値×当該選手のタイム）÷ラップライム －Ｆ値 またはＰ＝（当該選手のタイム÷ラップタイム－１）×Ｆ値
            if (targetPlayer.Time == winner.Time)
            {
                return 0;
            }
            decimal fValue = 0;
            switch (type)
            {
                case CompetitionInfo.EventType.SG:
                    fValue = Convert.ToDecimal((int)FValue.SG);
                    break;

                case CompetitionInfo.EventType.GS:
                    fValue = Convert.ToDecimal((int)FValue.GS);
                    break;

                case CompetitionInfo.EventType.SL:
                    fValue = Convert.ToDecimal((int)FValue.SL);
                    break;

                default:
                    break;
            }
            decimal dTargetTime = Convert.ToDecimal(targetPlayer.Time);
            decimal dWinnerTime = Convert.ToDecimal(winner.Time);
            decimal dDividedTime = decimal.Divide(dTargetTime, dWinnerTime) - 1;

            return Convert.ToDouble(decimal.Multiply(dDividedTime, fValue));
        }

        private bool IsHiragana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsHiragana}*$");
        }

        private bool IsKatakana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsKatakana}*$");
        }

        private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
        {
            var matchedPlayers = new ObservableCollection<PlayerInfo>();

            if (sender is SearchBar searchBar && searchBar.Text is string searchText && !string.IsNullOrEmpty(searchText))
            {
                searchBar.Text = string.Empty;
                if (IsHiragana(searchText))
                {
                    searchText = ConvKanaFromHiragana(searchText);
                }
                matchedPlayers = IsKatakana(searchText) ? GetPlayersFromKana(searchText) : GetPlayersFromKanji(searchText);
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

        private (double?, double?) GetPenaltyPoints(CompetitionInfo.EventType type, List<PlayerInfo> startDefPlayers, List<PlayerInfo> finishDefPlayers)
        {
            //   A + B - C
            // （A)上位10名の中のポイントトップ5の所持ポイントの合計
            // （B)スタート時のトップ5の所持ポイント合計
            //  (C)上位10名の中のポイントトップ5 のレースポイントの合計　/ 10

            if (type == CompetitionInfo.EventType.NONE || startDefPlayers.Count < 5 || finishDefPlayers.Count < 5)
            {
                return (null, null);
            }

            var sortedFisStartDefPlayers = OrderByPoint(type, true, startDefPlayers);
            var sortedFisFinishDefPlayers = OrderByPoint(type, true, OrderByTime(finishDefPlayers).Take(10));
            var sortedSajStartDefPlayers = OrderByPoint(type, false, startDefPlayers);
            var sortedSajFinishDefPlayers = OrderByPoint(type, false, OrderByTime(finishDefPlayers).Take(10));

            var fisStartTopfive = sortedFisStartDefPlayers.Take(5).ToList();
            var fisFinishTopFive = sortedFisFinishDefPlayers.Take(5).ToList();

            var sajStartTopfive = sortedSajStartDefPlayers.Take(5).ToList();
            var sajFinishTopfive = sortedSajFinishDefPlayers.Take(5).ToList();

            var winner = finishDefPlayers.OrderBy(player => player.Time).First();

            double fisA, sajA, fisB, sajB, fisC, sajC;
            fisA = GetSumPoints(type, true, fisFinishTopFive);
            sajA = GetSumPoints(type, false, sajFinishTopfive);
            fisB = GetSumPoints(type, true, fisStartTopfive);
            sajB = GetSumPoints(type, false, sajStartTopfive);
            fisC = SumRacePoint(type, fisFinishTopFive, winner);
            sajC = SumRacePoint(type, sajFinishTopfive, winner);

            if (double.IsNaN(fisA) && double.IsNaN(fisB) && double.IsNaN(fisC) && double.IsNaN(sajA) && double.IsNaN(sajB) && double.IsNaN(sajC))
            {
                return (null, null);
            }

            return (CalcPenaltyPoint(type, fisA, fisB, fisC), CalcPenaltyPoint(type, sajA, sajB, sajC));
        }

        private void SetTestAds()
        {
            //string testId = "{OnPlatform Android=ca-app-pub-2633806931583277/2100712905, iOS=ca-app-pub-2633806931583277/8738536773}";
            //AdsArea.AdsId = testId;
        }

        private string ReadPointList(SexType sexType)
        {
            string strJson = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonResource = assembly.GetManifestResourceNames();
                if (jsonResource != null)
                {
                    var file = sexType == SexType.Men
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

        private void RemoveEmptyInName(List<PlayerInfo> listPlayer)
        {
            var listPlayerTemp = new List<PlayerInfo>(listPlayer);
            if (listPlayerTemp == null)
            {
                return;
            }

            foreach (var player in listPlayerTemp)
            {
                player.JapaneseName = Regex.Replace(player.JapaneseName, @"\s", "");
            }
        }

        private void SetUpCalcPoint()
        {
            m_startDefPlayers.Clear();
            m_finishDefPlayers.Clear();
            StartTopList.HeightRequest = 0;
            FinishTopList.HeightRequest = 0;
            m_allPlayers = GetPointList(m_Competition.Sex);
        }

        private List<PlayerInfo> GetPointList(SexType sexType)
        {
            var strJson = ReadPointList(sexType);
            if (strJson is string)
            {
                try
                {
                    var listPlayers = JsonSerializer.Deserialize<List<PlayerInfo>>(strJson);
                    if (listPlayers is List<PlayerInfo>)
                    {
                        RemoveEmptyInName(listPlayers);
                        return listPlayers;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }
            return null;
        }

        private double GetSumPoints(CompetitionInfo.EventType type, bool isFIS, IEnumerable<PlayerInfo> listPlayer)
        {
            decimal result = 0;
            foreach (var player in listPlayer)
            {
                var point = GetPoint(type, isFIS, player);
                if (!double.IsNaN(point))
                {
                    decimal dPoint = Convert.ToDecimal(point);
                    result = decimal.Add(result, dPoint);
                }
            }
            return Convert.ToDouble(result);
        }

        private double SumRacePoint(CompetitionInfo.EventType type, IEnumerable<PlayerInfo> listPlayer, PlayerInfo winner)
        {
            double sumRacePoint = 0;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoint(type, player, winner);
            }
            return sumRacePoint;
        }

        //private void RegisterCompetition(string Competition_name, string fis_penalty, string saj_penalty, string per_sec_race_point)
        //{
        //    try
        //    {
        //        string user_id = Application.Current.Resources["LoginUserId"].ToString();
        //        if (!string.IsNullOrEmpty(user_id))
        //        {
        //            using (var connection = DatabaseUtility.ConnectDataBase())
        //            {
        //                connection.Open();
        //                var time = DateTime.Now.ToString().Replace('/', '-');
        //                using (var transaction = connection.BeginTransaction())
        //                {
        //                    var sql = $"INSERT INTO Competitions_table (Competition_name, fis_penarty, saj_penarty, per_sec_race_point, user_id, created_at, updated_at) VALUES('{Competition_name}', '{fis_penalty}', '{saj_penalty}', '{per_sec_race_point}', '{user_id}', '{time}', '{time}');";
        //                    DatabaseUtility.ExecuteSqlNonquery(sql, connection);
        //                    transaction.Commit();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        #endregion 計算

        #endregion 内部関数

        #region クラス定義

        public class PlayerInfo
        {
            private double ConvertStrPointToDouble(string strPoint)
            {
                string strValue = strPoint;
                int pos = strPoint.Length - 2;
                if (strPoint.Length == 4 && pos > 0 && strPoint[pos] != '.')
                {
                    strValue = strPoint.Insert(pos, ".");
                }
                return double.Parse(strValue);
            }

            public double FisDh
            {
                get
                {
                    double dValue = 330.00;
                    if (!string.IsNullOrWhiteSpace(StrFisDh))
                    {
                        dValue = ConvertStrPointToDouble(StrFisDh);
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
                        dValue = ConvertStrPointToDouble(StrFisGs);
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
                        dValue = ConvertStrPointToDouble(StrFisSc);
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
                        dValue = ConvertStrPointToDouble(StrFisSg);
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
                        dValue = ConvertStrPointToDouble(StrFisSl);
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
                        dValue = ConvertStrPointToDouble(StrSajDh);
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
                        dValue = ConvertStrPointToDouble(StrSajGs);
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
                        dValue = ConvertStrPointToDouble(StrSajSc);
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
                        dValue = ConvertStrPointToDouble(StrSajSg);
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
                        dValue = ConvertStrPointToDouble(StrSajSl);
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
            private double? _ResultSajPoint = null;

            public double? ResultSajPoint
            {
                get
                {
                    if (_ResultSajPoint == null)
                    {
                        return null;
                    }
                    else
                    {
                        return (double)Math.Round((decimal)_ResultSajPoint, 2);
                    }
                }
                set { _ResultSajPoint = value; }
            }

            private double? _ResultFisPoint = null;

            public double? ResultFisPoint
            {
                get
                {
                    if (_ResultFisPoint == null)
                    {
                        return null;
                    }
                    else
                    {
                        return (double)Math.Round((decimal)_ResultFisPoint, 2);
                    }
                }
                set { _ResultFisPoint = value; }
            }

            public double? Time { get; set; } = 120.00;

            public double? Diff { get; set; } = null;


            public PlayerInfo DeepCopy()
            {
                var player = MemberwiseClone() as PlayerInfo;
                return player;
            }
        }

        public class CompetitionInfo
        {
            public enum EventType
            { NONE, DH, SG, GS, SL }

            public string Name { get; set; } = string.Empty;
            public SexType Sex { get; set; } = SexType.Men;
            public EventType Type { get; set; } = EventType.SG;
        }

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