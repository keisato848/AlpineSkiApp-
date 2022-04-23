using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using PointApp.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Reflection;


namespace PointApp.Utilities
{
    public class PointUtility
    {
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
            DH = 330
        }

        public static ObservableCollection<PlayerInfo> GetPlayersFromKana(string kana, List<PlayerInfo> players)
        {
            return new ObservableCollection<PlayerInfo>(from player in players
                                                        where player.KanaName.StartsWith(kana)
                                                        orderby player.FisGs
                                                        select player);
        }

        public static ObservableCollection<PlayerInfo> GetPlayersFromKanji(string kanji, List<PlayerInfo> players)
        {
            return new ObservableCollection<PlayerInfo>(from player in players
                                                        where player.JapaneseName.StartsWith(kanji)
                                                        select player);
        }

        private static double? CalcPenaltyPoint(CompetitionInfo.EventType type, double sumFinishFivePenaltyPoint, double sumStartFivePenaltyPoint, double sumFinishFiveRacePoint)
        {
            //decimal def = decimal.Add(Convert.ToDecimal(sumFinishFivePenaltyPoint), Convert.ToDecimal(sumStartFivePenaltyPoint));
            //decimal def2 = decimal.Subtract(def, Convert.ToDecimal(sumFinishFiveRacePoint));

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

        public static string ConvKanaFromHiragana(string str)
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

        private static double GetPoint(CompetitionInfo.EventType type, bool isFIS, PlayerInfo player)
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

        public static double GetRacePoint(CompetitionInfo.EventType type, PlayerInfo targetPlayer, PlayerInfo winner)
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

        public static bool IsHiragana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsHiragana}*$");
        }

        public static bool IsKatakana(string str)
        {
            return Regex.IsMatch(str, @"^\p{IsKatakana}*$");
        }

        private static List<PlayerInfo> OrderByPoint(CompetitionInfo.EventType type, bool isFisPoint, IEnumerable<PlayerInfo> listPlayerInfos)
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

        private static List<PlayerInfo> OrderByTime(IEnumerable<PlayerInfo> listPlayerInfos)
        {
            if (listPlayerInfos == null)
            {
                return null;
            }
            return listPlayerInfos.Where(playerInfo => playerInfo.Time != null).OrderBy(playerInfo => playerInfo.Time).ToList();
        }

        public static (double?, double?) GetPenaltyPoints(CompetitionInfo.EventType type, List<PlayerInfo> startDefPlayers, List<PlayerInfo> finishDefPlayers)
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

        private static string ReadPointList(CompetitionInfo.SexType sexType)
        {
            string strJson = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonResource = assembly.GetManifestResourceNames();
                if (jsonResource != null)
                {
                    var file = sexType == CompetitionInfo.SexType.Men
                        ? assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.PointList_M.json")))
                        : assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.PointList_L.json")));
                    if (file != null)
                    {
                        using var sr = new System.IO.StreamReader(file);
                        strJson = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return strJson;
        }

        private static void RemoveEmptyInName(List<PlayerInfo> listPlayer)
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


        public static List<PlayerInfo> GetPointList(CompetitionInfo.SexType sexType)
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

        private static double GetSumPoints(CompetitionInfo.EventType type, bool isFIS, IEnumerable<PlayerInfo> listPlayer)
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

        private static double SumRacePoint(CompetitionInfo.EventType type, IEnumerable<PlayerInfo> listPlayer, PlayerInfo winner)
        {
            double sumRacePoint = 0;
            foreach (var player in listPlayer)
            {
                sumRacePoint += GetRacePoint(type, player, winner);
            }
            return sumRacePoint;
        }
    }
}
