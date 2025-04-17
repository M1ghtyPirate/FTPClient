using FTPClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FTPClient {
	public static class Helper {

		/// <summary>
		/// Вывод окна с сообщением
		/// </summary>
		/// <param name="message"></param>
		/// <param name="owner"></param>
		/// <param name="prompt"></param>
		public static bool ShowMessage(string message, Window owner = null, bool prompt = false) {
			var messageWindow = new MessageWindow(message);
			messageWindow.CancelButton.Visibility = prompt ? Visibility.Visible : Visibility.Collapsed;
			messageWindow.Owner = owner;
			messageWindow.ShowDialog();
			return messageWindow.DialogResult ?? false;
		}

		/// <summary>
		/// Преобразования строки элемента директории
		/// </summary>
		/// <param name="listDirectoryDetailsString"></param>
		/// <returns></returns>
		public static HostItem ParseHostItem(string listDirectoryDetailsString) {
			if (string.IsNullOrWhiteSpace(listDirectoryDetailsString)) {
				return null;
			}

			var regexPattern = new Regex("^([-drwx]{10})\\s+(\\d+)\\s+(\\S+)\\s+(\\S+)\\s+(\\d+)\\s+(\\w{3}\\s+\\d{1,2}\\s+(\\d{1,2}:\\d{1,2}|\\d+))\\s+(.+)$");
			var match = regexPattern.Match(listDirectoryDetailsString);
			if (!match.Success) {
				return null;
			}

			var hostItem = new HostItem() {
				Permissions = match.Groups[1].Value,
				HardLinkCount = match.Groups[2].Value,
				UserOwner = match.Groups[3].Value,
				GroupOwner = match.Groups[4].Value,
				Size = int.Parse(match.Groups[5].Value),
				//DateModified = DateTime.Parse(match.Groups[6].Value),
				Name = match.Groups[8].Value,
			};
			hostItem.IsDirectory = hostItem.Permissions[0] == 'd';
			hostItem.IsSystemNavigationItem = hostItem.Name == "." || hostItem.Name == "..";
			//hostItem.IsEditable = 
			return hostItem;
		}

	}
}
