using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPChat.Server
{
    public class ChatData
    {
        public ChatType Type { get; set; }
        public string Name { get; set; }
        public string Chat { get; set; }
        public string? SystemMessage { get; set; }
        public ObservableCollection<string> Clients { get; set; }
        public ObservableCollection<string> ChatList { get; set; } = new ObservableCollection<string>();
        public ChatData(ChatType type, string name, string chat, string sysmsg, ObservableCollection<string> clients)
        {
            Type = type;
            Name = name;
            Chat = chat;
            SystemMessage = sysmsg;
            Clients = clients;
        }
    }
    public enum ChatType
    {
        PM,
        Common
    }
}
