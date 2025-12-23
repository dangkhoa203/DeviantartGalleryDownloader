using DeviantartDownloader.Service.Interface;
using DeviantartDownloader.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.Service
{
    public class DialogService : IDialogService {
        public TViewModel? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : ViewModel {
            // 1. Map ViewModel to View (often via a naming convention or dictionary)
            Window window = CreateViewForViewModel(viewModel);

            // 2. Set DataContext to the provided ViewModel
            window.DataContext = viewModel;

            // 3. Show as a modal dialog
            window.ShowDialog();
            return viewModel;
        }

        private Window CreateViewForViewModel(object viewModel) {
            // Example: If VM is 'UserEditViewModel', look for 'UserEditWindow'
            string viewName = viewModel.GetType().FullName.Replace("ViewModel", "View");
            Type viewType = Type.GetType(viewName);
            
            return (Window)Activator.CreateInstance(viewType);
        }
    }
}
