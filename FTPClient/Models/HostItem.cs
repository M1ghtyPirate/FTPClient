using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPClient.Models {
	/// <summary>
	/// Модель данных о элементе сервера
	/// </summary>
	public class HostItem {

		public string Permissions { get; set; }

		public string HardLinkCount { get; set; }

		public string UserOwner { get; set; }

		public string GroupOwner { get; set; }

		public int Size { get; set; }

		public DateTime DateModified { get; set; }

		public string Name { get; set; }

		public bool IsDirectory { get; set; }

		public bool IsEditable { get; set; }

	}
}
