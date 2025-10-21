using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf.Protocol;
using Lucene.Net.Store;
using static System.Net.Mime.MediaTypeNames;
using static Server.Game.GameObject;

namespace Server.Game
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
        public int Id
        {
            get { return Info.ObjectId; }
            set { Info.ObjectId = value; }
        }

        public GameRoom Room { get; set; }

        ObjectInfo _objectInfo = new ObjectInfo()
        {
            StatInfo = new StatInfo(),
            PosInfo = new PositionInfo(),
            RotInfo = new RotationInfo() { Qw = 1f }
        };

        public ObjectInfo Info
        {
            get { return _objectInfo; }
            set { _objectInfo = value; PosInfo = value.PosInfo; RotInfo = value.RotInfo; Stat = value.StatInfo; }
        }

        public PositionInfo PosInfo
        {
            get { return Info.PosInfo; }
            set
            {
                if (Info.PosInfo.Equals(value))
                    return;

                PosInfo = value;
                State = value.State;
            }
        }

        public RotationInfo RotInfo
        {
            get { return Info.RotInfo; }
            set
            {
                if (value == null)
                    return;

                if (Info.RotInfo.Equals(value))
                    return;

                Info.RotInfo.Qx = value.Qx;
                Info.RotInfo.Qy = value.Qy;
                Info.RotInfo.Qz = value.Qz;
                Info.RotInfo.Qw = value.Qw;
            }
        }

        public StatInfo Stat 
        {
            get
            {
                if (Info.StatInfo == null)
                    Info.StatInfo = new StatInfo();
                return Info.StatInfo;
            }
            set
            {
                if (Info.StatInfo == null)
                    Info.StatInfo = new StatInfo();

                if (!Info.StatInfo.Equals(value))
                    Info.StatInfo.MergeFrom(value);
            }
        }

        public virtual float Speed
        {
            get { return Stat.MoveSpeed; }
            set { Stat.MoveSpeed = value; }
        }

        public virtual float HpRegen
        {
            get { return Stat.HpRegen; }
            set { Stat.HpRegen = Math.Max(value, 0); }
        }

        public virtual float Hp
        {
            get { return Stat.Hp; }
            set { Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp); }
        }

        public virtual float MaxHp
        {
            get { return Stat.MaxHp; }
            set { Stat.MaxHp = Math.Max(value, 0); }
        }

        public float Barrier
        {
            get { return Stat.Barrier; }
        }

        public virtual float StaminaRegen
        {
            get { return Stat.StaminaRegen; }
            set { Stat.StaminaRegen = Math.Max(value, 0); }
        }

        public virtual float Stamina
        {
            get { return Stat.Stamina; }
            set { Stat.Stamina = Math.Clamp(value, 0, Stat.MaxStamina); }
        }

        public virtual float MaxStamina
        {
            get { return Stat.MaxStamina; }
            set { Stat.MaxStamina = Math.Max(value, 0); }
        }

        public virtual float Attack
        {
            get { return Stat.Attack; }
            set { Stat.Attack = Math.Max(value, 0); }
        }

        public virtual float FixedDefensePenetration
        {
            get { return 0f; }
        }

        public virtual float PercentageDefensePenetration
        {
            get { return 0f; }
        }

        public virtual float Defense
        {
            get { return Stat.Defense; }
            set { Stat.Defense = Math.Max(value, 0); }
        }

        public CreatureState State
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }

        public virtual void Update()
        {
            UpdateController();
        }

        protected virtual void UpdateController()
        {
            //switch (State)
            //{
            //    case CreatureState.Idle:
            //        break;
            //    case CreatureState.Moving:
            //        break;
            //    case CreatureState.Attack:
            //        break;
            //    case CreatureState.Skill:
            //        break;
            //    case CreatureState.Dead:
            //        break;
            //    case CreatureState.Rest:
            //        break;
            //}
        }

        public virtual void OnDamaged(GameObject attacker, float damage)
        {
            if (Room == null || State == CreatureState.Dead)
                return;

            float finalDefense = Defense * (1f - attacker.PercentageDefensePenetration * 0.01f) - attacker.FixedDefensePenetration;
            float finalDamage = damage * 100f / (100f + finalDefense);

            //배리어가 흡수할 수치 계산
            float absorbed = Math.Min(Barrier, finalDamage);
            ReduceBarrier(absorbed);
            float remaining = finalDamage - absorbed;
            Hp = Math.Max(0, Hp - remaining);

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            changePacket.Barrier = Barrier;

            //temp
            changePacket.Damages.Add(new DamageInfo { Damage = remaining, Type = DamageType.Ad });

            if (absorbed > 0)
                changePacket.Absorbed = absorbed;

            Room.Broadcast(changePacket);

            if(Hp <= 0)
            {
                OnDead(attacker);
            }
        }

        public virtual void OnHeal(GameObject go, float heal)
        {
            if (Room == null || State == CreatureState.Dead)
                return;

            Hp = Math.Min(MaxHp, Hp + heal);

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            Room.Broadcast(changePacket);
        }

        public virtual void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;

            S_Die diePacket = new S_Die();
            diePacket.ObjectId = Id;
            diePacket.AttackerId = attacker.Id;
            Room.Broadcast(diePacket);

            GameRoom room = Room;
            room.LeaveGame(Id);

            Hp = MaxHp;
            Stamina = MaxStamina;
            State = CreatureState.Idle;
            PosInfo.PosX = 0;
            PosInfo.PosY = 0;
            PosInfo.PosZ = 0;
            RotInfo.Qx = 0;
            RotInfo.Qy = 0;
            RotInfo.Qz = 0;
            RotInfo.Qw = 1;

            room.EnterGame(this);
        }

        object _lock = new object();

        HashSet<StatusEffect> _statusEffects = new HashSet<StatusEffect>(); // Buffs & Debuffs
        List<StatusEffect> _barriers = new List<StatusEffect>(); // 방어막 전용

        #region StatusEffect(버프, 디버프), Barrier(방어막) 관련

        public class StatusEffect
        {
            public string type; // ex) 둔화, 기절, 속박  /  이동속도 증가
            public string stat;
            public float value; // ex) 둔화량 20퍼 or 이동속도 증가 20퍼
            public float duration; // 지속시간
            public int startTick; // 시작시간
            public Subject subject; // 적용대상
        }

        public void AddStatusEffect(StatusEffect statusEffect)
        {
            lock (_lock)
            {
                statusEffect.startTick = Room.CurTick;

                if (statusEffect.stat == "barrier")
                {
                    _barriers.Add(statusEffect);
                    UpdateBarrier();
                }                    
                else
                    _statusEffects.Add(statusEffect);
            }
        }

        public void RemoveExpiredStatusEffects()
        {
            List<StatusEffect> expired = new List<StatusEffect>();
            List<StatusEffect> expiredBarriers = new List<StatusEffect>();

            List<StatusEffect> snapshot;
            List<StatusEffect> barrierSnapshot;

            lock (_lock)
            {
                snapshot = _statusEffects.ToList<StatusEffect>();
                barrierSnapshot = _barriers.ToList<StatusEffect>();
            }


            foreach (var effect in snapshot)
            {
                if (unchecked(Room.CurTick - effect.startTick) >= effect.duration * 1000f)
                    expired.Add(effect);
            }

            foreach (var effect in barrierSnapshot)
            {
                if (unchecked(Room.CurTick - effect.startTick) >= effect.duration * 1000f)
                    expiredBarriers.Add(effect);
            }

            lock (_lock)
            {
                foreach (var e in expired)
                    _statusEffects.Remove(e);

                if (expiredBarriers.Count > 0)
                    _barrierUpdateRequired = true;

                foreach (var s in expiredBarriers)
                    _barriers.Remove(s);
            }
        }

        public void ReduceBarrier(float damage)
        {
            if (damage <= 0)
                return;
            
            lock (_lock)
            {
                if (_barriers.Count == 0)
                    return;

                float remaining = damage;

                // 만료된 보호막
                List<StatusEffect> expired = new List<StatusEffect>();

                foreach (var b in _barriers)
                {
                    // 남은 피해가 없으면
                    if (remaining <= 0) break;

                    // 방어막 감소
                    if (b.value >= remaining)
                    {
                        b.value -= remaining;
                        remaining = 0;
                    }
                    else
                    {
                        remaining -= b.value;
                        b.value = 0;
                    }

                    // 값이 0되면 만료 처리
                    if (b.value <= 0)
                        expired.Add(b);
                }

                // 보호막 제거
                foreach (var e in expired)
                    _barriers.Remove(e);

                UpdateBarrier();
            }
        }

        public void UpdateBarrier()
        {

        }

        #endregion
    }
}
