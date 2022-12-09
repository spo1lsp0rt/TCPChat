using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml.Linq;

namespace TCPChat.Server
{
    internal class Program
    {
        //static TcpListener listener = new TcpListener(IPAddress.Any, 5050);
        static IPAddress ip = IPAddress.Parse("26.91.248.130");
        static TcpListener listener = new TcpListener(IPAddress.Any, 5050);
        static List<ConnectedClient> clientsList = new List<ConnectedClient>();
        static List<ChatData> chatdataList = new List<ChatData>() { new ChatData("Общий чат", "Начало сессии...\n", new System.Collections.ObjectModel.ObservableCollection<string>()) };

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
                                JoinChat(clientData, chatdataList.First()); //К общему ли чату подключит
                                break;
                            }
                            else
                            { 
                                var sw = new StreamWriter(client.GetStream());
                                sw.AutoFlush = true;
                                string json = ChatDataToJSON("Система: Пользователь с таким ником уже есть" + System.Environment.NewLine, null);
                                sw.WriteLine(json);
                                client.Client.Disconnect(false);
                            }
                        }
                    }

                    while(client.Connected)
                    {
                        try
                        {
                            var clientData = clientsList.FirstOrDefault(s => s.Client == client);
                            var line = sr.ReadLine();
                            // && !string.IsNullOrWhiteSpace(nick)
                            if (line.StartsWith("Logout: "))
                            {
                                Console.WriteLine($"{clientData.Name} отключился");
                                ChatData activeChat = chatdataList.FirstOrDefault(s => s.Users.Contains(clientData.Name));
                                activeChat.Users.Remove(clientData.Name); //проверить удаляет ли по ссылке из chatdata
                                clientData.Client.Close();
                                string msg = $"{clientData.Name} отключился" + System.Environment.NewLine;
                                clientsList.Remove(clientData);
                                SendToAllClientsInChat(msg, activeChat);
                                break;
                            }
                            else if (line.StartsWith("Create: "))
                            {
                                var chatname = line.Replace("Create: ", "");
                                if (chatdataList.FirstOrDefault(s => s.Name == chatname) is null)
                                {
                                    chatdataList.Add(new ChatData(chatname, "", new System.Collections.ObjectModel.ObservableCollection<string>()));
                                    ChatData chatData = chatdataList.FirstOrDefault(s => s.Name == chatname);
                                    SendToAllChats($"{clientData.Name} создал чат {chatData.Name}" + System.Environment.NewLine);
                                    //JoinChat(clientData, chatData);
                                }
                                else
                                {
                                    var sw = new StreamWriter(client.GetStream());
                                    sw.AutoFlush = true;
                                    string json = ChatDataToJSON("Система: Чат с таким названием уже есть" + System.Environment.NewLine, null);
                                    sw.WriteLine(json);
                                }
                            }
                            else if (line.StartsWith("Join: "))
                            {
                                var chatname = line.Replace("Join: ", "");
                                ChatData chatData = chatdataList.FirstOrDefault(s => s.Name == chatname);
                                JoinChat(clientData, chatData);
                            }
                            else if (line.StartsWith("File: "))
                            {

                            }
                            else
                            {
                                ChatData activeChat = chatdataList.FirstOrDefault(s => s.Users.Contains(clientData.Name));
                                SendToAllClientsInChat(line + System.Environment.NewLine, activeChat);
                                Console.WriteLine(line);
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
            string json = "";
            ChatData? activeChat = chatdataList.FirstOrDefault(s => s.Users.Contains(clientData.Name));
            if (activeChat != null)
            {
                activeChat.Users.Remove(clientData.Name);
                SendToAllClientsInChat($"Покинул чат - {clientData.Name}" + System.Environment.NewLine, activeChat);
            }
            chatData.Users.Add(clientData.Name);
            SendToAllClientsInChat($"Присоединился к чату - {clientData.Name}" + System.Environment.NewLine, chatData);
        }

        private static string ChatDataToJSON(string message, ChatData chatData)
        {
            ChatData cdata = new ChatData("", "", new System.Collections.ObjectModel.ObservableCollection<string>());
            if (chatData != null)
            {
                chatData.Chat += message;
                cdata = chatData;
                cdata.ChatList.Clear();
                foreach (ChatData chat in chatdataList)
                {
                    cdata.ChatList.Add(chat.Name);
                }
            }
            else
            {
                cdata.SystemMessage = message;
            }

            string json = JsonSerializer.Serialize(cdata);
            cdata.SystemMessage = null;
            return json;
        }

        private static void SendToAllClientsInChat(string msg, ChatData chatData)
        {
            Task.Run(() =>
            {
                string json = ChatDataToJSON(msg, chatData);
                for (int i = 0; i < chatData.Users.Count; i++)
                {
                    try
                    {
                        var client = clientsList.FirstOrDefault(s => s.Name == chatData.Users[i]);
                        if (client.Client.Connected)
                        {
                            var sw = new StreamWriter(client.Client.GetStream());
                            sw.AutoFlush = true;
                            sw.WriteLine(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }
        private static void SendToAllChats(string msg)
        {
            Task.Run(() =>
            {
                for (int i = 0; i < chatdataList.Count; i++)
                {
                    string json = ChatDataToJSON(msg, chatdataList[i]);
                    for (int j = 0; j < chatdataList[i].Users.Count; j++)
                    {
                        try
                        {
                            var client = clientsList.FirstOrDefault(s => s.Name == chatdataList[i].Users[j]);
                            if (client.Client.Connected)
                            {
                                var sw = new StreamWriter(client.Client.GetStream());
                                sw.AutoFlush = true;
                                sw.WriteLine(json);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            });
        }
    }
}
