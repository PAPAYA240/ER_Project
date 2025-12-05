using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Player_StunState;
using static Server.Game.StunState;

namespace Server.Game
{
    public class GameObject
    {
        #region Player Info
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
        public int Id
        {
            get { return Info.ObjectId; }
            set { Info.ObjectId = value; }
        }

        public GameRoom Room { get; set; }

        public CharacterType CharType => Info.Player.CharType;

        ObjectInfo _objectInfo = new ObjectInfo()
        {
            StatInfo = new StatInfo(),
            PosInfo = new PositionInfo(),
            RotInfo = new RotationInfo() { Qw = 1f },
            ScaleInfo = new ScaledInfo() { ScaledX = 1f, ScaledY = 1f, ScaledZ = 1f, }
        };

        public ObjectInfo Info
        {
            get { return _objectInfo; }
            set { _objectInfo = value; PosInfo = value.PosInfo; RotInfo = value.RotInfo; Stat = value.StatInfo; ScaleInfo = value.ScaleInfo; }
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
        public ScaledInfo ScaleInfo
        {
            get { return Info.ScaleInfo; }
            set
            {
                if (Info.ScaleInfo.Equals(value))
                    return;

                ScaleInfo = value;
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
        protected const string STAT_ATTACK_SPEED = "AttackSpeed";
        protected const string STAT_DEFENSE = "Defense";
        protected const string STAT_HEALING = "Healing";

        // 인스턴스별 퍼센트 누적( +0.20f = +20% )
        protected readonly Dictionary<StatusEffect, (string key, float delta)> _mulByInst = new Dictionary<StatusEffect, (string key, float delta)>();
        // key : effects.stat
        protected readonly Dictionary<string, float> _mulBuffAccum = new Dictionary<string, float>(); // 버프 전용
        protected readonly Dictionary<string, float> _mulDebuffAccum = new Dictionary<string, float>(); // 디버프 전용

        // 인스턴스별 고정수치(Flat) 누적
        protected readonly Dictionary<StatusEffect, (string key, float delta)> _flatByInst = new Dictionary<StatusEffect, (string key, float delta)>();
        protected readonly Dictionary<string, float> _flatBuffAccum = new Dictionary<string, float>();
        protected readonly Dictionary<string, float> _flatDebuffAccum = new Dictionary<string, float>();

        protected bool _isUpdatedStatus = false;
        public void UpdateStatusFlag(bool isUpdated = true) => _isUpdatedStatus = isUpdated;
        protected bool _isCcImmune = false;
        public bool IsCcImmune { get { return _isCcImmune; } set { _isCcImmune = value; } }

        public bool IsDead => State == CreatureState.Dead;

        public bool _isHit = false;

        public bool IsHit
        {
            get { return _isHit; }
            set { _isHit = value; }
        }

        public virtual CreatureState State
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }

        #region CombatState
        // CombatState
        protected float _combatTime = 0f;
        protected readonly float _nonCombatTime = 5f;
        public float CombatTime
        {
            get { return _combatTime; }
            set { _combatTime = value; }
        }

        private CombatState _curCombat;

        public CombatState CombatState
        {
            get { return _curCombat; }
            set { _curCombat = value; }
        }
        #endregion

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
        public int MonsterTeam
        {
            get { return Info.Monster.Team; }
            set { Info.Monster.Team = value; }
        }
        #endregion

        public virtual void Update() { }

        #region Damage
        public bool IsAttackable()
        {
            if (State == CreatureState.Dead)
                return false;

            return true;
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

            Player player = this as Player;
            if (player != null)
            {
                CombatState = CombatState.Combat;
                S_CombatMode combatModePkt = new S_CombatMode();
                combatModePkt.CombatMode = CombatState;
                Room.Push(player.Session.Send, combatModePkt);
                CombatTime = 0f;
            }
            IsHit = true;
        }

        private void AttackInfo(GameObject attacker)
        {
            string attackKey = "";
            bool isAttackerValid = false;

            if (attacker is Player playerAttack)
            {
                if (playerAttack.Info.Player.CharType == CharacterType.Abigail)
                    return;                 

                isAttackerValid = true;
                Player_SkillState skillstate = playerAttack.CurrentState as Player_SkillState;

                if (skillstate != null)
                    attackKey = skillstate.Handler.GetKeyCode().ToString();
                else
                {
                     attackKey = "Attack";
                }
            }
            else if (attacker is Monster monsterAttack)
            {
                isAttackerValid = true;
                attackKey = monsterAttack.CurrentSkill.ToString();
            }

            if (isAttackerValid)
            {
                S_AttackInfo attackInfoPacket = new S_AttackInfo
                {
                    ObjectId = this.Id,          
                    AttackerId = attacker.Id,  
                    AttackType = attackKey,   
                };

                Room.Broadcast(attackInfoPacket);
            }
        }
        protected virtual void OnDamaged(GameObject attacker, float damage, bool isBasicAttack = false)
        {
            AttackInfo(attacker);

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

        public float CalcFinalDamage(GameObject attacker, float damage)
        {
            float finalDefense = Defense * (1f - attacker.PercentageDefensePenetration * 0.01f) - attacker.FixedDefensePenetration;
            float finalDamage = damage * 100f / (100f + finalDefense);
            return finalDamage;
        }
        #endregion

        #region State
        public virtual void OnHeal(GameObject go, float heal)
        {
            if (Room == null || State == CreatureState.Dead || heal <= 0)
                return;

            Hp = Math.Min(MaxHp, Hp + heal);

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            Room.Broadcast(changePacket);

            S_CombatText combatTextPkt = new S_CombatText();
            combatTextPkt.ObjectId = Id;
            combatTextPkt.Type = CombatTextType.HpRecovery;
            combatTextPkt.Value = heal;
            Room.Broadcast(combatTextPkt);
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
            room.Push(room.LeaveGame, Id);

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

            GameObjectType type = ObjectManager.GetObjectTypeById(Id);
            if (type == GameObjectType.Player)
            {
                Player player = this as Player;
                if (player == null)
                    return;

                room.Push(room.EnterGame, this, player.Info.Player.Team);
            }
            else
                room.Push(room.EnterGame, this, 0);
        }
        #endregion

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
            public long startTick; // 시작시간
            public Subject subject; // 적용대상
            public ValueType valueType; // Ratio or Flat

            public float coeff; // 스킬 계수  ex) (+스킬 증폭의 2%)
            public float ratioPerTarget; // 대상 1명 추가당 증가량 (ex: 아비게일 W: 추가로 적중한 적 하나 당 보호막량 20% 증가)
            public float maxRatio;       // 최대 증가량

            public int targetCnt; // 적중한 대상 갯수

            public Creature attacker; // 시전자

            public string condition; // 적용조건
        }

        public void AddStatusEffect(StatusEffect statusEffect)
        {
            lock (_lock)
            {
                statusEffect.startTick = TimeUtil.Instance.LastTick;

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
                        stunPacket.AttackerId = statusEffect.attacker.Id;
                        stunPacket.AttackerTeam = statusEffect.attacker.Info.Player.Team;
                        stunPacket.Duration = 4/*statusEffect.duration*/;
                        Room.Broadcast(stunPacket);

                        if (this is Player player)
                        {
                            player.ChangeState(new Player_StunState(new StunStateDesc
                            {
                                Duration = statusEffect.duration
                            }));
                        }
                        else if (this is Monster monster)
                        {
                            monster.ChangeState(new StunState(new MonsterStunDesc
                            {
                                Duration = statusEffect.duration
                            }));
                        }
                    }
                    else if (statusEffect.type == "Pyosik")
                    {
                        S_AddYukiPyosik yukiPyosikPkt = new S_AddYukiPyosik();
                        yukiPyosikPkt.ObjectId = Id;
                        yukiPyosikPkt.AttackerId = statusEffect.attacker.Id;
                        yukiPyosikPkt.Position = new PositionInfo
                        {
                            PosX = PosInfo.PosX,
                            PosY = PosInfo.PosY,
                            PosZ = PosInfo.PosZ
                        };
                        Room.Broadcast(yukiPyosikPkt);

                        Player player = statusEffect.attacker as Player;
                        player.Room.Push(player.Room.BroadcastAbigailSound, player, AbigailSound.YukiRattack, 1f);
                        player.Room.Push(player.Room.BroadcastAbigailSound, player, AbigailSound.YukiRdebuff, 1f);
                        // 유키 궁 표식 데미지
                        int curLevel = player.GetSkillLevel(Data.DataUtils.KeyCode.R);
                        float curAttack = player.Attack;
                        _ = CoDelayYukiCoupDeGrace(player, curAttack, curLevel, 1000);
                    }
                    else if (statusEffect.type == "Buff" || statusEffect.type == "Debuff")
                    {
                        if (statusEffect.valueType == ValueType.Ratio)
                        {
                            // value가 20(%) 형태로 들어올 수도 있으니 0~1로 정규화
                            float pct = statusEffect.value;
                            if (MathF.Abs(pct) > 1f)
                                pct *= 0.01f;

                            RegisterMultiplier(statusEffect, statusEffect.stat, pct);
                        }
                        else
                        {
                            RegisterFlat(statusEffect, statusEffect.stat, statusEffect.value);
                        }

                        RegisterCommonEffect(statusEffect, statusEffect.stat);
                    }
                    else if(statusEffect.type == "Untargetable")
                    {
                        Player player = this as Player;
                        if (player != null)
                            player.SendUntargetablePacket(true);
                    }
                    else if (statusEffect.type == "Unstoppable")
                    {
                        Player player = this as Player;
                        if (player != null)
                        {
                            UpdateUnstoppable(true);
                            player.SendUnstoppablePacket(true);
                        }
                    }
                }                    
            }
        }

        // Yuki pyosik damage coroutine
        List<float> FixedDamage = new List<float> { 0.06f, 0.1f, 0.14f };
        private async Task CoDelayYukiCoupDeGrace(Player atk, float curAttack, int curLevel, int delayMs)
        {
            await Task.Delay(delayMs);

            SendYukiSkillEffect(SkillEffectType.RHit);
            atk.Room.Push(atk.Room.BroadcastAbigailSound, atk, AbigailSound.YukiRdebuffHit, 1f);
            atk.Room.Push(atk.Room.BroadcastAbigailSound, atk, AbigailSound.YukiRend, 1f);

            float damage = MaxHp * (FixedDamage[curLevel - 1] + (curAttack * 0.05f) * 0.01f);
            Room.Push(OnDamaged, atk, damage, true, false);
        }

        public int RemoveStatusEffects(string type, string stat = null) // 해당 종류의 상태효과 모두 제거
        {
            lock (_lock)
            {
                var toRemove = _statusEffects
                    .Where(se => se.type == type && (stat == null || se.stat == stat))
                    .ToList();

                foreach (var se in toRemove)
                    OnStatusEffectRemove(se);

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

                if (earliest != null)
                {
                    OnStatusEffectRemove(earliest);
                    _statusEffects.Remove(earliest);
                }
            }
        }

        public void RemoveAllStatusEffects() // 전체 상태효과 제거
        {
            lock (_lock)
            {
                var toRemove = _statusEffects.ToList();

                foreach (var se in toRemove)
                    OnStatusEffectRemove(se);

                _statusEffects.Clear();
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
                if (unchecked(TimeUtil.Instance.LastTick - effect.startTick) >= effect.duration * 1000f)
                    expired.Add(effect);
            }

            foreach (var effect in barrierSnapshot)
            {
                if (unchecked(TimeUtil.Instance.LastTick - effect.startTick) >= effect.duration * 1000f)
                    expiredBarriers.Add(effect);
            }

            lock (_lock)
            {
                foreach (var e in expired)
                {
                    OnStatusEffectRemove(e);
                    _statusEffects.Remove(e);
                }

                foreach (var s in expiredBarriers)
                    _barriers.Remove(s);

                if (expiredBarriers.Count > 0)
                    UpdateBarrier();
            }
        }

        public bool FindStatStatusEffect(string name)
        {
            foreach (StatusEffect effect in _statusEffects)
            {
                if (effect.stat == name)
                    return true;
            }
            return false;
        }
        void OnStatusEffectRemove(StatusEffect statusEffect)
        {
            if (statusEffect.type == "Buff" || statusEffect.type == "Debuff")
            {
                if (statusEffect.valueType == ValueType.Ratio)
                    UnregisterMultiplier(statusEffect);
                else
                    UnregisterFlat(statusEffect);

                UnregisterCommonEffect(statusEffect, statusEffect.stat);
            }
            else if (statusEffect.type == "Untargetable")
            {
                Player player = this as Player;
                if (player != null)
                    player.SendUntargetablePacket(false);
            }
            else if (statusEffect.type == "Unstoppable")
            {
                Player player = this as Player;
                if (player != null)
                {
                    UpdateUnstoppable(false);
                    player.SendUnstoppablePacket(false);
                }
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

        public bool IsUntargetable()
        {
            foreach (var effect in _statusEffects)
            {
                if (effect.type == "Untargetable")
                    return true;
            }
            return false;
        } // 대상지정불가 상태인지 아닌지

        public bool IsUnstoppable()
        {
            foreach (var effect in _statusEffects)
            {
                if (effect.type == "Unstoppable")
                    return true;
            }
            return false;
        } // 저지불가 상태인지 아닌지

        protected void UpdateUnstoppable(bool isUnStoppable)
        {
            IsCcImmune = isUnStoppable;
            UpdateStatusFlag();
        }

        public bool IsVisionShare()
        {
            foreach (StatusEffect effect in _statusEffects)
            {
                if (effect.type == "Coord" || effect.type == "VisionShare")
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region StatusEffect 연동 
        // 최종 = 비율 → 고정 순 합성
        protected float ComposeFinal(string key, float baseVal, bool ignoreDebuff = false, float mulbuffOffset = 0f)
        {
            float mulBuff = _mulBuffAccum.GetValueOrDefault(key) + mulbuffOffset;
            float mulDebuff = ignoreDebuff ? 0f : _mulDebuffAccum.GetValueOrDefault(key);

            float flatBuff = _flatBuffAccum.GetValueOrDefault(key);
            float flatDebuff = ignoreDebuff ? 0f : _flatDebuffAccum.GetValueOrDefault(key);

            float result = baseVal * (1f + mulBuff + mulDebuff) + flatBuff + flatDebuff;
            return MathF.Max(0f, result);
        }

        // 인스턴스 등록 : 비율(%) 누적
        protected void RegisterMultiplier(StatusEffect inst, string key, float delta)
        {
            if (MathF.Abs(delta) < 1e-9f)
                return;

            _mulByInst[inst] = (key, delta);

            if (inst.type == "Buff")
                _mulBuffAccum[key] = _mulBuffAccum.GetValueOrDefault(key) + delta;
            else if (inst.type == "Debuff")
                _mulDebuffAccum[key] = _mulDebuffAccum.GetValueOrDefault(key) + delta;

            UpdateStatusFlag();
        }

        // 인스턴스 등록 : 고정수치 누적
        protected void RegisterFlat(StatusEffect inst, string key, float delta)
        {
            if (MathF.Abs(delta) < 1e-9f)
                return;

            _flatByInst[inst] = (key, delta);

            if (inst.type == "Buff")
                _flatBuffAccum[key] = _flatBuffAccum.GetValueOrDefault(key) + delta;
            else if (inst.type == "Debuff")
                _flatDebuffAccum[key] = _flatDebuffAccum.GetValueOrDefault(key) + delta;

            UpdateStatusFlag();
        }

        // 인스턴스 제거 : 해당 인스턴스가 더했던 비율 제거
        protected void UnregisterMultiplier(StatusEffect inst)
        {
            if (!_mulByInst.TryGetValue(inst, out var pair))
                return;

            var (key, delta) = pair;
            _mulByInst.Remove(inst);

            if (inst.type == "Buff")
            {
                _mulBuffAccum[key] = _mulBuffAccum.GetValueOrDefault(key) - delta;
                if (MathF.Abs(_mulBuffAccum[key]) < 1e-6f)
                    _mulBuffAccum.Remove(key);
            }
            else if (inst.type == "Debuff")
            {
                _mulDebuffAccum[key] = _mulDebuffAccum.GetValueOrDefault(key) - delta;
                if (MathF.Abs(_mulDebuffAccum[key]) < 1e-6f)
                    _mulDebuffAccum.Remove(key);
            }

            UpdateStatusFlag();
        }

        // 인스턴스 제거 : 해당 인스턴스가 더했던 고정수치 제거
        protected void UnregisterFlat(StatusEffect inst)
        {
            if (!_flatByInst.TryGetValue(inst, out var pair))
                return;

            var (key, delta) = pair;
            _flatByInst.Remove(inst);

            if (inst.type == "Buff")
            {
                _flatBuffAccum[key] = _flatBuffAccum.GetValueOrDefault(key) - delta;
                if (MathF.Abs(_flatBuffAccum[key]) < 1e-6f)
                    _flatBuffAccum.Remove(key);
            }
            else if (inst.type == "Debuff")
            {
                _flatDebuffAccum[key] = _flatDebuffAccum.GetValueOrDefault(key) - delta;
                if (MathF.Abs(_flatDebuffAccum[key]) < 1e-6f)
                    _flatDebuffAccum.Remove(key);
            }

            UpdateStatusFlag();
        }

        // 이펙트 등록
        protected void RegisterCommonEffect(StatusEffect inst, string key)
        {
            string fxName = ResolveCommonEffect(inst, key);
            SendCommonSkillEffect(default, commonName: fxName, type: "Caster");     
        }

        // 이펙트 제거
        protected void UnregisterCommonEffect(StatusEffect inst, string key)
        {
            string fxName = ResolveCommonEffect(inst, key);
            SendRemoveCommonEffect(isCaster: true, commonName: fxName);
        }

        protected string ResolveCommonEffect(StatusEffect inst, string key)
        {
            if (inst.type == "Debuff")
            {
                switch (key)
                {
                    case "MoveSpeed":
                        return "Debuff_Slow";
                    case "Healing":
                        return "Debuff_HealedDecrease";
                }
            }

            return "";
        }
        #endregion

        #region Packet
        public void SendMovePacket(PositionInfo posInfo, RotationInfo rotInfo)
        {
            S_Move packet = new S_Move()
            {
                ObjectId = Id,
                PosInfo = posInfo,
                RotInfo = rotInfo
            };

            Room?.Push(Room.Broadcast, packet);
        }

        public void SendYukiSkillEffect(SkillEffectType type, bool isPlay = true)
        {
            S_SkillEffect pkt = new S_SkillEffect
            {
                ObjectId = Id,
                EffectType = type,
                IsPlay = isPlay
            };

            Room.Push(Room.Broadcast, pkt);
        }

        public virtual void SendCommonSkillEffect( Vector2 mousePos, string commonName = "", string type = "Caster", 
            string fxName = "", bool useTargetTransform = false, int targetId = 0) { }

        public virtual void SendRemoveCommonEffect(bool isCaster, string commonName, string fxName = "") { }
        #endregion
    }
}
