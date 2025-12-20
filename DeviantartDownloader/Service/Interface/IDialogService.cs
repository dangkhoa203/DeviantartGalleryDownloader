using DeviantartDownloader.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Service.Interface
{
    interface IDialogService
    {
        bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : ViewModel;
    }
}
