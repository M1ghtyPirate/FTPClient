using FTPClient.Models;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
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

		private string _workingDirectoryPath;

		/// <summary>
		/// Рабочая директория
		/// </summary>
		public string WorkingDirectoryPath { get => _workingDirectoryPath; set { _workingDirectoryPath = value; OnPropertyChanged(); } }

		/// <summary>
		/// На сервере выбрана дочерняя папка рабочей директории
		/// </summary>
		private bool IsInWorkingDirectory { get => CurrentHostPath?.StartsWith(WorkingDirectoryPath ?? "") ?? false; }

		/// <summary>
		/// Выбранный элемент
		/// </summary>
		public ListViewItem SelectedItem { get; set; }

		/// <summary>
		/// Креды
		/// </summary>
		private readonly NetworkCredential Credentials;

		private bool _canCopy;

		/// <summary>
		/// Доступно ли копирование
		/// </summary>
		public bool CanCopy { get => _canCopy; set { _canCopy = value; OnPropertyChanged(); } }

		private Visibility _loadingVisibility;

		/// <summary>
		/// Видимость загрузочного оверлея
		/// </summary>
		public Visibility LoadingOverlayVisibility { get => _loadingVisibility; set { _loadingVisibility = value; OnPropertyChanged(); } }

		public event PropertyChangedEventHandler? PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private Brush DirectoryItemBackground = Brushes.LightGray;

		private Brush ParentDirectoryItemBackground = Brushes.DarkGray;

		#endregion

		public ExplorerWindow(string baseUri, NetworkCredential credentials) {
			InitializeComponent();
			LoadingOverlayVisibility = Visibility.Collapsed;
			BaseUri = baseUri;
			Credentials = credentials;
			CurrentHostPath = baseUri;
			CurrentLocalPath = null;
			WorkingDirectoryPath = $"{baseUri}/Z3440_Совков В. В.";
			this.DataContext = this;
			updateHostItems();
			updateLocalItems();
		}

		#region Eventhandlers

		private void StopButton_Click(object sender, RoutedEventArgs e) {
			if (!Helper.ShowMessage("Завершить работу?", this, true)) {
				return;
			}

			var hostAddressWindow = new HostAddressWindow();
			hostAddressWindow.Show();
			this.Close();
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e) {
			copyFile();
		}

		private void HostItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var selectedListViewItem = (ListViewItem)(e.AddedItems?.Count == 1 ? e.AddedItems[0] : null);
			if (selectedListViewItem != null) {
				this.LocalItemsListView?.UnselectAll();
				updateItemSelection(selectedListViewItem);
			}
		}

		private void LocalItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var selectedListViewItem = (ListViewItem)(e.AddedItems?.Count == 1 ? e.AddedItems[0] : null);
			if (selectedListViewItem != null) {
				this.HostItemsListView?.UnselectAll();
				updateItemSelection(selectedListViewItem);
			}
		}

		private void CreateWorkingDirectoryButton_Click(object sender, RoutedEventArgs e) {
			createWorkingDirectory();
		}

		private void DeleteWorkingDirectoryButton_Click(object sender, RoutedEventArgs e) {
			removeWorkingDirectory();
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
		/// Выполнение запроса со считыванием строк ответа
		/// </summary>
		/// <param name="ftpWebRequest"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private List<string> executeFTPWebRequest(FtpWebRequest ftpWebRequest, out bool result) {
			var responseList = new List<string>();
			result = true;
			try {
				using (var response = ftpWebRequest.GetResponse())
				using (var stream = response.GetResponseStream())
				using (var reader = new StreamReader(stream)) {
					var test = ((FtpWebResponse)response).StatusDescription;
					while (!reader.EndOfStream) {
						responseList.Add(reader.ReadLine());
					}
				}
			} catch (Exception ex) {
				result = false;
			}
			return responseList;
		}

		/// <summary>
		/// Обновления содержимого текущего каталога сервера
		/// </summary>
		/// <returns></returns>
		private bool updateHostItems() {
			if (this.HostItemsListView == null) {
				return false;
			}

			var ftpWebRequest = getFTPWebRequest(CurrentHostPath, WebRequestMethods.Ftp.ListDirectoryDetails);
			var dirList = executeFTPWebRequest(ftpWebRequest, out var result);
			if (!result) {
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
				if (hostItem.IsSystemNavigationItem) {
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

		/// <summary>
		/// Отображение локальных дисков
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Обновление выбранного элемента
		/// </summary>
		private void updateItemSelection(ListViewItem item) {
			SelectedItem = item;
			if (item?.Tag is HostItem) {
				
				var canWriteToLocalDirectory = false;
				if (!string.IsNullOrEmpty(CurrentLocalPath)) {
					var currentIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
					var localDirectory = new DirectoryInfo(CurrentLocalPath);
					var localDirectoryACL = localDirectory.GetAccessControl(System.Security.AccessControl.AccessControlSections.Access);
					var localDirectoryRules = localDirectoryACL.GetAccessRules(true, true, typeof(NTAccount));
					var writeGranted = false;
					var writeDenied = false;
					foreach (AuthorizationRule localDirectoryRule in localDirectoryRules) {
						if (currentIdentity.User.Translate(localDirectoryRule.IdentityReference.GetType()) == localDirectoryRule.IdentityReference 
							|| currentIdentity.Groups.Any(g => g.Translate(localDirectoryRule.IdentityReference.GetType()) == localDirectoryRule.IdentityReference)) {
							var fileSystemAccessRule = (FileSystemAccessRule)localDirectoryRule;
							if ((fileSystemAccessRule.FileSystemRights & FileSystemRights.WriteData) == 0) {
								continue;
							}
							writeGranted |= fileSystemAccessRule.AccessControlType == AccessControlType.Allow;
							writeDenied |= fileSystemAccessRule.AccessControlType == AccessControlType.Deny;
						}
					}
					canWriteToLocalDirectory = writeGranted && !writeDenied;
				}
				CanCopy = !(item.Tag as HostItem).IsDirectory && canWriteToLocalDirectory && IsInWorkingDirectory;
			} else if (item?.Tag is DirectoryInfo) {
				CanCopy = false;
			} else if (item?.Tag is FileInfo) {
				CanCopy = IsInWorkingDirectory;
			} else {
				CanCopy = false;
			}
		}

		/// <summary>
		/// Проверка существования пути на сервере
		/// </summary>
		/// <param name="absolutePath"></param>
		/// <returns></returns>
		private bool checkIfHostPathExists(string absolutePath) {
			var ftpWebRequest = getFTPWebRequest(absolutePath, WebRequestMethods.Ftp.ListDirectory);
			var dirList = executeFTPWebRequest(ftpWebRequest, out var result);
			return dirList?.Any() ?? false;
		}

		/// <summary>
		/// Удаление файла
		/// </summary>
		/// <param name="absolutePath"></param>
		/// <returns></returns>
		private bool deleteHostFile(string absolutePath) {
			var fileDeleteRequest = getFTPWebRequest(absolutePath, WebRequestMethods.Ftp.DeleteFile);
			var fileDeleteList = executeFTPWebRequest(fileDeleteRequest, out var fileDeleteResult);
			return fileDeleteResult;
		}

		/// <summary>
		/// Удаление директории
		/// </summary>
		/// <param name="absolutePath"></param>
		/// <param name="recurce"></param>
		/// <returns></returns>
		private bool removeHostDirectory(string absolutePath, bool recurce = false) {
			var result = true;
			if (recurce) {
				var dirListRequest = getFTPWebRequest(absolutePath, WebRequestMethods.Ftp.ListDirectoryDetails);
				var dirList = executeFTPWebRequest(dirListRequest, out var dirListResult);
				if (!dirListResult) {
					return false;
				}
				var dirItems = dirList?.Select(s => Helper.ParseHostItem(s)).Where(i => !i.IsSystemNavigationItem) ?? Array.Empty<HostItem>();
				foreach (var dirItem in dirItems) {
					if (dirItem.IsDirectory) {
						result &= removeHostDirectory($"{absolutePath}/{dirItem.Name}", true);
					} else {
						result &= deleteHostFile(absolutePath);
					}
				}
			}
			var deleteRequest = getFTPWebRequest(absolutePath, WebRequestMethods.Ftp.RemoveDirectory);
			executeFTPWebRequest(deleteRequest, out var removeResult);
			result &= removeResult;
			return result;
		}

		/// <summary>
		/// Копирование файлов
		/// </summary>
		/// <returns></returns>
		private bool copyFile() {
			// Копирование с сервера
			if (SelectedItem?.Tag is HostItem) {
				var hostItem = (HostItem)SelectedItem.Tag;
				var localFilePath = System.IO.Path.Combine(CurrentLocalPath, hostItem.Name);
				if (File.Exists(localFilePath)) {
					if (!Helper.ShowMessage($"Файл с именем <{localFilePath}> уже существует.\nЗаменить?", this, true)) {
						return false;
					}
					try {
						File.Delete(localFilePath);
					} catch {
						Helper.ShowMessage($"Не удалось удалить файл <{hostItem.Name}> в локальной директории.", this);
						return false;
					}
					
				}
				var fileDownloadRequest = getFTPWebRequest($"{CurrentHostPath}/{hostItem.Name}", WebRequestMethods.Ftp.DownloadFile);
				try {
					using (var fileStream = File.Create(localFilePath))
					using (var fileDownloadResponse = fileDownloadRequest.GetResponse())
					using (var fileDownloadStream = fileDownloadResponse.GetResponseStream()) {
						fileDownloadStream.CopyTo(fileStream);
					}
				} catch (Exception ex) {
					Helper.ShowMessage(ex+"");
					Helper.ShowMessage($"Не удалось скопировать файл <{hostItem.Name}> в локальную директорию.", this);
					return false;
				}
				changeLocalDirectory(CurrentLocalPath);
			// Копирование на сервер
			} else if (SelectedItem?.Tag is FileInfo) {
				var fileInfo = (FileInfo)SelectedItem.Tag;
				var hostFilePath = $"{CurrentHostPath}/{fileInfo.Name}";
				if (checkIfHostPathExists(hostFilePath)) {
					if (!Helper.ShowMessage($"Файл с именем <{hostFilePath}> уже существует.\nЗаменить?", this, true)) {
						return false;
					}
					if (!deleteHostFile(hostFilePath)) {
						Helper.ShowMessage($"Не удалось удалить файл <{fileInfo.Name}> на сервере.", this);
						return false;
					}
				}
				var fileUploadRequest = getFTPWebRequest(hostFilePath, WebRequestMethods.Ftp.UploadFile);
				try {
					using (var fileStream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read))
					using (var fileUploadStream = fileUploadRequest.GetRequestStream()) {
						fileStream.CopyTo(fileUploadStream);
					}
				} catch {
					Helper.ShowMessage($"Не удалось скопировать файл <{fileInfo.Name}> на сервер.", this);
					return false;
				}
				changeHostDirectory(CurrentHostPath);
			}
			Helper.ShowMessage($"Файл скопирован.", this);
			return true;
		}

		/// <summary>
		/// Создание рабочей директории
		/// </summary>
		private void createWorkingDirectory() {
			if (checkIfHostPathExists(WorkingDirectoryPath)) {
				var promptResult = Helper.ShowMessage($"Рабочая директорию уже создана <{WorkingDirectoryPath}>.\nПерезаписать?", this, true);
				if (!promptResult || !removeHostDirectory(WorkingDirectoryPath, true)) {
					return;
				}
			}

			LoadingOverlayVisibility = Visibility.Visible;
			Task.Run(() => {
				var createRequest = getFTPWebRequest(WorkingDirectoryPath, WebRequestMethods.Ftp.MakeDirectory);
				executeFTPWebRequest(createRequest, out var createResult);
				if (!createResult) {
					Dispatcher.Invoke(() => Helper.ShowMessage($"Не удалось создать директорию <{WorkingDirectoryPath}>.", this));
				} else {
					for (var i = 0; i < 10; i++) {
						var subFolderPath = $"{WorkingDirectoryPath}/{i}";
						var createSubfolderRequest = getFTPWebRequest(subFolderPath, WebRequestMethods.Ftp.MakeDirectory);
						executeFTPWebRequest(createSubfolderRequest, out var createSubfolderResult);
						if (!createSubfolderResult) {
							Dispatcher.Invoke(() => Helper.ShowMessage($"Не удалось создать директорию <{subFolderPath}>.", this));
							continue;
						}
						for (var j = 0; j < 8; j++) {
							var subSubfolderPath = $"{subFolderPath}/{(char)('A' + j)}";
							var createSubSubfolderRequest = getFTPWebRequest(subSubfolderPath, WebRequestMethods.Ftp.MakeDirectory);
							executeFTPWebRequest(createSubSubfolderRequest, out var createSubSubfolderResult);
							if (!createSubSubfolderResult) {
								Dispatcher.Invoke(() => Helper.ShowMessage($"Не удалось создать директорию <{subSubfolderPath}>.", this));
							}
						}
					}
				}

				//if (IsInWorkingDirectory) {
				//	Dispatcher.Invoke(() => changeHostDirectory(BaseUri));
				//} else if (CurrentHostPath == BaseUri) {
				//	Dispatcher.Invoke(() => changeHostDirectory(CurrentHostPath));
				//}
				Dispatcher.Invoke(() => changeHostDirectory(WorkingDirectoryPath));
				LoadingOverlayVisibility = Visibility.Collapsed;
				Dispatcher.Invoke(() => Helper.ShowMessage($"Рабочая директория создана <{WorkingDirectoryPath}>.", this));
			});
		}

		/// <summary>
		/// Удаление рабочей директории
		/// </summary>
		private void removeWorkingDirectory() {
			if (!checkIfHostPathExists(WorkingDirectoryPath)) {
				Helper.ShowMessage($"Рабочая директория <{WorkingDirectoryPath}> не найдена.", this);
				return;
			}

			var promptResult = Helper.ShowMessage($"Удалить рабочую директорию <{WorkingDirectoryPath}>?", this, true);
			if (!promptResult) {
				return;
			}

			LoadingOverlayVisibility = Visibility.Visible;
			Task.Run(() => {
				var removeResult = removeHostDirectory(WorkingDirectoryPath, true);
				if (IsInWorkingDirectory) {
					Dispatcher.Invoke(() => changeHostDirectory(BaseUri));
				} else if (CurrentHostPath == BaseUri) {
					Dispatcher.Invoke(() => changeHostDirectory(CurrentHostPath));
				}
				Dispatcher.Invoke(() => LoadingOverlayVisibility = Visibility.Collapsed);
				Dispatcher.Invoke(() => Helper.ShowMessage($"Рабочая директория удалена <{WorkingDirectoryPath}>.", this));
			});
		}

		#endregion
	}
}
