using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FTPClient {
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();
        }

        #region Eventhandlers

        private void LoginTextBox_TextChanged(object sender, RoutedEventArgs e) {
            updateButtonAvailiability();
        }

        private void PasswordPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            updateButtonAvailiability();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
            this.Close();
        }

        private void LoginButton_Loaded(object sender, RoutedEventArgs e) {
            updateButtonAvailiability();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Обновление доступности кнопки входа
        /// </summary>
        private void updateButtonAvailiability() {
            if (this.LoginTextBox == null || this.PasswordPasswordBox == null || this.LoginButton == null) {
                return;
            }

            this.LoginButton.IsEnabled = (this.LoginTextBox.Text?.Trim().Any() ?? false)
                && (this.PasswordPasswordBox.Password?.Any() ?? false);
        }

        #endregion

    }
}