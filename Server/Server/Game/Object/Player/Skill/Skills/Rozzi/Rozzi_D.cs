using Google.Protobuf.Protocol;
using Server.Game;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Rozzi_D : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;

    private float _elapsed;
    private float _StopSkillTime = 1.0f;

    private bool _onMoveCmd, _hasSentRunAnimation;
    private Vector3 _targetPosition;
    private const float STOP_RANGE = 0.1f; 

    private string ANIM_RUN = "RUN";
    private string ANIM_IDLE = "WAIT";
    private string ANIM_SKILL = "SKILL_D";

    public Rozzi_D()
    {
        _characterType = CharacterType.Rozzi;
        _animName = ANIM_SKILL;
        _keyCode = KeyCode.D;

        HitboxRequired = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        //base.OnEnter(p, ctx);

        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.ObjectId = p.Id;
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Broadcast(combatModePkt);
            p.CombatTime = 0f;
        }
        
        // 애니메이션 패킷 전송
        p.SendAnimPacket("ROZZI_D", 0.05f);
        
        if (HitboxRequired)
            CreateHitbox(p, ctx);

        p.Room.AddStatusEffect(p, p, _keyCode, null);   // 이속 증가

        SendSkillConfirmPacket(p);
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: false);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (CanStopSkill)
            return;

        _elapsed += TimeUtil.Instance.DeltaTime;
        if (_elapsed >= _StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
            return;
        }

        if (_onMoveCmd)
        {
            if(!_hasSentRunAnimation)
            {
                p.SendAnimPacket(ANIM_RUN);
                _hasSentRunAnimation = true;
            }
        }
        else
        {
            if(_hasSentRunAnimation)
            {
                if(Vector3.Distance(p.Position, _targetPosition) <= STOP_RANGE)
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
        base.OnExit(p, ctx);

        p.AttackSpeedBuff(0.7f, 2);
    }
}

