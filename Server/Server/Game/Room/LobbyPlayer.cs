using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class LobbyPlayer
    {
        public PickRoom Room { get; set; }
        public ClientSession Session { get; set; }

        public string UserName { get; set; }
    }
}
