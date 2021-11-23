﻿using System;
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
using Npgsql;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Shapes;

namespace PointApp.Views
{
    public partial class CalcPoint : ContentPage
    {
        private List<Player> m_allPlayers;

        private ObservableCollection<Player> m_finishDefPlayers;

        private ObservableCollection<Player> m_startDefPlayers;

        private Tournament m_tournament;

        private Player m_user;

        public CalcPoint()
        {
            InitializeComponent();

            m_tournament = new Tournament();
            m_startDefPlayers = new ObservableCollection<Player>();
            m_finishDefPlayers = new ObservableCollection<Player>();
            m_allPlayers = new List<Player>();

            SetUp();
        }

        private enum Association { FIS = 0, SAJ = 1 }

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

        private enum ViewCellRowStyle { Height = 46 }

        public void UpdateControl()
        {
            StartTopList.IsVisible = m_startDefPlayers.Count > 0 ? true : false;
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
            else if (AllList.SelectedItem != null)
            {
                selectedPlayer = AllList.SelectedItem as Player;
                if (selectedPlayer != null && TopList.ItemsSource != null)
                {
                    DisplayErrorMessage(ErrorCode.OverUserCount);
                    return;
                }
                var copyPlayer = selectedPlayer.DeepCopy();
                var searchedPlayer = new ObservableCollection<Player>();
                searchedPlayer.Add(copyPlayer);
                SetTopListViewLayout(searchedPlayer, TopList, PlayerEntry);
                TopList.ScrollTo(copyPlayer, ScrollToPosition.Center, true);
            }
        }

        private async void Switch_Share_Toggled(object sender, ToggledEventArgs e)
        {
            var switchToggle = sender as Switch;
            if (switchToggle != null)
            {
                object loginUserId;
                Application.Current.Resources.TryGetValue("LoginUserId", out loginUserId);
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
                double[] penaltyPoints = GetPenaltyPoints();
                if (penaltyPoints != null && !double.IsNaN(penaltyPoints[(int)Association.FIS]) && !double.IsNaN(penaltyPoints[(int)Association.SAJ]))
                {
                    string tournamentName = Entry_TournamentName.Text;
                    string fisPoint = penaltyPoints[(int)Association.FIS].ToString();
                    string sajPoint = penaltyPoints[(int)Association.SAJ].ToString();
                    string userFisPoint = null;
                    string userSajPoint = null;
                    if (Switch_Share.IsToggled)
                    {
                        RegisterTournament(tournamentName, fisPoint, sajPoint);
                    }
                    if (TopList.SelectedItem != null)
                    {
                        var racePoint = GetRacePoint(TopList.SelectedItem as Player, m_finishDefPlayers.OrderBy(player => player.Time).First());
                        if (!double.IsNaN(racePoint))
                        {
                            userFisPoint = (Convert.ToString(fisPoint) + racePoint).ToString();
                            userSajPoint = (Convert.ToString(sajPoint) + racePoint).ToString();
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

        private void RegisterTournament(string tournament_name, string fis_penalty, string saj_penalty)
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
                            var sql = $"INSERT INTO tournaments_table (tournament_name, fis_penarty, saj_penarty, user_id, created_at, updated_at) VALUES('{tournament_name}', '{fis_penalty}', '{saj_penalty}', '{user_id}', '{time}', '{time}');";
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
            var radioButton = sender as RadioButton;
            if (radioButton?.ClassId != null)
            {
                m_tournament.Sex = int.Parse(radioButton.ClassId);
            }
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
            }
            return fValue * targetPlayer.Time / winner.Time - fValue;
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

            if (sender == StartPlayerEntry) allList = StartAllList;
            if (sender == FinishPlayerEntry) allList = FinishAllList;
            if (sender == PlayerEntry) allList = AllList;
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
        }

        private string ReadPointList()
        {
            string strJson = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonResource = assembly.GetManifestResourceNames();
                if (jsonResource != null)
                {
                    var file = assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.PointList.json")));
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
            listView.IsVisible = players.Count > 0 ? true : false;
            listView.ItemsSource = players;
            listView.HeightRequest = players.Count * (int)ViewCellRowStyle.Height;
            listView.SelectedItem = null;
            entry.Text = string.Empty;
            if (entry.Equals(StartPlayerEntry))
            {
                StartPlayerEntry.IsVisible = m_finishDefPlayers.Count() < 5;
            }
            else
            {
                FinishPlayerEntry.IsVisible = m_finishDefPlayers.Count() < 10;
            }
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
        private double SumRacePoint(IEnumerable<Player> listPlayer, Player winner)
        {
            double sumRacePoint = 0;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoint(player, winner);
            }
            return sumRacePoint;
        }

        public override bool Equals(object obj)
        {
            return obj is CalcPoint point &&
                   EqualityComparer<IDispatcher>.Default.Equals(Dispatcher, point.Dispatcher) &&
                   EqualityComparer<object>.Default.Equals(BindingContext, point.BindingContext) &&
                   AutomationId == point.AutomationId &&
                   ClassId == point.ClassId &&
                   EqualityComparer<IList<Effect>>.Default.Equals(Effects, point.Effects) &&
                   Id.Equals(point.Id) &&
                   EqualityComparer<VisualElement>.Default.Equals(ParentView, point.ParentView) &&
                   StyleId == point.StyleId &&
                   EqualityComparer<ReadOnlyCollection<Element>>.Default.Equals(LogicalChildren, point.LogicalChildren) &&
                   EqualityComparer<Element>.Default.Equals(RealParent, point.RealParent) &&
                   EqualityComparer<Element>.Default.Equals(Parent, point.Parent) &&
                   EqualityComparer<IEffectControlProvider>.Default.Equals(EffectControlProvider, point.EffectControlProvider) &&
                   EqualityComparer<IPlatform>.Default.Equals(Platform, point.Platform) &&
                   EqualityComparer<INavigation>.Default.Equals(Navigation, point.Navigation) &&
                   EqualityComparer<Style>.Default.Equals(Style, point.Style) &&
                   EqualityComparer<IList<string>>.Default.Equals(StyleClass, point.StyleClass) &&
                   EqualityComparer<IList<string>>.Default.Equals(@class, point.@class) &&
                   EqualityComparer<NavigationProxy>.Default.Equals(NavigationProxy, point.NavigationProxy) &&
                   EqualityComparer<IVisual>.Default.Equals(Visual, point.Visual) &&
                   FlowDirection == point.FlowDirection &&
                   AnchorX == point.AnchorX &&
                   AnchorY == point.AnchorY &&
                   EqualityComparer<Color>.Default.Equals(BackgroundColor, point.BackgroundColor) &&
                   EqualityComparer<Brush>.Default.Equals(Background, point.Background) &&
                   EqualityComparer<IList<Behavior>>.Default.Equals(Behaviors, point.Behaviors) &&
                   EqualityComparer<Xamarin.Forms.Rectangle>.Default.Equals(Bounds, point.Bounds) &&
                   Height == point.Height &&
                   HeightRequest == point.HeightRequest &&
                   InputTransparent == point.InputTransparent &&
                   IsEnabled == point.IsEnabled &&
                   IsFocused == point.IsFocused &&
                   IsVisible == point.IsVisible &&
                   MinimumHeightRequest == point.MinimumHeightRequest &&
                   MinimumWidthRequest == point.MinimumWidthRequest &&
                   Opacity == point.Opacity &&
                   Rotation == point.Rotation &&
                   RotationX == point.RotationX &&
                   RotationY == point.RotationY &&
                   Scale == point.Scale &&
                   ScaleX == point.ScaleX &&
                   ScaleY == point.ScaleY &&
                   TabIndex == point.TabIndex &&
                   IsTabStop == point.IsTabStop &&
                   TranslationX == point.TranslationX &&
                   TranslationY == point.TranslationY &&
                   EqualityComparer<IList<TriggerBase>>.Default.Equals(Triggers, point.Triggers) &&
                   Width == point.Width &&
                   WidthRequest == point.WidthRequest &&
                   X == point.X &&
                   Y == point.Y &&
                   EqualityComparer<Geometry>.Default.Equals(Clip, point.Clip) &&
                   Batched == point.Batched &&
                   DisableLayout == point.DisableLayout &&
                   IsInNativeLayout == point.IsInNativeLayout &&
                   IsNativeStateConsistent == point.IsNativeStateConsistent &&
                   IsPlatformEnabled == point.IsPlatformEnabled &&
                   EqualityComparer<ResourceDictionary>.Default.Equals(Resources, point.Resources) &&
                   BackgroundImage == point.BackgroundImage &&
                   EqualityComparer<ImageSource>.Default.Equals(BackgroundImageSource, point.BackgroundImageSource) &&
                   EqualityComparer<FileImageSource>.Default.Equals(Icon, point.Icon) &&
                   EqualityComparer<ImageSource>.Default.Equals(IconImageSource, point.IconImageSource) &&
                   IsBusy == point.IsBusy &&
                   EqualityComparer<Thickness>.Default.Equals(Padding, point.Padding) &&
                   Title == point.Title &&
                   EqualityComparer<IList<ToolbarItem>>.Default.Equals(ToolbarItems, point.ToolbarItems) &&
                   EqualityComparer<Xamarin.Forms.Rectangle>.Default.Equals(ContainerArea, point.ContainerArea) &&
                   IgnoresContainerArea == point.IgnoresContainerArea &&
                   EqualityComparer<ObservableCollection<Element>>.Default.Equals(InternalChildren, point.InternalChildren) &&
                   EqualityComparer<ControlTemplate>.Default.Equals(ControlTemplate, point.ControlTemplate) &&
                   EqualityComparer<View>.Default.Equals(Content, point.Content) &&
                   EqualityComparer<List<Player>>.Default.Equals(m_allPlayers, point.m_allPlayers) &&
                   EqualityComparer<ObservableCollection<Player>>.Default.Equals(m_finishDefPlayers, point.m_finishDefPlayers) &&
                   EqualityComparer<ObservableCollection<Player>>.Default.Equals(m_startDefPlayers, point.m_startDefPlayers) &&
                   EqualityComparer<Tournament>.Default.Equals(m_tournament, point.m_tournament) &&
                   EqualityComparer<Player>.Default.Equals(m_user, point.m_user) &&
                   EqualityComparer<Entry>.Default.Equals(StartPlayerEntry, point.StartPlayerEntry) &&
                   EqualityComparer<ListView>.Default.Equals(StartAllList, point.StartAllList) &&
                   EqualityComparer<ListView>.Default.Equals(StartTopList, point.StartTopList) &&
                   EqualityComparer<Entry>.Default.Equals(FinishPlayerEntry, point.FinishPlayerEntry) &&
                   EqualityComparer<ListView>.Default.Equals(FinishAllList, point.FinishAllList) &&
                   EqualityComparer<ListView>.Default.Equals(FinishTopList, point.FinishTopList) &&
                   EqualityComparer<Switch>.Default.Equals(Switch_Share, point.Switch_Share) &&
                   EqualityComparer<Button>.Default.Equals(Btn_Calc, point.Btn_Calc);
        }

        public override int GetHashCode()
        {
            int hashCode = 1525230600;
            hashCode = hashCode * -1521134295 + EqualityComparer<IDispatcher>.Default.GetHashCode(Dispatcher);
            hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(BindingContext);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AutomationId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ClassId);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<Effect>>.Default.GetHashCode(Effects);
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<VisualElement>.Default.GetHashCode(ParentView);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StyleId);
            hashCode = hashCode * -1521134295 + EqualityComparer<ReadOnlyCollection<Element>>.Default.GetHashCode(LogicalChildren);
            hashCode = hashCode * -1521134295 + EqualityComparer<Element>.Default.GetHashCode(RealParent);
            hashCode = hashCode * -1521134295 + EqualityComparer<Element>.Default.GetHashCode(Parent);
            hashCode = hashCode * -1521134295 + EqualityComparer<IEffectControlProvider>.Default.GetHashCode(EffectControlProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPlatform>.Default.GetHashCode(Platform);
            hashCode = hashCode * -1521134295 + EqualityComparer<INavigation>.Default.GetHashCode(Navigation);
            hashCode = hashCode * -1521134295 + EqualityComparer<Style>.Default.GetHashCode(Style);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(StyleClass);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(@class);
            hashCode = hashCode * -1521134295 + EqualityComparer<NavigationProxy>.Default.GetHashCode(NavigationProxy);
            hashCode = hashCode * -1521134295 + EqualityComparer<IVisual>.Default.GetHashCode(Visual);
            hashCode = hashCode * -1521134295 + FlowDirection.GetHashCode();
            hashCode = hashCode * -1521134295 + AnchorX.GetHashCode();
            hashCode = hashCode * -1521134295 + AnchorY.GetHashCode();
            hashCode = hashCode * -1521134295 + BackgroundColor.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Brush>.Default.GetHashCode(Background);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<Behavior>>.Default.GetHashCode(Behaviors);
            hashCode = hashCode * -1521134295 + Bounds.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            hashCode = hashCode * -1521134295 + HeightRequest.GetHashCode();
            hashCode = hashCode * -1521134295 + InputTransparent.GetHashCode();
            hashCode = hashCode * -1521134295 + IsEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 + IsFocused.GetHashCode();
            hashCode = hashCode * -1521134295 + IsVisible.GetHashCode();
            hashCode = hashCode * -1521134295 + MinimumHeightRequest.GetHashCode();
            hashCode = hashCode * -1521134295 + MinimumWidthRequest.GetHashCode();
            hashCode = hashCode * -1521134295 + Opacity.GetHashCode();
            hashCode = hashCode * -1521134295 + Rotation.GetHashCode();
            hashCode = hashCode * -1521134295 + RotationX.GetHashCode();
            hashCode = hashCode * -1521134295 + RotationY.GetHashCode();
            hashCode = hashCode * -1521134295 + Scale.GetHashCode();
            hashCode = hashCode * -1521134295 + ScaleX.GetHashCode();
            hashCode = hashCode * -1521134295 + ScaleY.GetHashCode();
            hashCode = hashCode * -1521134295 + TabIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + IsTabStop.GetHashCode();
            hashCode = hashCode * -1521134295 + TranslationX.GetHashCode();
            hashCode = hashCode * -1521134295 + TranslationY.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<TriggerBase>>.Default.GetHashCode(Triggers);
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + WidthRequest.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Geometry>.Default.GetHashCode(Clip);
            hashCode = hashCode * -1521134295 + Batched.GetHashCode();
            hashCode = hashCode * -1521134295 + DisableLayout.GetHashCode();
            hashCode = hashCode * -1521134295 + IsInNativeLayout.GetHashCode();
            hashCode = hashCode * -1521134295 + IsNativeStateConsistent.GetHashCode();
            hashCode = hashCode * -1521134295 + IsPlatformEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ResourceDictionary>.Default.GetHashCode(Resources);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BackgroundImage);
            hashCode = hashCode * -1521134295 + EqualityComparer<ImageSource>.Default.GetHashCode(BackgroundImageSource);
            hashCode = hashCode * -1521134295 + EqualityComparer<FileImageSource>.Default.GetHashCode(Icon);
            hashCode = hashCode * -1521134295 + EqualityComparer<ImageSource>.Default.GetHashCode(IconImageSource);
            hashCode = hashCode * -1521134295 + IsBusy.GetHashCode();
            hashCode = hashCode * -1521134295 + Padding.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<ToolbarItem>>.Default.GetHashCode(ToolbarItems);
            hashCode = hashCode * -1521134295 + ContainerArea.GetHashCode();
            hashCode = hashCode * -1521134295 + IgnoresContainerArea.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ObservableCollection<Element>>.Default.GetHashCode(InternalChildren);
            hashCode = hashCode * -1521134295 + EqualityComparer<ControlTemplate>.Default.GetHashCode(ControlTemplate);
            hashCode = hashCode * -1521134295 + EqualityComparer<View>.Default.GetHashCode(Content);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Player>>.Default.GetHashCode(m_allPlayers);
            hashCode = hashCode * -1521134295 + EqualityComparer<ObservableCollection<Player>>.Default.GetHashCode(m_finishDefPlayers);
            hashCode = hashCode * -1521134295 + EqualityComparer<ObservableCollection<Player>>.Default.GetHashCode(m_startDefPlayers);
            hashCode = hashCode * -1521134295 + EqualityComparer<Tournament>.Default.GetHashCode(m_tournament);
            hashCode = hashCode * -1521134295 + EqualityComparer<Player>.Default.GetHashCode(m_user);
            hashCode = hashCode * -1521134295 + EqualityComparer<Entry>.Default.GetHashCode(StartPlayerEntry);
            hashCode = hashCode * -1521134295 + EqualityComparer<ListView>.Default.GetHashCode(StartAllList);
            hashCode = hashCode * -1521134295 + EqualityComparer<ListView>.Default.GetHashCode(StartTopList);
            hashCode = hashCode * -1521134295 + EqualityComparer<Entry>.Default.GetHashCode(FinishPlayerEntry);
            hashCode = hashCode * -1521134295 + EqualityComparer<ListView>.Default.GetHashCode(FinishAllList);
            hashCode = hashCode * -1521134295 + EqualityComparer<ListView>.Default.GetHashCode(FinishTopList);
            hashCode = hashCode * -1521134295 + EqualityComparer<Switch>.Default.GetHashCode(Switch_Share);
            hashCode = hashCode * -1521134295 + EqualityComparer<Button>.Default.GetHashCode(Btn_Calc);
            return hashCode;
        }

        public class Player : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string BirthOfDate { get; set; } = string.Empty;

            public string Country { get; set; } = string.Empty;

            public string EnglishName { get; set; } = string.Empty;

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

            public string FisNo { get; set; } = string.Empty;

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

            public string JapaneseName { get; set; } = string.Empty;

            public string KanaName { get; set; } = string.Empty;

            public string Prefecture { get; set; } = string.Empty;

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

            public string SajNo { get; set; } = string.Empty;

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

            public string strFisDh { get; set; } = string.Empty;

            public string strFisGs { get; set; } = string.Empty;

            public string strFisSc { get; set; } = string.Empty;

            public string strFisSg { get; set; } = string.Empty;

            public string strFisSl { get; set; } = string.Empty;

            public string strSajDh { get; set; } = string.Empty;

            public string strSajGs { get; set; } = string.Empty;

            public string strSajSc { get; set; } = string.Empty;

            public string strSajSg { get; set; } = string.Empty;

            public string strSajSl { get; set; } = string.Empty;

            public string TeamName { get; set; } = string.Empty;

            public double Time { get; set; } = 120.00;

            public Player DeepCopy()
            {
                var player = MemberwiseClone() as Player;
                return player;
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