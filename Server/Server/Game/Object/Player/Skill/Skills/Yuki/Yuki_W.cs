using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Yuki_W : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;

    private bool _hasSentRunAnimation;

    private string ANIM_RUN = "RUN";
    private string ANIM_IDLE = "WAIT";
    private string ANIM_SKILL = "SKILL_W";

    public Yuki_W()
    {
        _characterType = CharacterType.Yuki;
        _animName = ANIM_SKILL;
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Push(p.Session.Send, combatModePkt);
            p.CombatTime = 0f;
        }

        // 애니메이션 패킷 전송
        p.SendAnimPacket("YUKI_W", 0.05f);

        SendSkillConfirmPacket(p);

        p.Room.AddStatusEffect(p, p, _keyCode, null);

        p.SendYukiSkillEffect(SkillEffectType.WFlower);

        p.Room.Push(p.Room.BroadcastAbigailSound, p, AbigailSound.YukiWactive, 1f);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        //if (_onMoveCmd)
        //{
        //    if (!_hasSentRunAnimation)
        //    {
        //        p.SendAnimPacket(ANIM_RUN);
        //        _hasSentRunAnimation = true;
        //    }
        //}
        //else
        //{
        //    if (_hasSentRunAnimation)
        //    {
        //        if (Vector3.Distance(p.Position, _targetPosition) <= STOP_RANGE)
        //        {
        //            p.SendAnimPacket(ANIM_IDLE);
        //            p.SendStopPacket();
        //            _hasSentRunAnimation = false;
        //        }
        //    }
        //}

        //_onMoveCmd = false;

        return;
    }

    public override void OnMove(Player p, C_Move packet)
    {
        if (!_hasSentRunAnimation)
        {
            p.SendAnimPacket(ANIM_RUN);
            _hasSentRunAnimation = true;
        }
    }

    public override void OnStop(Player p)
    {
        if (_hasSentRunAnimation)
        {
            p.SendAnimPacket(ANIM_IDLE);
            _hasSentRunAnimation = false;
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.YukiStud = 4;

        S_YukiStud yukiStudPkt = new S_YukiStud();
        yukiStudPkt.ObjectId = p.Id;
        yukiStudPkt.StudCnt = p.YukiStud;

        p.Room.Push(p.Room.Broadcast, yukiStudPkt);
        p.SendYukiSkillEffect(SkillEffectType.WEffect);

        base.OnExit(p, ctx);
    }
}
