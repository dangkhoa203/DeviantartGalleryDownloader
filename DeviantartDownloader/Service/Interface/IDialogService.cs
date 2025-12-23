using DeviantartDownloader.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Service.Interface
{
    public interface IDialogService
    {
        TViewModel? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : ViewModel;
    }
}
