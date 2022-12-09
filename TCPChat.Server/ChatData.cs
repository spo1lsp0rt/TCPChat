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
        public string Name { get; set; }
        public string Chat { get; set; }
        public string SystemMessage { get; set; }
        public ObservableCollection<string> Users { get; set; }
        public ObservableCollection<string> ChatList { get; set; } = new ObservableCollection<string>();
        public ChatData(string name, string chat, ObservableCollection<string> users)
        {
            Name = name;
            Chat = chat;
            Users = users;
        }
    }
}
