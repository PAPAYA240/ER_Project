using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_Q : SkillHandlerBase
{
    public Hyunwoo_Q()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        // 만약 여기서 체인지 스테이트를 하면 될 것 같은데 되나? > p.ChangeState();
        // 루프 애니메이션을 계속 돌아야하는데 > 딕트에 시간을 바꿔놓고 사용.
        // 음 애매해. 키 인풋을 막는 시간과 그렇지 않은 시간으로 나뉘어서
        // 특정 시간 이후에는 키 인풋을 받을 수 있도록 한다. 그래서 키 인풋이 들어오면
        // 해당 키에 맞는 스테이트로 바로 넘어가고, 아니면 나머지 애니메이션을 재생한다.
        
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

    }
}

