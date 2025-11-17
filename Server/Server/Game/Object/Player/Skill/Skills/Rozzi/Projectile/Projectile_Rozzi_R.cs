using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using static Server.Data.DataUtils;
using ValueType = Server.Game.ValueType;

public class Projectile_Rozzi_R : Projectile
{
    private readonly float _maxDistance = 5.5f;
    private Vector3 _startPosition;

    private long _tStartTick, _tEndTick;

    private readonly float _duration = 8f;
    private readonly float _speed = 10f;

    private readonly float[] maxHpDamageRatio = { 0, 3, 6, 9 };

    // 폭탄 상태
    private enum BombState
    {
        Flying,             // 날아가는 중
        AttachedToTarget,   // 적에게 부착
        StuckOnGround,      // 지면에 부착
        Exploded            // 폭발 완료
    }

    private BombState _state = BombState.Flying;

    // 부착 대상
    private GameObject _target;
    public GameObject Target => _target;
    private long _attachTick;
    private long _explodeTick;

    // 히트 스택
    private int _hitStack = 0;
    private const int MaxStack = 5;
    private const int FuseMs = 3000;   

    public override void Init()
    {
        if (Owner == null)
            return;

        _tStartTick = TimeUtil.Instance.LastTick;
        _tEndTick = unchecked(_tStartTick + (int)MathF.Round(_duration * 1000f));

        // Owner의 현재 위치를 복사
        Info.PosInfo = new PositionInfo
        {
            PosX = Owner.PosInfo.PosX,
            PosY = Owner.PosInfo.PosY,
            PosZ = Owner.PosInfo.PosZ
        };
        Info.RotInfo = Owner.RotInfo;

        _startPosition = Info.PosInfo.ToVector();
    }

    // CollisionManager에서 R 스킬 히트가 발생했을 때 호출됨
    // (CollisionManager.CheckPlayerHit 내부에서 호출 중)
    public void OnProjectileHit(GameObject target)
    {
        if (_state != BombState.Flying)
            return;

        if (target is Creature creature)
        {
            AttachToTarget(creature);
            Console.WriteLine("@ Rozzi R Hit!!! Attach bomb to target");
        }
    }

    public override void Update()
    {
        if (Owner == null)  // TEMP: Owner가 죽어도 남아있기?
            return;

        // 상태별 업데이트
        switch (_state)
        {
            case BombState.Flying:
                if (Deactivation())
                {
                    Console.WriteLine("@ Leave Game : Projectile_Rozzi_R (timeout while Flying)");
                    Room.Push(Room.LeaveGame, Id);
                    return;
                }

                UpdatePositionWhileFlying();
                break;

            case BombState.AttachedToTarget:
                // 타겟이 죽었거나 사라지면 그냥 제자리 폭발
                if (_target == null || _target.IsDead)
                {
                    Explode(false);
                    return;
                }

                // 타겟을 따라다님
                Info.PosInfo.SetPosInfoFromVector3(_target.Position);
                Info.PosInfo.PosY = 1.5f;
                SendMovePacket(PosInfo, RotInfo);

                if (TimeUtil.Instance.IsPastOrNow(_explodeTick))
                {
                    Explode(false);
                    return;
                }
                break;

            case BombState.StuckOnGround:
                if (TimeUtil.Instance.IsPastOrNow(_explodeTick))
                {
                    Explode(false);
                    return;
                }
                break;

            case BombState.Exploded:
                // 이미 폭발 처리, Room.LeaveGame 대기
                break;
        }
    }

    private void AttachToTarget(Creature target)
    {
        _state = BombState.AttachedToTarget;
        _target = target;

        _attachTick = TimeUtil.Instance.LastTick;
        _explodeTick = unchecked(_attachTick + FuseMs);

        // 부착 시 이속 디버프 (3초)
        Room.Push(Room.AddStatusEffect, Owner as Player, _target, KeyCode.R, "Attach");
        Console.WriteLine("@ AttachToTarget : Slow!");

        // TODO : 시야 공유 시스템이 있으면 여기서 연동
        // e.g. target.Room.Vision.Share(Owner, target);

        // 위치를 타겟에게 붙임
        Info.PosInfo.SetPosInfoFromVector3(target.Position);
        Info.PosInfo.PosY = 1.5f;
        SendMovePacket(PosInfo, RotInfo);
    }

    private void AttachToGround()
    {
        if (_state != BombState.Flying)
            return;

        _state = BombState.StuckOnGround;

        _attachTick = TimeUtil.Instance.LastTick;
        _explodeTick = unchecked(_attachTick + FuseMs);

        // 더 이상 이동하지 않고 해당 위치에 고정
        // (FX 연출은 클라에서 패킷 보고 처리)
        Console.WriteLine("@ Rozzi R stick on ground");
    }

    protected override bool Deactivation()
    {
        if (TimeUtil.Instance.IsPastOrNow(_tEndTick))
            return true;

        return false;
    }

    // Owner가 어떤 대상을 때렸을 때 CollisionManager에서 호출
    // isSkillHit = true 이면 스킬 적중(2스택), false 이면 기본 공격(1스택)
    public void RegisterOwnerHit(bool isSkillHit)
    {
        if (_state != BombState.AttachedToTarget)
            return;
        if (_target == null)
            return;

        Console.WriteLine("@ RegisterOwnerHit");

        int add = isSkillHit ? 2 : 1;
        _hitStack += add;

        // 5스택 이상이면 조기 폭발
        if (_hitStack >= MaxStack)
        {
            Explode(true);
            Console.WriteLine("@@@ Early Explode!!!");
        }
    }

    private void UpdatePositionWhileFlying()
    {
        // 이미 부착되거나 터졌으면 이동 안 함
        if (_state != BombState.Flying)
            return;

        if (Vector3.Distance(Position, _startPosition) >= _maxDistance)
        {
            // 최대 사거리 도달 → 지면 부착
            AttachToGround();
            return;
        }

        Vector3 forwardVector = Info.RotInfo.Forward();
        Vector3 moveDistance = forwardVector * _speed * TimeUtil.Instance.DeltaTime;

        Vector3 myCurPosition = Info.PosInfo.ToVector();
        Vector3 targetPos = myCurPosition + moveDistance;

        if (Vector3.Distance(targetPos, _startPosition) >= _maxDistance)
            targetPos = _startPosition + forwardVector * _maxDistance;

        Info.PosInfo.SetPosInfoFromVector3(targetPos);
        Info.PosInfo.PosY = 1.5f;
        SendMovePacket(PosInfo, RotInfo);
    }

    private void Explode(bool early)
    {
        if (_state == BombState.Exploded)
            return;

        _state = BombState.Exploded;

        Vector3 explosionPos = Position;
        GameObject mainTarget = _target;

        if (mainTarget != null)
            explosionPos = mainTarget.Position;

        // 1) 폭발 피해
        ApplyExplosionDamage();

        // TODO : 바닥 폭탄인 경우 여기서 AoE 처리도 가능

        // 2) 조기 폭발 추가 효과
        if (early && mainTarget != null && !mainTarget.IsDead)
            ApplyEarlyExplosionEffects(mainTarget);

        // TODO : 폭발 FX 패킷 전송 (S_SemtexBoom 등)

        // TODO : 시야 공유를 했었다면 여기서 해제

        // 마지막으로 투사체 제거
        Console.WriteLine($"@ Rozzi R Bomb Explode (early:{early})");
        Room.Push(Room.LeaveGame, Id);
    }

    public void ApplyExplosionDamage()
    {
        if (Owner == null)
            return;

        Vector2 targetPos = new Vector2(Position.X, Position.Z);
        Room.CollManager.AddHitbox(Owner, Owner.CharType, KeyCode.F2, targetPos);

        // 조기 폭발일 때 비례 피해는 별도 함수에서 추가
    }

    private void ApplyEarlyExplosionEffects(GameObject target)
    {
        if (target == null || Owner == null)
            return;

        Player player = Owner as Player;
        // (1) 대상 최대 체력 비례 고정 피해
        float extraDamage = target.Stat.MaxHp * (maxHpDamageRatio[player.GetSkillLevel(KeyCode.R)] * 0.01f);
        Console.WriteLine($"=> Damage : {extraDamage}");
        if (extraDamage > 0)
            Room.Push(target.OnDamaged, Owner, extraDamage, true, false);

        // (2) 대상 1초 이속 디버프
        StatusEffect slowEffect = new StatusEffect
        {
            type = "Debuff",
            stat = "MoveSpeed",
            duration = 1f,
            value = -30,
            valueType = ValueType.Ratio
        };
        target.Room.Push(target.AddStatusEffect, slowEffect);

        // (3) R 스킬 쿨타임 50% 환급 (valueType.Ratio 로 해석하는 구현을 썼다고 가정)
        player.Skill.Reduce(KeyCode.R, 50, true);

        // (4) 로지 1초 이속 버프
        GameObject.StatusEffect selfBuff = new GameObject.StatusEffect
        {
            type = "Buff",
            stat = "MoveSpeed",
            duration = 1f,
            value = 30,
            valueType = ValueType.Ratio
        };
        Owner.Room.Push(Owner.AddStatusEffect, selfBuff);
    }
}

