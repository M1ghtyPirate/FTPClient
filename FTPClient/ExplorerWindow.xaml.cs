using FTPClient.Models;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
	/// Interaction logic for ExplorerWindow.xaml
	/// </summary>
	public partial class ExplorerWindow : Window, INotifyPropertyChanged {

		#region Properties

		/// <summary>
		/// Адрес FTP сервера
		/// </summary>
		private readonly string BaseUri;

		private string _currentHostPath;

		/// <summary>
		/// Текущий путь на сервере
		/// </summary>
		public string CurrentHostPath { get => _currentHostPath; set { _currentHostPath = value; OnPropertyChanged(); } }

		private string _currentLocalPath;

		/// <summary>
		/// Текущий локальный путь
		/// </summary>
		private string CurrentLocalPath { get => _currentLocalPath; set { _currentLocalPath = value; OnPropertyChanged(); } }

		/// <summary>
		/// Креды
		/// </summary>
		private readonly NetworkCredential Credentials;

		public event PropertyChangedEventHandler? PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

		public ExplorerWindow(string baseUri, NetworkCredential credentials) {
			InitializeComponent();
			BaseUri = baseUri;
			Credentials = credentials;
			CurrentHostPath = baseUri;
			this.DataContext = this;
			updateHostItems();
		}



		#region Eventhandlers

		private void StopButton_Click(object sender, RoutedEventArgs e) {
			var hostAddressWindow = new HostAddressWindow();
			hostAddressWindow.Show();
			this.Close();
		}

		#endregion

		#region Methods

		private FtpWebRequest getFTPWebRequest(string uri, string method) {
			var ftpWebRequest = (FtpWebRequest)System.Net.FtpWebRequest.Create(uri);
			ftpWebRequest.Credentials = Credentials;
			ftpWebRequest.Method = method;
			return ftpWebRequest;
		}

		private void updateHostItems() {
			if (this.HostItemsListView == null) {
				return;
			}

			var ftpWebRequest = getFTPWebRequest(CurrentHostPath, WebRequestMethods.Ftp.ListDirectoryDetails);
			var dirList = new List<string>();
			try {
				using (var dirListResponse = ftpWebRequest.GetResponse())
				using (var dirListStream = dirListResponse.GetResponseStream())
				using (var dirListReader = new StreamReader(dirListStream)) {
					while (!dirListReader.EndOfStream) {
						dirList.Add(dirListReader.ReadLine());
					}
				}
			} catch {
				Helper.ShowMessage($"Не удалось запросить список папок.", this);
				return;
			}

			this.HostItemsListView.Items.Clear();
			
			if (CurrentHostPath != BaseUri) {
				var parentDirectoryItem = new ListViewItem() {
					Content = "..",
					Tag = new HostItem(),
					Background = Brushes.Blue
				};
				parentDirectoryItem.MouseDoubleClick += (s, e) => changeHostDirectory(CurrentHostPath.Substring(0, CurrentHostPath.LastIndexOf("/")));
				this.HostItemsListView.Items.Add(parentDirectoryItem);
			}

			var listViewItems = new List<ListViewItem>();
			foreach (var dir in dirList) { 
				var hostItem = Helper.ParseHostItem(dir);
				if (hostItem.Name == "." || hostItem.Name == "..") {
					continue;
				}
				var listViewItem = new ListViewItem() { 
					Content = hostItem.Name,
					Tag = hostItem,
				};
				if (hostItem.IsDirectory) {
					listViewItem.Background = Brushes.Cyan;
					listViewItem.MouseDoubleClick += (s, e) => changeHostDirectory($"{CurrentHostPath}/{hostItem.Name}");
				}
				listViewItems.Add(listViewItem);
			}

			foreach (var listViewItem in listViewItems.Where(i => (i.Tag as Models.HostItem)?.IsDirectory ?? false).OrderBy(i => (i.Tag as Models.HostItem)?.Name)) {
				this.HostItemsListView.Items.Add(listViewItem);
			}

			foreach (var listViewItem in listViewItems.Where(i => !(i.Tag as Models.HostItem)?.IsDirectory ?? false).OrderBy(i => (i.Tag as Models.HostItem)?.Name)) {
				this.HostItemsListView.Items.Add(listViewItem);
			}
		}

		private void changeHostDirectory(string absolutePath) {
			CurrentHostPath = absolutePath;
			updateHostItems();
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e) {
			//CurrentHostPath += "/1/";
		}

		#endregion
	}
}
