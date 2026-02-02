using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DeviantartDownloader.Views {
    /// <summary>
    /// Interaction logic for SettingView.xaml
    /// </summary>
    public partial class SettingView : MetroWindow {
        public SettingView() {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {

            Regex regex = new Regex("[^1-5]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_PreviewTextInput1(object sender, TextCompositionEventArgs e) {

            Regex regex = new Regex("[^0-5]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Space) {
                e.Handled = true;
            }
        }
    }
}
