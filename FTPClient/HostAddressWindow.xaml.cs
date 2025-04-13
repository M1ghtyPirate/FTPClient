using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace FTPClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    #region Eventhandlers

    private void HostAddressTextBox_TextChanged(object sender, RoutedEventArgs e) {
        updateButtonAvailiability();
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e) {
        connectToHost();
    }

    private void ConnectButton_Loaded(object sender, RoutedEventArgs e) {
        updateButtonAvailiability();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Обновление доступности кнопки подключения
    /// </summary>
    private void updateButtonAvailiability() {
        if (this.HostAddressTextBox == null || this.ConnectButton == null) {
            return;
        }

        this.ConnectButton.IsEnabled = this.HostAddressTextBox.Text?.Trim().Any() ?? false;
    }

    /// <summary>
    /// Подключение к FTP серверу
    /// </summary>
    /// <returns></returns>
    private void connectToHost() {
        var hostAddress = this.HostAddressTextBox?.Text?.Trim();
        if (!(hostAddress?.Any() ?? false)) {
            return;
        }

        FtpWebRequest ftpWebRequest = null;
        try {
            var uri = new Uri(hostAddress);
            if (uri.Scheme != Uri.UriSchemeFtp) {
                throw new Exception();
            }
            var baseUri = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
            ftpWebRequest = (FtpWebRequest)System.Net.FtpWebRequest.Create(baseUri);
        } catch {
            Helper.showMessage($"Не удалось создать подключение к FTP серверу <{hostAddress}>", this);
        }
        
        if (ftpWebRequest == null) {
            return;
        }

        var loginWindow = new LoginWindow();
        loginWindow.Owner = this;
        var result = loginWindow.ShowDialog();
        if (!(result ?? false)) {
            return;
        }

        var login = loginWindow.LoginTextBox.Text.Trim();
        var password = loginWindow.PasswordPasswordBox.Password;
        var credentials = new NetworkCredential(login, password);
        ftpWebRequest.Credentials = credentials;
        ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
        try {
            var dirList = new List<string>();
            using (var dirListResponse = ftpWebRequest.GetResponse())
            using (var dirListStream = dirListResponse.GetResponseStream())
            using (var dirListReader = new StreamReader(dirListStream)) {
                while (!dirListReader.EndOfStream) {
                    dirList.Add(dirListReader.ReadLine());
                }
            }
            Helper.showMessage(string.Join('\n', dirList), this);
        } catch {
            Helper.showMessage($"Не удалось запросить список папок.", this);
            return;
        }
        return;
    }

    #endregion
}