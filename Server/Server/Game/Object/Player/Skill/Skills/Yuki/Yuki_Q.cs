using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_Q : InstantHandlerBase
{
    public Yuki_Q()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void ExecuteInstant(Player p)
    {
        p.AttackActive = true;
        Console.WriteLine("Yuki Q Active");
    }
}