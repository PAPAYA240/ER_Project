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

        public Weapon Weapon { get; set; } = Weapon.None;

        public TraitType Trait { get; set; }

        public string UserName { get; set; }

        public int Team { get; set; }

        public int PickIdx { get; set; }

        public bool IsReady { get; set; } = false;
    }
}
