using ofline_telemitry.Models;
using ofline_telemitry.PageModels;

namespace ofline_telemitry.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}