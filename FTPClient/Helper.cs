using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FTPClient {
    internal static class Helper {

        /// <summary>
        /// Вывод окна с сообщением
        /// </summary>
        /// <param name="message"></param>
        /// <param name="owner"></param>
        internal static void showMessage(string message, Window owner = null) {
            var messageWindow = new MessageWindow(message);
            messageWindow.Owner = owner;
            messageWindow.ShowDialog();
        }

    }
}
