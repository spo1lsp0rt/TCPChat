using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using DevExpress.Mvvm.POCO;

namespace TCPChat.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool firstJoinCompleted = true;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (btnCon.IsEnabled == false)
                btnClosing.Command.Execute(btnClosing.CommandParameter);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbChat.ScrollToEnd();
        }

        private void cbChatList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbChatList.SelectedIndex == -1)
            {
                cbChatList.SelectedIndex = cbChatList.Items.IndexOf("Общий чат");
            }
            else
            {
                if (firstJoinCompleted)
                {
                    firstJoinCompleted = false;
                    return;
                }
                btnJoinChat.Command.Execute(btnJoinChat.CommandParameter);
            }
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnJoinPMChat.Command.Execute(btnJoinPMChat.CommandParameter);
        }

        private void tbPMChat_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbPMChat.ScrollToEnd();
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            tbSystemMessages.ScrollToEnd();
        }

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                string folder = dialog.FileName;
                tbMessage.Text = folder;
                tbMessage.CaretIndex = 0; //Боже храни королеву от таких костылей
                tbMessage.Focus();
                tbMessage.IsEnabled = false;
                btnCancelFile.Visibility = Visibility.Visible;
            }
        }

        private void btnCancelFile_Click(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "";
            tbMessage.CaretIndex = 0;
            tbMessage.Focus();
            tbMessage.IsEnabled = true;
            btnCancelFile.Visibility = Visibility.Hidden;
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (btnCancelFile.Visibility == Visibility.Hidden)
            {
                btnSendMsg.Command.Execute(btnSendMsg.CommandParameter);
            }
            else
            {
                btnSendFile.Command.Execute(btnSendFile.CommandParameter);
                tbMessage.Text = "";
                tbMessage.CaretIndex = 0;
                tbMessage.Focus();
                tbMessage.IsEnabled = true;
                btnCancelFile.Visibility = Visibility.Hidden;
            }
        }
    }
}
