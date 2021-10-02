using PointApp.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace PointApp.Views
{
	public partial class ItemDetailPage : ContentPage
	{
		public ItemDetailPage()
		{
			InitializeComponent();
			BindingContext = new ItemDetailViewModel();
		}
	}
}