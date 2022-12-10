﻿using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace TCPChat.Client
{
    public class MainViewModel : ViewModelBase
    {
        public string IP { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5050;
        public string Name { get; set; } = "Name";
        //public string Password { get; set; } = "Password"; <TextBox MaxLength="8" Text="{Binding Password}"/>
        public string NewChatName { get; set; } = "Новая комната";
        public string SelectedChatName { get; set; }
        public string SelectedClientName { get; set; }
        public string Chat 
        { 
            get => GetValue<string>(); 
            set => SetValue(value);
        }
        public string SystemChat
        {
            get => GetValue<string>();
            set => SetValue(value);
        }
        public string PMChatName
        {
            get => GetValue<string>();
            set => SetValue(value);
        }
        public string PMChat 
        { 
            get => GetValue<string>(); 
            set => SetValue(value);
        }
        public string Message { get => GetValue<string>(); set => SetValue(value); }
        public string PersonalMessage { get => GetValue<string>(); set => SetValue(value); }
        public ObservableCollection<string> Users { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ChatList { get; set; } = new ObservableCollection<string>();

        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public MainViewModel()
        {
            
        }

        private void Listener()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (_client?.Connected == true)
                        {
                            string? json = _reader?.ReadLine();
                            if (json != null)
                            {
                                var cdata = JsonSerializer.Deserialize<ChatData>(json);

                                if (cdata.SystemMessage != null)
                                {
                                    SystemChat += cdata.SystemMessage;
                                }
                                switch (cdata.Type)
                                {
                                    case ChatType.PM:
                                        {
                                            if (cdata.Clients.Contains(SelectedClientName))
                                            {
                                                PMChatName = $"PM: {SelectedClientName}";
                                                if (cdata.Chat != null)
                                                {
                                                    PMChat = cdata.Chat;
                                                }
                                            }
                                            /*else
                                            {
                                                var tempUsers = Users;
                                                App.Current.Dispatcher.Invoke((Action)delegate
                                                {
                                                    Users.Clear();
                                                    foreach (string nick in tempUsers)
                                                    {
                                                        string temp = nick;
                                                        if (temp == Name)
                                                            temp += " (Я)";
                                                        if (temp == SelectedClientName)
                                                            temp += " Новое сообщение";
                                                        Users.Add(temp);
                                                    }
                                                });
                                            }*/
                                            break;
                                        }
                                    case ChatType.Common:
                                        {
                                            if (cdata.Chat != null)
                                            {
                                                Chat = cdata.Chat;
                                            }
                                            App.Current.Dispatcher.Invoke((Action)delegate
                                            {
                                                foreach (string chat in cdata.ChatList)
                                                {
                                                    if (!ChatList.Contains(chat))
                                                        ChatList.Add(chat);
                                                }
                                            });
                                            if (cdata.Clients.Count > 0)
                                            {
                                                App.Current.Dispatcher.Invoke((Action)delegate
                                                {
                                                    Users.Clear();
                                                    foreach (string nick in cdata.Clients)
                                                    {
                                                        string temp = nick;
                                                        if (temp == Name) 
                                                            temp += " (Я)";
                                                        Users.Add(temp);
                                                    }
                                                });
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                _client.Close();
                                Chat += "Connected error.\n";
                            }
                        }
                        Task.Delay(10).Wait();
                    }
                    catch (Exception ex)
                    { 
                        Chat += ex.Message + "\n";
                    }
                }
            });
        }

        public AsyncCommand CreateChatCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            _writer?.WriteLine($"Create: {NewChatName}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    });
                }, () => _client?.Connected == true);
            }
        }
        public AsyncCommand JoinChatCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            _writer?.WriteLine($"Join: {SelectedChatName}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    });
                }, () => _client?.Connected == true);
            }
        }
        public AsyncCommand JoinPMChatCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        if (SelectedClientName != Name + " (Я)")
                        {
                            try
                            {
                                _writer?.WriteLine($"JoinPM: {SelectedClientName}");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                    });
                }, () => _client?.Connected == true);
            }
        }

        public AsyncCommand DisonnectCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            _writer.WriteLine($"Logout: {Name}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    });
                });
            }
        }

        public AsyncCommand ConnectCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            _client = new TcpClient();
                            _client.Connect(IP, Port);
                            _reader = new StreamReader(_client.GetStream());
                            _writer = new StreamWriter(_client.GetStream());
                            Listener();
                            _writer.AutoFlush = true;
                            _writer.WriteLine($"Login: {Name}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    });
                }, () => _client is null || _client?.Connected == false);
            }
        }

        public AsyncCommand SendPMCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(PersonalMessage))
                        {
                            var time = DateTime.UtcNow;
                            _writer?.WriteLine($"PM: {SelectedClientName} [{time.Hour}:{time.Minute}:{time.Second}] {Name}: {PersonalMessage}");
                            PersonalMessage = "";
                        }
                    });
                }, () => _client?.Connected == true);
            }
        }
        public AsyncCommand SendCommand
        {
            get
            {
                return new AsyncCommand(() =>
                {
                    return Task.Run(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(Message))
                        {
                            var time = DateTime.UtcNow;
                            _writer?.WriteLine($"[{time.Hour}:{time.Minute}:{time.Second}] {Name}: {Message}");
                            Message = "";
                        }
                    });
                }, () => _client?.Connected == true);
            }
        }
    }
}
