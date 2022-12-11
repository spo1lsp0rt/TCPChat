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
        public string Name { get; set; }
        public string Chat { get; set; }
        public ObservableCollection<string> Clients { get; set; }
        public string SystemMessage { get; set; }
        public string FileName { get; set; }
        public string FileBase64 { get; set; }
        public ObservableCollection<string> ChatList { get; set; } = new ObservableCollection<string>();
        public ChatData(ChatType type, string name, string chat, ObservableCollection<string> clients, string sysmsg = null, string filename = null, string filebase64 = null)
        {
            Type = type;
            Name = name;
            Chat = chat;
            Clients = clients;
            SystemMessage = sysmsg;
            FileName = filename;
            FileBase64 = filebase64;
        }
    }
    public enum ChatType
    {
        PM,
        Common
    }
}
