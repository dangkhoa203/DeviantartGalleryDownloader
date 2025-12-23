using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DeviantartDownloader.Views
{
    /// <summary>
    /// Interaction logic for GetGallery.xaml
    /// </summary>
    public partial class GetGalleryView : Window
    {
        public GetGalleryView()
        {
            InitializeComponent();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Btn_Submit_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
