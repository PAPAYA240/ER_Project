using Google.Protobuf.Protocol;
using Server.Game;
using System;
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
        p.SendYukiSkillEffect(SkillEffectType.QBuff);

        Console.WriteLine("Yuki Q Active");
    }
}