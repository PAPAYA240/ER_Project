using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Yuki_W : SkillHandlerBase
{
    private bool _onMoveCmd, _hasSentRunAnimation;
    private Vector3 _targetPosition;
    private const float STOP_RANGE = 0.1f;

    private string ANIM_RUN = "RUN";
    private string ANIM_IDLE = "WAIT";

    public Yuki_W()
    {
        _characterType = CharacterType.Yuki;
        _animName = "YUKI_W";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);

        p.Room.AddStatusEffect(p, p, _keyCode, null);

        p.SendYukiSkillEffect(SkillEffectType.YukiW);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_onMoveCmd)
        {
            if (!_hasSentRunAnimation)
            {
                p.SendAnimPacket(ANIM_RUN);
                _hasSentRunAnimation = true;
            }
        }
        else
        {
            if (_hasSentRunAnimation)
            {
                if (Vector3.Distance(p.Position, _targetPosition) <= STOP_RANGE)
                {
                    p.SendAnimPacket(ANIM_IDLE);
                    p.SendStopPacket();
                    _hasSentRunAnimation = false;
                }
            }
        }

        _onMoveCmd = false;

        return;
    }

    public override void OnMove(Player p, C_Move packet)   // OnTick 보다 먼저 실행(Flush)
    {
        _onMoveCmd = true;
        _targetPosition = packet.TargetPosition.ToVector();
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.YukiStud = 4;

        S_YukiStud yukiStudPkt = new S_YukiStud();
        yukiStudPkt.ObjectId = p.Id;
        yukiStudPkt.StudCnt = p.YukiStud;

        p.Room.Push(p.Room.Broadcast, yukiStudPkt);

        base.OnExit(p, ctx);
    }
}
