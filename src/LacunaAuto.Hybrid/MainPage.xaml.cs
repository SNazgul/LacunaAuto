namespace LacunaAuto.Hybrid
{
    using Microsoft.AspNetCore.Components.WebView.Maui;
    using LacunaAuto.Hybrid.Components;

    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            blazorWebView.RootComponents.Add(new RootComponent()
            {
                Selector = "#app",
                ComponentType = typeof(Routes)
            });
        }
    }
}
