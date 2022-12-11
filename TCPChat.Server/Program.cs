using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Buffers.Text;
using System.Text;
using static System.Net.WebRequestMethods;

namespace TCPChat.Server
{
    internal class Program
    {
        //static TcpListener listener = new TcpListener(IPAddress.Any, 5050);
        static IPAddress ip = IPAddress.Parse("26.91.248.130");
        static TcpListener listener = new TcpListener(IPAddress.Any, 5050);
        static List<ConnectedClient> clientsList = new List<ConnectedClient>();
        static List<ChatData> chatdataList = new List<ChatData>() { new ChatData(ChatType.Common, "Общий чат", "Начало сессии...\n", new System.Collections.ObjectModel.ObservableCollection<string>()) };

        static void Main(string[] args)
        {
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();

                Task.Factory.StartNew(() =>
                {
                    var sr = new StreamReader(client.GetStream());
                    while (client.Connected)
                    {
                        var line = sr.ReadLine();
                        var nick = line.Replace("Login: ", "");

                        if (line.StartsWith("Login: ") && !string.IsNullOrWhiteSpace(nick))
                        {
                            if (clientsList.FirstOrDefault(s => s.Name == nick) is null)
                            {
                                clientsList.Add(new ConnectedClient(client, nick));
                                Console.WriteLine($"Подключился {nick}");
                                var clientData = clientsList.FirstOrDefault(s => s.Client == client);
                                JoinChat(clientData, chatdataList.First());
                                break;
                            }
                            else
                            {
                                SendSystemMessageToClient("Пользователь с таким ником уже есть", client, ChatType.Common);
                                client.Client.Disconnect(false);
                            }
                        }
                    }

                    while(client.Connected)
                    {
                        try
                        {
                            var clientData = FindClientData(client);
                            var line = sr.ReadLine();

                            if (line.StartsWith("Logout: "))
                            {
                                DisconnectClient(clientData);
                                break;
                            }
                            else if (line.StartsWith("Create: "))
                            {
                                var chatname = line.Replace("Create: ", "");
                                bool ok = CreateChat(chatname);
                                if (ok)
                                {
                                    SendSystemMessageToAll($"{clientData.Name} создал чат {chatname}", ChatType.Common);
                                    //JoinChat(clientData, chatData);
                                }
                                else SendSystemMessageToClient("Чат с таким названием уже есть", client, ChatType.Common);
                            }
                            else if (line.StartsWith("Join: "))
                            {
                                var chatname = line.Replace("Join: ", "");
                                ChatData chatData = chatdataList.FirstOrDefault(s => s.Name == chatname);
                                if (chatData != null)
                                {
                                    JoinChat(clientData, chatData);
                                }
                            }
                            else if (line.StartsWith("JoinPM: "))
                            {
                                var receiver = line.Replace("JoinPM: ", "");
                                ChatData chatData = chatdataList.FirstOrDefault(s => s.Type == ChatType.PM && s.Clients.Contains(receiver) && s.Clients.Contains(clientData.Name));
                                if (chatData == null)
                                {
                                    CreatePMChat(clientData.Name, receiver); 
                                    chatData = chatdataList.FirstOrDefault(s => s.Type == ChatType.PM && s.Clients.Contains(receiver) && s.Clients.Contains(clientData.Name));
                                }
                                chatData.Name = $"PM: {receiver}";
                                JoinChat(clientData, chatData);
                            }
                            else if (line.StartsWith("File: "))
                            {
                                var temp = line.Substring(5);
                                var name = temp.Substring(0, temp.IndexOfAny(new char[] { ':' }) - 1);
                                var file = temp.Substring(name.Length);
                                ChatData activeChat = chatdataList.FirstOrDefault(s => s.Type == ChatType.Common && s.Clients.Contains(clientData.Name));
                                string msg = $"{clientData.Name} отправил файл {name} (проверьте папку \"Downloads\")" + System.Environment.NewLine;
                                activeChat.Chat += msg;
                                activeChat.FileName = name;
                                activeChat.FileBase64 = file;
                                SendChatDataToAllInChat(activeChat);
                                Console.WriteLine("Chat: " + activeChat.Name + " >> " + msg);

                            }
                            else if (line.StartsWith("PM: "))
                            {
                                var temp = line.Substring(3);
                                var receiver = temp.Substring(0, temp.IndexOfAny(new char[] { '[' }) - 1);
                                var message = temp.Substring(receiver.Length);
                                ChatData pmChat = chatdataList.FirstOrDefault(s => s.Type == ChatType.PM && s.Clients.Contains(clientData.Name) && s.Clients.Contains(receiver));

                                pmChat.Chat += message + System.Environment.NewLine;

                                SendChatDataToAllInChat(pmChat);
                                Console.WriteLine("PM: " + receiver + " >> " + message);
                            }
                            else
                            {
                                ChatData activeChat = chatdataList.FirstOrDefault(s => s.Type == ChatType.Common && s.Clients.Contains(clientData.Name));
                                activeChat.Chat += line + System.Environment.NewLine;
                                SendChatDataToAllInChat(activeChat);
                                Console.WriteLine("Chat: " + activeChat.Name + " >> " + line);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                     }
                });
            }
        }

        private static void JoinChat(ConnectedClient clientData, ChatData chatData)
        {
            switch (chatData.Type)
            {
                case ChatType.PM:
                    {
                        SendChatDataToClient(clientData.Client, chatData);
                        break;
                    }
                case ChatType.Common:
                    {
                        ChatData? activeChat = chatdataList.FirstOrDefault(s => s.Clients.Contains(clientData.Name));
                        if (activeChat != null)
                        {
                            activeChat.Clients.Remove(clientData.Name);
                            chatData.Chat += $"Покинул чат - {clientData.Name}" + System.Environment.NewLine;
                            SendChatDataToAllInChat(activeChat);
                            //SendSystemMessageToAllInChat($"Покинул чат - {clientData.Name}" + System.Environment.NewLine, activeChat);
                        }
                        chatData.Clients.Add(clientData.Name);
                        chatData.Chat += $"Присоединился к чату - {clientData.Name}" + System.Environment.NewLine;
                        SendChatDataToAllInChat(chatData);
                        //SendSystemMessageToAllInChat($"Присоединился к чату - {clientData.Name}" + System.Environment.NewLine, chatData);
                        break;
                    }
                default:
                    break;
            }
        }

        private static void CreatePMChat(string user1, string user2)
        {
            chatdataList.Add(new ChatData(ChatType.PM, $"PM: {user2}", "", new System.Collections.ObjectModel.ObservableCollection<string>() { user1, user2 }));
        }
        private static bool CreateChat(string chatname)
        {
            if (chatdataList.FirstOrDefault(s => s.Name == chatname) is null)
            {
                chatdataList.Add(new ChatData(ChatType.Common, chatname, "", new System.Collections.ObjectModel.ObservableCollection<string>()));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static ObservableCollection<string> ToObservableCollection(List<string> list)
        {
            ObservableCollection<string> collection = new ObservableCollection<string>();
            foreach (var item in list)
            {
                collection.Add(item);
            }
            return collection;
        }

        private static void DisconnectClient(ConnectedClient clientData)
        {
            string name = clientData.Name;

            ChatData activeChat = chatdataList.FirstOrDefault(s => s.Clients.Contains(clientData.Name));
            activeChat.Clients.Remove(name);
            clientData.Client.Close();
            clientsList.Remove(clientData);

            Console.WriteLine($"{name} отключился");
            string msg = $"Покинул чат - {clientData.Name}" + System.Environment.NewLine;
            activeChat.Chat += msg;
            SendChatDataToAllInChat(activeChat);
            SendSystemMessageToAll($"{name} отключился", ChatType.Common);
        }

        private static ConnectedClient FindClientData(TcpClient client)
        {
            return clientsList.FirstOrDefault(s => s.Client == client);
        }
        private static ConnectedClient FindClientData(string name)
        {
            return clientsList.FirstOrDefault(s => s.Name == name);
        }

        private static ObservableCollection<string> GetClientInChatInfo(string clientname)
        {
            return chatdataList.FirstOrDefault(s => s.Type == ChatType.Common && s.Clients.Contains(clientname)).Clients;
        }
        private static ObservableCollection<string> GetChatListInfo()
        {
            return ToObservableCollection(chatdataList.FindAll(s => s.Type == ChatType.Common).ToList().Select(s => s.Name).ToList());
        }

        private static void SendSystemMessageToAll(string msg, ChatType chattype)
        {
            foreach (var clientData in clientsList)
            {
                SendSystemMessageToClient(msg, clientData.Client, chattype, GetClientInChatInfo(clientData.Name), GetChatListInfo());
            }
        }
        private static void SendSystemMessageToAllInChat(string msg, ChatData chatData)
        {
            foreach (var client in chatData.Clients)
            {
                var clientData = clientsList.FirstOrDefault(s => s.Name == client);
                SendSystemMessageToClient(msg, clientData.Client, chatData.Type);
            }
        }
        private static void SendSystemMessageToClient(string msg, TcpClient client, ChatType chattype, ObservableCollection<string> Clients = null, ObservableCollection<string> ChatList = null)
        {
            Task.Run(() =>
            {
                try
                {
                    var time = DateTime.UtcNow;
                    ChatData cdata = new ChatData(chattype, null, $"{System.Environment.NewLine}[{time.Hour}:{time.Minute}:{time.Second}] Система: {msg}", new System.Collections.ObjectModel.ObservableCollection<string>());
                    if (Clients != null && ChatList != null)
                    {
                        cdata.Clients = Clients;
                        cdata.ChatList = ChatList;
                    }
                    string json = JsonSerializer.Serialize(cdata);
                    SendJsonToClient(json, client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private static void SendChatDataToAll(ChatData chatData)
        {
            foreach (var clientData in clientsList)
            {
                SendChatDataToClient(clientData.Client, chatData);
            }
        }
        private static void SendChatDataToAllInChat(ChatData chatData)
        {
            foreach (var client in chatData.Clients)
            {
                var clientData = clientsList.FirstOrDefault(s => s.Name == client);
                SendChatDataToClient(clientData.Client, chatData);
            }
        }
        private static void SendChatDataToClient(TcpClient client, ChatData chatData)
        {
            Task.Run(() =>
            {
                try
                {
                    chatData.ChatList = GetChatListInfo();
                    string json = JsonSerializer.Serialize(chatData);
                    SendJsonToClient(json, client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private static void SendJsonToClient(string json, TcpClient client)
        {
            if (client.Connected)
            {
                var sw = new StreamWriter(client.GetStream());
                sw.AutoFlush = true;
                sw.WriteLine(json);
            }
        }
    }
}
