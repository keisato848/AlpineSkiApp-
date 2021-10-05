using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PointApp.Views
{
	public partial class CalcPoint : ContentPage
	{
		public enum PlayersCount { DEFAULT = 5 }
		public Tournament m_tournament;
		public Player m_user;
		public ObservableCollection<Player> m_startDefPlayers;
		public ObservableCollection<Player> m_finishDefPlayers;
		private bool m_isEditing { get; set; }
		public CalcPoint()
		{
			InitializeComponent();
			SetUp();
		}

		public void SetUp()
        {
			m_tournament = new Tournament();
			m_startDefPlayers = new ObservableCollection<Player>();
			m_finishDefPlayers = new ObservableCollection<Player>();
			m_isEditing = false;
			StartTopList.ItemsSource = m_startDefPlayers;
		}
		public void UpdateControl()
        {
			if (m_isEditing)
            {
				PlayerEntry.IsVisible = true;
				ButtonAdd.IsVisible = false;
            }
            else
            {
				PlayerEntry.IsVisible = false;
				ButtonAdd.IsVisible = true;

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
			private int Idx { get; set; }
			private string _Name { get; set; }
			public string Name
			{
				get => _Name;
				set
				{
					_Name = value;
					NotifyPropertyChanged();
				}
			}
			public string DisplayName 
			{
				get { return Idx.ToString() + "人目：" + Name; }
			}
			public DateTime Time { get; set; }
			public double FisDh { get; set; }
			public double SajDh { get; set; }
			public double FisSg { get; set; }
			public double SajSg { get; set; }
			public double FisGs { get; set; }
			public double SajGs { get; set; }
			public double FisSl { get; set; }
			public double SajSl { get; set; }
			public Player()
			{
				Name  = string.Empty;
				Time  = DateTime.MinValue;
				FisDh = double.MinValue;
				SajDh = double.MinValue;
				FisSg = double.MinValue;
				SajSg = double.MinValue;
				FisGs = double.MinValue;
				SajGs = double.MinValue;
				FisSl = double.MinValue;
				SajSl = double.MinValue;
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
			if (e.Value is false)
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
			addedPlayer.Name = entry.Text;
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