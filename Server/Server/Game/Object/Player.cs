using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Server.Game
{
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }

        //Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();

        class CoolTime
        {
            public bool isCoolDown;
            public float coolTime;
        }

        public Player()
        {
            ObjectType = GameObjectType.Player;
        }

        public override void OnDamaged(GameObject attacker, float damage)
        {
            base.OnDamaged(attacker, damage);
        }

        public override void OnDead(GameObject attacker)
        {
            //base.OnDead(attacker);
        }
    }
}
