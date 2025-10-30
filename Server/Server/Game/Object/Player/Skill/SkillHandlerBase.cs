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

public abstract class SkillHandlerBase : ISkill
{
    public virtual bool CanMoveDuringCast => false;
    public virtual float MoveSpeedMultiplier => 1.0f;

    public int                      LastSeq { get { return _lastSeq; } set { _lastSeq = value; } }
    public SkillCollisionProposal   Latest { get { return _latest; } set { _latest = value; } }

    protected int _lastSeq;
    protected SkillCollisionProposal _latest;
    protected bool _committed;
    protected Vector3 _finalEnd;

    protected CharacterType _characterType;
    protected string _animName;
    protected KeyCode _keyCode;

    public virtual void OnEnter(Player p, SkillContext ctx)
    {
        LastSeq = 0;
        Latest = default;
        _committed = false;

        // 애니메이션 패킷 전송
        p.SendAnimPacket(_animName, 0.05f);

        // 이동 잠금
        if (CanMoveDuringCast == false)
        {
            // 이동 금지 스킬이면 강제 정지
            p.SendStopPacket(StopReason.StopMoveOnly);
        }
    }

    public virtual void OnExit(Player p, SkillContext ctx)
    {
        // 최종 보정 1회
        p.PosInfo.PosX = _finalEnd.X;
        p.PosInfo.PosY = _finalEnd.Y;
        p.PosInfo.PosZ = _finalEnd.Z;
        p.SendMovePacket(new PositionInfo(p.PosInfo), new RotationInfo(p.RotInfo));
    }

    public virtual void OnHit(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public virtual void OnTick(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public virtual void OnPropose(Player p, in SkillCollisionProposal prop)
    {
        if (_committed)
            return;

        if (prop.Seq <= LastSeq)
            return;

        LastSeq = prop.Seq;
        Latest = prop;
    }

    public virtual bool CanCast(Player p, SkillContext ctx)
    {
        return true;
    }

    #region Utils
    public float GetDuration()
    {
        if (_animName == null)
            return 0.05f;

        return DataManager.AnimLengthInfoDict[_characterType][_animName].Length;
    }

    public KeyCode GetKeyCode()
    {
        return _keyCode;
    }

    protected SkillSpec GetSkillSpec(bool isCast = true)
    {
        if(isCast)
            return DataManager.SkillSpecDict[_characterType][_keyCode].cast;
        else
            return DataManager.SkillSpecDict[_characterType][_keyCode].followup;
    }

    // Tick 등에서 소비(가져가면 플래그 리셋)
    protected bool TryConsumeLatest(out SkillCollisionProposal prop)
    {
        if (LastSeq <= 0)
        { 
            prop = default; 
            return false; 
        }

        prop = _latest;
        return true;
    }
    #endregion
}
