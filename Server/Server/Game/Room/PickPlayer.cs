using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game
{

    public class PickPlayer
    {
        public PickRoom Room { get; set; }
        public ClientSession Session { get; set; }

        //public CharacterType CharacterType { get; set; }
        public string UserName { get; set; }
    }
}
