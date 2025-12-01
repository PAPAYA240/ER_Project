using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;
using static Server.Game.GameObject;

public sealed class Rozzi_W : RozziSkillHandler
{
    public override bool CanMoveDuringCast => true;

    public Rozzi_W()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);

        p.Room.AddStatusEffect(p, p, _keyCode, null); // 스킬 사용시 이속 버프
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: false);
    }

    public override void OnCollision<T>(Player p, List<T> targets, GameObject.StatusEffect effect)
    {
        foreach (var t in targets)
        {
            GameObject go = t as GameObject;
            if (go == null)
                return;

            p.SendSkillEffect(new Vector2(go.Position.X, go.Position.Z), keyCode: _keyCode, sendLookatMousePacket: false,
                targetPos: default, targetRot: default,
                type: "Select", "FX_BI_Rozzi_Skill02_Debuff",
                useTargetTransform: true, targetId: go.Id);

            p.SendSkillEffect(new Vector2(go.Position.X, go.Position.Z), keyCode: _keyCode, sendLookatMousePacket: false,
                targetPos: default, targetRot: default,
                type: "Select", "FX_BI_Rozzi_Skill02_Hit",
                useTargetTransform: true, targetId: go.Id);
        }
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
        AddAttackToken(p);
    }
}

