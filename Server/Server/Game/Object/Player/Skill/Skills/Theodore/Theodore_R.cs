using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Theodore_R : SkillHandlerBase
{
    public override bool CanMoveDuringCast => false;
    public Theodore_R()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);

        // 90도 추가 회전
       //Vector2 playerPos = new Vector2(p.Info.PosInfo.PosX, p.Info.PosInfo.PosZ);
       //Vector2 mousePos = new Vector2(ctx.MousePos.X, ctx.MousePos.Y);
       //Quaternion lookAtMouseRot = QuaternionHelper.LookRotationY(playerPos, mousePos);
       //Quaternion xRotation = QuaternionHelper.FromYRotation(90f);
       //Quaternion finalRot = lookAtMouseRot * xRotation;
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode);

        //p.SendSkillEffect(new Vector2(ctx.MousePos.X, ctx.MousePos.Y), keyCode: _keyCode, sendLookatMousePacket: true,
        //    targetPos: default, targetRot: finalRot, type: "Select", "FX_Skill04_linoleum");

        p.SendSkillEffect(new Vector2(ctx.MousePos.X, ctx.MousePos.Y), keyCode: _keyCode, sendLookatMousePacket: false,
             targetPos: default, targetRot: default, type: "Select", "FX_Skill04_Charging");
    }
  
    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

