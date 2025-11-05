using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_E_Loop : SkillHandlerBase
{
    public Hyunwoo_E_Loop()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_E_LOOP";
        _keyCode = KeyCode.E;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        // 만약 여기서 체인지 스테이트를 하면 될 것 같은데 되나? > p.ChangeState();
        // 루프 애니메이션을 계속 돌아야하는데 > 딕트에 시간을 바꿔놓고 사용.
        

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

    }
}

