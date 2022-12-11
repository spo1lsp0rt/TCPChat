using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPChat.Client
{
    public class ChatData
    {
        public ChatType Type { get; set; }
        public string? Name { get; set; }
        public string? Chat { get; set; }
        public ObservableCollection<string>? Clients { get; set; }
        public string? SystemMessage { get; set; }
        public string? FileName { get; set; }
        public string? FileBase { get; set; }
        public ObservableCollection<string>? ChatList { get; set; }
        public ChatData(ChatType type, string name, string chat, ObservableCollection<string>? clients = null, string? systemMessage = null, string? fileName = null, string? fileBase = null, ObservableCollection<string>? chatList = null)
        {
            Type = type;
            Name = name;
            Chat = chat;
            Clients = clients;
            SystemMessage = systemMessage;
            FileName = fileName;
            FileBase = fileBase;
            ChatList = chatList;
        }
    }
    public enum ChatType
    {
        PM,
        Common
    }
}
