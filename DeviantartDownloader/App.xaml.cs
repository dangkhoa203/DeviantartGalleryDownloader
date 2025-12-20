using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using DeviantartDownloader.ViewModels;
using DeviantartDownloader.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Navigation;

namespace DeviantartDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        private void ConfigureService(IServiceCollection services) {
            services.AddSingleton<IDialogService, DialogService>();
        }
        public App() {
            IServiceCollection services = new ServiceCollection();
            ConfigureService(services);
            _serviceProvider = services.BuildServiceProvider();
        }
    }

}
