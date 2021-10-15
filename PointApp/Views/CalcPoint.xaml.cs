using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xamarin.Forms;



namespace PointApp.Views
{
	public partial class CalcPoint : ContentPage
	{
		public enum PlayersCount { DEFAULT = 5 }
		public Tournament m_tournament;
		public Player m_user;
		public ObservableCollection<Player> m_startDefPlayers;
		public ObservableCollection<Player> m_finishDefPlayers;
		public static List<Player>  m_allPlayers;
		private bool m_isEditing { get; set; }

		public CalcPoint()
		{
			InitializeComponent();
			SetUp();
		}

		public void SetUp()
        {
			m_tournament       = new Tournament();
			m_startDefPlayers  = new ObservableCollection<Player>();
			m_finishDefPlayers = new ObservableCollection<Player>();
			m_allPlayers       = new List<Player>();
			m_isEditing        = false;

			var strjson = ReadPointList();
			if (strjson is string)
			{
				var listPlayers = JsonSerializer.Deserialize<List<Player>>(strjson);
				if (listPlayers is List<Player>)
                {
					m_allPlayers = listPlayers;
                }
			}
			//StartTopList.ItemsSource = m_startDefPlayers;
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
		public void UpdateControl()
        {
			if (m_isEditing)
            {
				PlayerEntry.IsVisible = true;
				ButtonAdd.IsVisible   = false;
            }
            else
            {
				PlayerEntry.IsVisible = false;
				ButtonAdd.IsVisible   = true;

			}
			StartTopList.IsVisible = m_startDefPlayers.Count > 0 ? true : false;
		}

		public class Player : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
			
			public  string SajNo { get; set; }
			public  string FisNo { get; set; }
			public string EnglishName { get; set; }
			public string JapaneseName { get; set; }
			public  string Country { get; set; }
			public  string Prefecture { get; set; }
			public  DateTime Time { get; set; }
			public  string FisDh { get; set; }
			public  string SajDh { get; set; }
			public  string FisSg { get; set; }
			public  string SajSg { get; set; }
			public  string FisSc { get; set; }
			public  string SajSc { get; set; }
			public  string FisGs { get; set; }
			public  string SajGs { get; set; }
			public  string FisSl { get; set; }
			public  string SajSl { get; set; }
			public  string TeamName { get; set; }
			public  string  BirthOfDate { get; set; }
			public  string KanaName { get; set; }
			public Player()
			{
				SajNo         = string.Empty;
				FisNo         = string.Empty;
				EnglishName   = string.Empty;
				JapaneseName  = string.Empty;
				Country       = string.Empty;
				Prefecture    = string.Empty;
				Time          = DateTime.MinValue;
				FisDh         = string.Empty;
				SajDh         = string.Empty;
				FisSg         = string.Empty;
				SajSg         = string.Empty;
				FisSc         = string.Empty;
				SajSc         = string.Empty;
				FisGs         = string.Empty;
				SajGs         = string.Empty;
				FisSl         = string.Empty;
				SajSl         = string.Empty;
				TeamName      = string.Empty;
				BirthOfDate   = string.Empty;
				KanaName      = string.Empty;
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
				Types     = EventTypes.NONE;
				HeightDh  = int.MinValue;
				HeightSg  = int.MinValue;
				HeightGs  = int.MinValue;
				HeightSl  = int.MinValue;
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
        }

        private void Entry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
			// インクリメンタルサーチ
		}

        private void Entry_Completed(object sender, EventArgs e)
        {
			var entry = sender as Entry;
			if (entry is null || entry.Text is null)
			{
				return;
			}
			Player addedPlayer = m_startDefPlayers[m_startDefPlayers.Count - 1];
			addedPlayer.JapaneseName = entry.Text;
			m_isEditing = false;
			UpdateControl();
		}

        private void ButtonAdd_Clicked(object sender, EventArgs e)
        {
			m_isEditing = true;
			UpdateControl();
			m_startDefPlayers.Add(new Player());
        }
	}
}