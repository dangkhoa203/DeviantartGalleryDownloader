using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using DeviantartDownloader.ViewModels;
using DeviantartDownloader.Views;
using MahApps.Metro.Controls.Dialogs;
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
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<GetGalleryViewModel>();
            services.AddSingleton<KeySettingViewModel>();
            services.AddSingleton<SettingViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<DeviantartService>();
            services.AddSingleton<IDialogCoordinator, DialogCoordinator>();
        }
        public App() {
            IServiceCollection services = new ServiceCollection();
            ConfigureService(services);
            _serviceProvider = services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e) {
            var mainWindows = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindows.Show();
            base.OnStartup(e);
        }
        private void OnExit(object sender, ExitEventArgs e) {
            // Dispose of services if needed
            if (_serviceProvider is IDisposable disposable) {
                disposable.Dispose();
            }
        }
    }

}
