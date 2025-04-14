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
		public string CurrentLocalPath { get => _currentLocalPath; set { _currentLocalPath = value; OnPropertyChanged(); } }

		/// <summary>
		/// Креды
		/// </summary>
		private readonly NetworkCredential Credentials;

		public event PropertyChangedEventHandler? PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private Brush DirectoryItemBackground = Brushes.LightGray;

		private Brush ParentDirectoryItemBackground = Brushes.DarkGray;

		#endregion

		public ExplorerWindow(string baseUri, NetworkCredential credentials) {
			InitializeComponent();
			BaseUri = baseUri;
			Credentials = credentials;
			CurrentHostPath = baseUri;
			CurrentLocalPath = null;
			this.DataContext = this;
			updateHostItems();
			updateLocalItems();
		}



		#region Eventhandlers

		private void StopButton_Click(object sender, RoutedEventArgs e) {
			var hostAddressWindow = new HostAddressWindow();
			hostAddressWindow.Show();
			this.Close();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Формирование FTP запроса
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		private FtpWebRequest getFTPWebRequest(string uri, string method) {
			var ftpWebRequest = (FtpWebRequest)System.Net.FtpWebRequest.Create(uri);
			ftpWebRequest.Credentials = Credentials;
			ftpWebRequest.Method = method;
			return ftpWebRequest;
		}

		/// <summary>
		/// ОБновления содержимого текущего каталога сервера
		/// </summary>
		/// <returns></returns>
		private bool updateHostItems() {
			if (this.HostItemsListView == null) {
				return false;
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
				return false;
			}

			this.HostItemsListView.Items.Clear();
			
			if (CurrentHostPath != BaseUri) {
				var parentDirectoryItem = new ListViewItem() {
					Content = "..",
					Tag = new HostItem(),
					Background = ParentDirectoryItemBackground
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
					listViewItem.Background = DirectoryItemBackground;
					listViewItem.MouseDoubleClick += (s, e) => changeHostDirectory($"{CurrentHostPath}/{((HostItem)((ListViewItem)s).Tag).Name}");
				}
				listViewItems.Add(listViewItem);
			}

			foreach (var listViewItem in listViewItems.Where(i => (i.Tag as Models.HostItem)?.IsDirectory ?? false).OrderBy(i => (i.Tag as Models.HostItem)?.Name)) {
				this.HostItemsListView.Items.Add(listViewItem);
			}

			foreach (var listViewItem in listViewItems.Where(i => !(i.Tag as Models.HostItem)?.IsDirectory ?? false).OrderBy(i => (i.Tag as Models.HostItem)?.Name)) {
				this.HostItemsListView.Items.Add(listViewItem);
			}
			return true;
		}

		private bool setWindowsRootItems() {
			if (this.LocalItemsListView == null) {
				return false;
			}

			var drives = DriveInfo.GetDrives();

			this.LocalItemsListView.Items.Clear();
			foreach (var drive in drives) {
				var driveItem = new ListViewItem() {
					Content = $"{drive.VolumeLabel ?? ""} ({drive.Name})",
					Tag = drive.Name,
					Background = DirectoryItemBackground
				};
				driveItem.MouseDoubleClick += (s, e) => changeLocalDirectory((string)((ListViewItem)s).Tag);
				this.LocalItemsListView.Items.Add(driveItem);
			}

			return true;
		}

		/// <summary>
		/// ОБновления содержимого текущего каталога сервера
		/// </summary>
		/// <returns></returns>
		private bool updateLocalItems() {
			if (this.LocalItemsListView == null) {
				return false;
			}
			
			if (string.IsNullOrEmpty(CurrentLocalPath)) {
				return setWindowsRootItems();
			}

			DirectoryInfo currentDirectory = null;
			var directories = new List<DirectoryInfo>();
			var files = new List<FileInfo>();
			try {
				currentDirectory = new DirectoryInfo(CurrentLocalPath);
				directories.AddRange(currentDirectory.GetDirectories() ?? Array.Empty<DirectoryInfo>());
				files.AddRange(currentDirectory.GetFiles() ?? Array.Empty<FileInfo>());
			} catch {
				Helper.ShowMessage($"Не перейти в папку.", this);
				return false;
			}
			
			LocalItemsListView.Items.Clear();
			var parentDirectoryItem = new ListViewItem() {
				Content = "..",
				Tag = currentDirectory.Parent,
				Background = ParentDirectoryItemBackground
			};
			parentDirectoryItem.MouseDoubleClick += (s, e) => changeLocalDirectory(((DirectoryInfo)((ListViewItem)s).Tag)?.FullName);
			this.LocalItemsListView.Items.Add(parentDirectoryItem);
			foreach (var directory in directories.OrderBy(d => d.Name)) {
				var listViewItem = new ListViewItem() {
					Content = directory.Name,
					Tag = directory,
					Background = DirectoryItemBackground
				};
				listViewItem.MouseDoubleClick += (s, e) => changeLocalDirectory(((DirectoryInfo)((ListViewItem)s).Tag)?.FullName);
				this.LocalItemsListView.Items.Add(listViewItem);
				
			}
			foreach (var file in files.OrderBy(f => f.Name)) {
				this.LocalItemsListView.Items.Add(new ListViewItem() {
					Content = file.Name,
					Tag = file,
				});
			}

			return true;
		}

		/// <summary>
		/// Переход в каталог сервера
		/// </summary>
		/// <param name="absolutePath"></param>
		private void changeHostDirectory(string absolutePath) {
			var oldPath = CurrentHostPath;
			CurrentHostPath = absolutePath;
			if (!updateHostItems()) {
				CurrentHostPath = oldPath;
			}
		}

		/// <summary>
		/// Переход в локальный каталог
		/// </summary>
		/// <param name="absolutePath"></param>
		private void changeLocalDirectory(string absolutePath) {
			var oldPath = CurrentLocalPath;
			CurrentLocalPath = absolutePath;
			if (!updateLocalItems()) {
				CurrentLocalPath = oldPath;
			}
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e) {
			//CurrentHostPath += "/1/";
		}

		#endregion
	}
}
