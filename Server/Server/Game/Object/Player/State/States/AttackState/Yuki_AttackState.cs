using Google.Protobuf.Protocol;
using Server.Game;
using System;
using static Player_StunState;
using static Server.Data.DataUtils;

public class Yuki_AttackState : Player_AttackState
{
    private const string AnimAttackQ = "SKILL_Q";
    KeyCode _keyCode = KeyCode.Q;
    bool IsPassiveAttack = false;

    public Yuki_AttackState(int targetId, bool chaseAllowed = true) : base(targetId, chaseAllowed)
    {
    }

    protected override void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds / p.AttackSpeed);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds / p.AttackSpeed);

        // 애니 송출(서버 권한)
        string animName;

        if (p.AttackActive == true)
        {
            animName = AnimAttackQ;

            // 이펙트 멈추기
            p.SendYukiSkillEffect(SkillEffectType.QBuff, false);

            p.AttackActive = false;

            animName = AnimAttackQ;
            p.Skill.StartCooldown(_keyCode);
            p.SendSkillCostPacket(_keyCode, p.Skill.GetCooldown(_keyCode));
        }
        else
        {
            animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
            _attackIndex = 1 - _attackIndex;
        }

        // 유키 단추
        if (p.YukiStud > 0)
        {
            p.YukiStud--;

            S_YukiStud yukiStudPkt = new S_YukiStud();
            yukiStudPkt.ObjectId = p.Id;
            yukiStudPkt.StudCnt = p.YukiStud;

            p.Room.Push(p.Room.Broadcast, yukiStudPkt);
        }

        // 전투 상태 평타 칠 때마다 갱신
        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Push(p.Session.Send, combatModePkt);
            p.CombatTime = 0f;
        }

        p.SendAnimPacket(animName, 0.05f, p.AttackSpeed, true);
    }

    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
            return;

        GameRoom room = p.Room;

        if (IsPassiveAttack)
        {
            Player targetPlayer = target as Player;

            if (targetPlayer != null)
            {
                StunStateDesc desc = new StunStateDesc();
                desc.EndPos = target.Position;
                desc.Duration = 0.5f;
                desc.Speed = 0f;

                targetPlayer.ChangeState(new Player_StunState(desc));
            }    
            //room.Push(target.OnDamaged, p, p.Attack, false, true);
        }

        // 평타 데미지
        room.Push(target.OnDamaged, p, p.Attack, false, true);
    }
}
