using Google.Protobuf.Protocol;
using Server.Game.Object.Monster.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object
{
    public class EnvironmentObj : GameObject
    {
        public EnvType _envType = EnvType.EnvNone;
        public EnvironmentObj() 
        {
            ObjectType = GameObjectType.Environment;
        }

        public void Init()
        {
            ObjectType = GameObjectType.Environment;
        }
    }
}
