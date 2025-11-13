using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static ISkill;
using static Server.Data.DataUtils;
using static Server.Game.GameObject;

public abstract class SkillHandlerBase : ISkill
{
    public virtual bool CanMoveDuringCast => false;
    public virtual float MoveSpeedMultiplier => 1.0f;
    public bool CanStopSkill { get; set; } = false;

    //public int                      LastSeq { get { return _lastSeq; } set { _lastSeq = value; } }
    //public SkillCollisionProposal   Latest { get { return _latest; } set { _latest = value; } }
    public Dictionary<int, SkillCollisionProposal> _collisions = new Dictionary<int, SkillCollisionProposal>();


    //protected int _lastSeq;
    //protected SkillCollisionProposal _latest;
    //protected bool _committed;
    protected Vector3 _finalEnd;

    // TEMP
    protected int _requestId = 0;
    protected int _commitId = 0;

    protected CharacterType _characterType;
    protected string _animName;
    protected KeyCode _keyCode;
    protected bool HitboxCreated { get; set; } = true;

    public virtual void OnEnter(Player p, SkillContext ctx)
    {
        p.CombatState = CombatState.Combat;
        p.CombatTime = 0f;
        //LastSeq = 0;
        //Latest = default;
        //_collisions = default;
        //_committed = false;

        // 애니메이션 패킷 전송
        p.SendAnimPacket(_animName, 0.05f);

        // 이동 잠금
        if (CanMoveDuringCast == false)
        {
            // 이동 금지 스킬이면 강제 정지
            p.SendStopPacket(StopReason.StopMoveOnly);
        }

        if(HitboxCreated)
            CreateHitbox(p, ctx);
    }

    public void CreateHitbox(Player p, SkillContext ctx)
    {
        switch (ctx.Key)
        {
            case KeyCode.Q:
            case KeyCode.W:
            case KeyCode.E:
            case KeyCode.R:
            case KeyCode.D:
                p.Room.CollManager.AddHitbox(p, p.Info.Player.CharType, ctx.Key, ctx.MousePos);
                break;

        }
    }
    public virtual void OnExit(Player p, SkillContext ctx)
    {
        //p.SendSkillMotion(
        //    type: SkillMotionType.Transform,
        //    start: p.Position,
        //    end: _finalEnd,
        //    authoritativeEnd: true);
    }

    public virtual void OnAttack(Player p)
    {
    }
    public virtual void OnHit(Player p, SkillContext ctx)
    {
        
    }

    public virtual void OnCollision<T>(Player p, List<T> targets, GameObject.StatusEffect effect)
    {
        
    }

    public virtual void OnCollision<T>(Player p, T nearestTarget, GameObject.StatusEffect effect)
    {
        
    }

    public virtual void OnTick(Player p, SkillContext ctx)
    {
        
    }

    public virtual void OnPropose(Player p, in SkillCollisionProposal prop)
    {
        _collisions.Add(prop.requestId, prop);
    }

    public virtual bool CanCast(Player p, SkillContext ctx)
    {
        return true;
    }

    #region 스킬 중 이동 관련
    public virtual void OnMove(Player p)
    {
    }

    public virtual void OnStop(Player p)
    {
    }
    #endregion
    #region Utils
    protected void SendSkillConfirmPacket(Player p, bool sendCostPacket = true)
    {
        p.SendSkillConfirmPacket(true, _keyCode, CanMoveDuringCast, sendCostPacket);
    }

    protected void SendSkillCollisionRequestPacket(Player p, CollisionType type, Vector3 startPos, Vector3 targetPos)
    {
        p.SendSkillCollisionRequestPacket(_keyCode, _requestId, type, startPos, targetPos);
        ++_requestId;
    }

    public float GetDuration()
    {
        if (_animName == null)
            return 0.05f;

        if (!DataManager.AnimLengthInfoDict[_characterType].ContainsKey(_animName))
            return 0.01f;

        return DataManager.AnimLengthInfoDict[_characterType][_animName].Length;
    }

    public KeyCode GetKeyCode()
    {
        return _keyCode;
    }

    // Tick 등에서 소비(가져가면 플래그 리셋)
    protected bool TryConsumeLatest(ref int requestId, out SkillCollisionProposal prop)
    {
        if(_collisions.TryGetValue(requestId, out prop))
        {
            _collisions.Remove(requestId);
            ++requestId;
            return true;
        }

        prop = default;
        return false;
    }


    #endregion
}
