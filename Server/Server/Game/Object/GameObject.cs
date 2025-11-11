using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Numerics;
using Google.Protobuf.Protocol;
using Lucene.Net.Store;
using ServerCore;
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
            set { Stat.Barrier = Math.Max(value, 0); }
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

        public virtual float AttackSpeed
        {
            get { return Stat.AttackSpeed; }
            set { Stat.AttackSpeed = value; }
        }

        public virtual float Healing  
        {
            get { return Stat.Healing; }
            set { Stat.Healing = value; }
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

        protected const string STAT_MOVE_SPEED = "MoveSpeed";
        protected const string STAT_ATTACK = "Attack";
        protected const string STAT_DEFENSE = "defense";
        protected const string STAT_ATTACK_SPEED = "AttackSpeed";
        protected const string STAT_HEALING = "healing";

        // 인스턴스별 퍼센트 누적( +0.20f = +20% )
        protected readonly Dictionary<StatusEffect, (string key, float delta)> _mulByInst = new Dictionary<StatusEffect, (string key, float delta)>();
        protected readonly Dictionary<string, float> _mulAccum = new Dictionary<string, float>();

        // 인스턴스별 고정수치(Flat) 누적
        private readonly Dictionary<StatusEffect, (string key, float delta)> _flatByInst = new Dictionary<StatusEffect, (string key, float delta)>();
        private readonly Dictionary<string, float> _flatAccum = new Dictionary<string, float>();

        protected bool _isUpdatedStatus = false;

        public virtual CreatureState State
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }

        float _radius = 0.55f;

        public virtual float Radius // 피격 반경
        {
            get => _radius;
            set => _radius = value;
        }

        public Vector3 Position
        {
            get { return new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ); }
            set { PosInfo.PosX = value.X; PosInfo.PosY = value.Y; PosInfo.PosZ = value.Z; }
        }

        public int Team
        {
            get { return Info.Player.Team; }
            set { Info.Player.Team = value; }
        }

        public virtual void Update()
        {
            
        }

        public virtual void OnDamaged(GameObject attacker, float damage, bool isTrueDamage = false, bool isBasicAttack = false)
        {
            if (Room == null || State == CreatureState.Dead || State == CreatureState.Appear)
                return;

            if (isTrueDamage)
            {
                OnDamaged(attacker, damage, isBasicAttack);
            }
            else
            {
                float finalDefense = Defense * (1f - attacker.PercentageDefensePenetration * 0.01f) - attacker.FixedDefensePenetration;
                float finalDamage = damage * 100f / (100f + finalDefense);

                OnDamaged(attacker, finalDamage, isBasicAttack);
            }
        }

        private void OnDamaged(GameObject attacker, float damage, bool isBasicAttack = false)
        {
            //배리어가 흡수할 수치 계산
            float absorbed = Math.Min(Barrier, damage);
            ReduceBarrier(absorbed);
            float remaining = damage - absorbed;
            Hp = Math.Max(0, Hp - remaining);

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            changePacket.Barrier = Barrier;

            Player targetPlayer = this as Player;
            Player attackPlayer = attacker as Player;

            // 보호막 데미지 텍스트를 공격자와 피격자에게 보냄.
            if (absorbed > 0)
            {
                S_CombatText barrierTextPacket = new S_CombatText();
                barrierTextPacket.ObjectId = Id;
                barrierTextPacket.Type = CombatTextType.Barrier;
                barrierTextPacket.Value = absorbed;

                if(targetPlayer != null)
                {
                    targetPlayer.Session.Send(barrierTextPacket);
                }

                if(attackPlayer != null)
                {
                    attackPlayer.Session.Send(barrierTextPacket);
                }
            }

            // 데미지 텍스트를 공격자와 피격자에게 보냄.
            //TODO 데미지 타입을 받아와야함.
            if (remaining >= 1)
            {
                S_CombatText damageTextPacket = new S_CombatText();
                damageTextPacket.ObjectId = Id;
                if(isBasicAttack)
                    damageTextPacket.Type = CombatTextType.Ad;
                else
                    damageTextPacket.Type = CombatTextType.Ap;
                damageTextPacket.Value = remaining;

                if (targetPlayer != null)
                {
                    targetPlayer.Session.Send(damageTextPacket);
                }

                if (attackPlayer != null)
                {
                    attackPlayer.Session.Send(damageTextPacket);
                }
            }

            Room.Broadcast(changePacket);

            if (Hp <= 0)
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
            //diePacket.AttackerId = attacker.Id;
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

        #region StatusEffect(버프, 디버프), Barrier(방어막) 관련

        object _lock = new object();

        HashSet<StatusEffect> _statusEffects = new HashSet<StatusEffect>(); // Buffs & Debuffs
        protected List<StatusEffect> _barriers = new List<StatusEffect>(); // 방어막 전용

        public class StatusEffect
        {
            public string type; // ex) 둔화, 기절, 속박  /  이동속도 증가
            public string stat;
            public float value; // ex) 둔화량 20퍼 or 이동속도 증가 20퍼
            public float duration; // 지속시간
            public int startTick; // 시작시간
            public Subject subject; // 적용대상

            public float coeff; // 스킬 계수  ex) (+스킬 증폭의 2%)
            public float ratioPerTarget; // 대상 1명 추가당 증가량 (ex: 아비게일 W: 추가로 적중한 적 하나 당 보호막량 20% 증가)
            public float maxRatio;       // 최대 증가량

            public int targetCnt; // 적중한 대상 갯수

            public Creature attacker; // 시전자
        }

        public void AddStatusEffect(StatusEffect statusEffect, Creature atk)
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
                {
                    _statusEffects.Add(statusEffect);

                    if (statusEffect.type == "Coord")
                    {
                        S_AddAbigailCoord addAbigailCoordPkt = new S_AddAbigailCoord();
                        addAbigailCoordPkt.ObjectId = Id;
                        addAbigailCoordPkt.AttackerTeam = statusEffect.attacker.Info.Player.Team;
                        addAbigailCoordPkt.Duration = statusEffect.duration;
                        Room.Broadcast(addAbigailCoordPkt);
                    }
                    else if (statusEffect.type == "Snare")
                    {
                        S_Snare stunPacket = new S_Snare();
                        stunPacket.ObjectId = Id;
                        stunPacket.AttackerId = atk.Id;
                        stunPacket.AttackerTeam = statusEffect.attacker.Info.Player.Team;
                        stunPacket.Duration = statusEffect.duration;
                        Room.Broadcast(stunPacket);

                        // NAYOUNGTODO : Idle로 변경해야 함
                    }
                    else if(statusEffect.type == "Buff" || statusEffect.type == "Debuff")
                    {
                        // value가 20(%) 형태로 들어올 수도 있으니 0~1로 정규화
                        float pct = statusEffect.value;
                        if (MathF.Abs(pct) > 1f)
                            pct *= 0.01f;

                        RegisterMultiplier(statusEffect, statusEffect.stat, pct);
                    }
                }                    
            }
        }

        public int RemoveStatusEffects(string type, string stat = null) // 해당 종류의 상태효과 모두 제거
        {
            lock (_lock)
            {
                //return _statusEffects.RemoveWhere(se =>
                //    se.type == type &&
                //    se.stat == stat);

                var toRemove = _statusEffects
                    .Where(se => se.type == type && (stat == null || se.stat == stat))
                    .ToList();

                foreach (var se in toRemove)
                {
                    if (se.type == "Buff" || se.type == "Debuff")
                        UnregisterMultiplier(se);
                }

                int removed = 0;
                foreach (var se in toRemove)
                    if (_statusEffects.Remove(se))
                        removed++;

                return removed;
            }
        }

        public void RemoveFirstStatusEffect(string type, string stat = null) // 해당 종류의 상태효과 중 가장 먼저 삽입된 원소 제거
        {
            lock (_lock)
            {
                StatusEffect earliest = null;

                foreach (var se in _statusEffects)
                {
                    if (se.type == type && se.stat == stat)
                    {
                        if (earliest == null || se.startTick < earliest.startTick)
                            earliest = se;
                    }
                }

                //if (earliest != null)
                //    _statusEffects.Remove(earliest);

                if (earliest != null)
                {
                    if (earliest.type == "Buff" || earliest.type == "Debuff")
                        UnregisterMultiplier(earliest);

                    _statusEffects.Remove(earliest);
                }
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
                //foreach (var e in expired)
                //    _statusEffects.Remove(e);

                foreach (var e in expired)
                {
                    if (e.type == "Buff" || e.type == "Debuff")
                        UnregisterMultiplier(e);

                    _statusEffects.Remove(e);
                }

                foreach (var s in expiredBarriers)
                    _barriers.Remove(s);

                if (expiredBarriers.Count > 0)
                    UpdateBarrier();
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

        public virtual void UpdateBarrier()
        {
            float barrier = 0;

            foreach (var b in _barriers)
                barrier += b.value;

            Barrier = barrier;

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            changePacket.Barrier = Barrier;
            Console.WriteLine($"Barrier: {barrier}");
            Room.Push(Room.Broadcast, changePacket);
        }

        #endregion

        #region StatusEffect 연동 
        // 최종 = 비율 → 고정 순 합성
        protected float ComposeFinal(string key, float baseVal)
        {
            float mul = _mulAccum.GetValueOrDefault(key);  // 비율 합
            float flat = _flatAccum.GetValueOrDefault(key); // 고정값 합
            float result = baseVal * (1f + mul) + flat;
            return MathF.Max(0f, result);
        }

        // 인스턴스 등록 : 비율(%) 누적
        protected void RegisterMultiplier(StatusEffect inst, string key, float delta)
        {
            if (MathF.Abs(delta) < 1e-9f)
                return;

            if (_mulByInst.TryGetValue(inst, out var old))
            {
                // 같은 인스턴스가 스택 증가 등으로 '같은 키'에 누적되는 경우만 허용 => 스킬 계속 쓰면 중첩됨 맞나이게
                if (old.key != key)
                {
                    // 설계상 한 인스턴스=한 스탯이므로 키 변경은 비정상
                    // 필요 시 여기서 Remove 후 새로 등록하는 흐름으로 교체 가능
                    throw new InvalidOperationException("StatusEffect instance already bound to another stat key.");
                }
                var newDelta = old.delta + delta;
                _mulByInst[inst] = (key, newDelta);
                _mulAccum[key] = _mulAccum.GetValueOrDefault(key) + delta;
            }
            else
            {
                _mulByInst[inst] = (key, delta);
                _mulAccum[key] = _mulAccum.GetValueOrDefault(key) + delta;
            }

            Console.WriteLine($"@ RegisterMultiplier : Id - {Id}, key - {key}, value - {_mulAccum[key]}");
            _isUpdatedStatus = true;
        }

        // 인스턴스 등록 : 고정수치 누적
        protected void RegisterFlat(StatusEffect inst, string key, float delta)
        {
            if (MathF.Abs(delta) < 1e-9f)
                return;

            _flatByInst[inst] = (key, delta);
            _flatAccum[key] = _flatAccum.GetValueOrDefault(key) + delta;

            Console.WriteLine($"@ RegisterFlat : Id - {Id}, key - {key}, value - {_mulAccum[key]}");

            _isUpdatedStatus = true;
        }

        // 인스턴스 제거 : 해당 인스턴스가 더했던 비율 제거
        protected void UnregisterMultiplier(StatusEffect inst)
        {
            if (!_mulByInst.TryGetValue(inst, out var pair))
                return;

            var (key, delta) = pair;
            _mulByInst.Remove(inst);

            _mulAccum[key] = _mulAccum.GetValueOrDefault(key) - delta;
            if (MathF.Abs(_mulAccum[key]) < 1e-6f)
                _mulAccum.Remove(key);

            _isUpdatedStatus = true;
        }

        // 인스턴스 제거 : 해당 인스턴스가 더했던 고정수치 제거
        protected void UnregisterFlat(StatusEffect inst)
        {
            if (!_flatByInst.TryGetValue(inst, out var pair))
                return;
            var (key, delta) = pair;
            _flatByInst.Remove(inst);

            _flatAccum[key] = _flatAccum.GetValueOrDefault(key) - delta;
            if (MathF.Abs(_flatAccum[key]) < 1e-6f)
                _flatAccum.Remove(key);

            _isUpdatedStatus = true;
        }
        #endregion
    }
}
