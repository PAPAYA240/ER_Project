using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Lucene.Net.Index;
using Microsoft.VisualBasic;
using Server.Data;
using static Server.Data.DataUtils;
using static Server.Game.GameObject;

namespace Server.Game
{
    public class Hitbox
    {
        public Creature Creature { get; set; }
        public float PosX { get; set; } = 0;
        public float PosZ { get; set; } = 0;

        public Vector2 MousePos { get; set; } = new Vector2();
        public float ChargeRatio { get; set; } = 1;
        public CharacterType CharType { get; set; }
        public KeyCode KeyCode { get; set; }
        public int Team { get; set; }

        public SkillHitbox Data { get; set; }
        public int StartTick { get; set; } // Skill Start Time
        public int EndTick { get; set; } // Skill End Time

        public bool IsUsed { get; set; } = false;

        // Key: ObjectId, Value: Nothing
        public ConcurrentDictionary<int, byte> HitObjs = new ConcurrentDictionary<int, byte>();

        #region 추가 데이터
        public Dictionary<KeyCode, List<string>> Interactions { get; set; } = new Dictionary<KeyCode, List<string>>();
        public HashSet<Hitbox> InteractedHitboxes { get; } = new HashSet<Hitbox>();
        public Hitbox trackingHitbox { get; set; } = null;
        public MonsterType MonstType { get; set; }

        public RotationInfo Rot { get; set; }
        public Vector2 OffsetPos { get; set; } = new Vector2();

        public bool IsInteracted = true;
        public float OffsetRadius = 0;

        //public long DrawMeshDelayTimer { get; set; } = 0;
        public Vector2 Forward { get; set; }
        public Vector2 Right { get; set; }
        #endregion
    }

    public enum Subject { Subject_None, Self, Ally, Enemy, Q, W, E, R, T }

    public class CollisionManager
    {
        object _lock = new object();

        // Key: ObjectId
        private Dictionary<int, HashSet<Hitbox>> _hitboxDict = new Dictionary<int, HashSet<Hitbox>>();

        // 2타 
        Dictionary<CharacterType, Dictionary<KeyCode, KeyCode>> _hitboxChainDict = new Dictionary<CharacterType, Dictionary<KeyCode, KeyCode>>();

        // 아군 대상 스킬
        Dictionary<CharacterType, HashSet<KeyCode>> _allyHitSkillDict = new Dictionary<CharacterType, HashSet<KeyCode>>();

        List<Hitbox> _pendingHitboxes = new List<Hitbox>();

        private InteractionManager _interactionManager = new InteractionManager();

        Dictionary<CharacterType, Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>> _statusEffects // Buffs & Debuffs
            = new Dictionary<CharacterType, Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>>();

        public int CurTick { get; set; }

        public void Init()
        {
            // 2타 hitbox 세팅
            Dictionary<KeyCode, KeyCode> abigailChainDict = new Dictionary<KeyCode, KeyCode> { { KeyCode.Q, KeyCode.F1 } };
            _hitboxChainDict.Add(CharacterType.Abigail, abigailChainDict);

            SetUpAllyHitSkills();
            SetUpStatusEffects();
        }

        public Hitbox AddHitbox(Creature player, CharacterType charType, KeyCode keyCode, Vector2 mousePos = new Vector2(), float chargeRatio = 0)
        {
            Hitbox hitbox = null;
            lock (_lock)
            {
                SkillHitbox skillHitbox = DataManager.SkillHitboxDict[charType][keyCode];
                if (skillHitbox.EndFrame <= 0)
                    return null;

                hitbox = new Hitbox
                {
                    Creature = player,
                    PosX = player.PosInfo.PosX,
                    PosZ = player.PosInfo.PosZ,
                    ChargeRatio = chargeRatio,
                    CharType = charType,
                    KeyCode = keyCode,
                    Team = player.Info.Player.Team,
                    Data = skillHitbox,

                    MousePos = mousePos,
                    Interactions = ConvertProtoInteractionsToKeyCodeDictionary(skillHitbox.Interactions)
                };

                SettingPointType(hitbox);
                _pendingHitboxes.Add(hitbox);
            }            

            // 2타 hitbox 추가
            if(_hitboxChainDict.TryGetValue(charType, out Dictionary<KeyCode, KeyCode> chainDict))
            {
                if (chainDict.TryGetValue(keyCode, out KeyCode value))
                    AddHitbox(player, charType, value, mousePos, chargeRatio);
            }
            return hitbox;
        }
       
        public void Update()
        {
            RemoveExpired();
            UpdatePos();
        }

        // 충돌체 찾기
        public Hitbox FindCollision(int id, KeyCode key)
        {
            foreach (var nestedKvp in _hitboxDict[id])
            {
                if (nestedKvp.KeyCode == key)
                    return nestedKvp;
            }
            return null;
        }

        public void CheckAllCollisions(
            Dictionary<int, Dictionary<int, Player>> teams,
            ConcurrentDictionary<int, Monster> monsters,
            ConcurrentDictionary<int, Projectile> projectiles)
        {
            Dictionary<int, Dictionary<int, float>> damageDict = new Dictionary<int, Dictionary<int, float>>();

            CheckCollisionHit();

            CheckPlayerHit(teams, damageDict);
            CheckHit(monsters, damageDict);
            SendChangeHpPkts(teams, damageDict);
        }

        public void RemoveExpired()
        {
            List<Hitbox> removeQueue = new List<Hitbox>();

            foreach (HashSet<Hitbox> hitboxSet in _hitboxDict.Values)
            {
                foreach (Hitbox hitbox in hitboxSet)
                {
                    if (CurTick >= hitbox.EndTick || hitbox.IsUsed)
                        removeQueue.Add(hitbox);
                }
            }

            lock (_lock)
            {
                foreach (Hitbox hitbox in removeQueue)
                {
                    if (_hitboxDict.TryGetValue(hitbox.Creature.Id, out var set))
                        set.Remove(hitbox);
                }
            }
        }

        public void UpdatePos()
        {
            foreach (var set in _hitboxDict.Values)
            {
                foreach (Hitbox hitbox in set)
                {
                    UpdatePosProjectile(hitbox);
                    UpdateTransform(hitbox);

                    if (hitbox.Creature == null || hitbox.Data == null)
                        continue;
                    if (false == System.Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                        continue;
                    if (type != SkillType.SkillTrack)
                        continue;

                    Quaternion rot = new Quaternion(
                        hitbox.Creature.RotInfo.Qx,
                        hitbox.Creature.RotInfo.Qy,
                        hitbox.Creature.RotInfo.Qz,
                        hitbox.Creature.RotInfo.Qw
                    );

                    Vector3 offset = new Vector3(hitbox.Data.RightOffset, 0, hitbox.Data.LookOffset);

                    Vector3 rotatedOffset = Vector3.Transform(offset, rot);

                    hitbox.PosX = hitbox.Creature.PosInfo.PosX + rotatedOffset.X;
                    hitbox.PosZ = hitbox.Creature.PosInfo.PosZ + rotatedOffset.Z;

                }
            }
        }

        void CheckPlayerHit(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            foreach (var nestedKvp in _hitboxDict)
            {
                int ownerId = nestedKvp.Key;
                HashSet<Hitbox> hitboxes = nestedKvp.Value;
                if (hitboxes.Count == 0)
                    continue;

                int myTeam = ObjectManager.Instance.GetTeam(ownerId);

                foreach (var hitbox in hitboxes)
                {
                    if (CurTick < hitbox.StartTick || CurTick > hitbox.EndTick)
                        continue;

                    List<Player> hitPlayers = new List<Player>();

                    foreach (var teamKvp in teams)
                    {
                        int teamId = teamKvp.Key;
                        if (teamId == myTeam)
                        {
                            HandleAllyHit(hitbox, teamKvp.Value);
                            continue;
                        }                            

                        HandleCollision<Player>(hitbox, teamKvp.Value, hitPlayers, damageDict);
                    }

                    if(hitPlayers.Count > 0)
                    {
                        HandleDamage<Player>(hitbox, hitPlayers, damageDict);
                        HandleStatusEffects<Player>(hitbox, hitPlayers);
                    }
                }
            }
        }

        void CheckHit<T>(IDictionary<int, T> targets, Dictionary<int, Dictionary<int, float>> damageDict) where T : GameObject, new()
        {
            foreach (var nestedKvp in _hitboxDict)
            {
                int ownerId = nestedKvp.Key;
                HashSet<Hitbox> hitboxes = nestedKvp.Value;
                if (hitboxes.Count == 0)
                    continue;

                foreach (var hitbox in hitboxes)
                {
                    if (CurTick < hitbox.StartTick || CurTick > hitbox.EndTick)
                        continue;

                    List<T> hitTargets = new List<T>();

                    HandleCollision<T>(hitbox, targets, hitTargets, damageDict);
                    if (hitTargets.Count > 0)
                    {
                        HandleDamage<T>(hitbox, hitTargets, damageDict);
                        HandleStatusEffects<T>(hitbox, hitTargets);
                    }
                }
            }
        }

        void HandleCollision<T>(Hitbox hitbox, IDictionary<int, T> targets, List<T> hitTargets, Dictionary<int, Dictionary<int, float>> damageDict) where T : GameObject, new()
        {
            foreach (var targetKvp in targets)
            {
                T target = targetKvp.Value;
                if (hitbox.HitObjs.ContainsKey(targetKvp.Key) || true == hitbox.IsUsed)
                    continue;

                if (CheckCollision(hitbox, target))
                {
                    hitTargets.Add(target);
                    HandlerInteraction(hitbox, target);
                }
            }
        }

        void HandleDamage<T>(Hitbox hitbox, List<T> hitTargets, Dictionary<int, Dictionary<int, float>> damageDict) where T : GameObject, new()
        {
            if (false == hitbox.Data.IsOneTimeUse) // 단일대상 히트박스가 아닌 경우
            {
                foreach (T target in hitTargets)
                    ApplyDamage(hitbox, target, damageDict);                 
            }
            else
            {
                T target = FindNearestTarget(hitbox, hitTargets);
                if (target == null) return;

                ApplyDamage(hitbox, target, damageDict);
                hitbox.IsUsed = true;
            }
        }

        T FindNearestTarget<T>(Hitbox hitbox, List<T> targets) where T : GameObject, new()
        {
            T nearestTarget = null;
            float nearestDistSq = float.MaxValue;
            foreach (var target in targets)
            {
                float dx = target.PosInfo.PosX - hitbox.PosX;
                float dz = target.PosInfo.PosZ - hitbox.PosZ;
                float distSq = dx * dx + dz * dz;

                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearestTarget = target;
                }
            }
            return nearestTarget;
        }

        bool CheckCollision(Hitbox hitbox, GameObject go)
        {
            if (!System.Enum.TryParse<SkillShape>(hitbox.Data.Shape, out var shape))
                return false;

            switch (shape)
            {
                case SkillShape.Circle:
                    {
                        float dx = go.PosInfo.PosX - hitbox.PosX;
                        float dz = go.PosInfo.PosZ - hitbox.PosZ;
                        float distanceSq = dx * dx + dz * dz;

                        float hitboxRadius = hitbox.Data.Radius + hitbox.OffsetRadius;
                        float totalRadius = hitboxRadius + go.Radius;

                        return distanceSq <= totalRadius * totalRadius;
                    }
                case SkillShape.Rectangle:
                    {
                        Vector2 center = hitbox.MousePos;

                        Vector2 forward = Vector2.Normalize(new Vector2(
                            center.X - hitbox.Creature.PosInfo.PosX,
                            center.Y - hitbox.Creature.PosInfo.PosZ));

                        Vector2 right = new Vector2(-forward.Y, forward.X);

                        Vector2 toTarget = new Vector2(
                            go.PosInfo.PosX - center.X,
                            go.PosInfo.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = hitbox.Data.Height * 0.5f;
                        float halfWidth = hitbox.Data.Width * 0.5f;

                        // 사각형 내부의 최근접점 찾기
                        float clampedForward = MathF.Max(-halfHeight, MathF.Min(projForward, halfHeight));
                        float clampedRight = MathF.Max(-halfWidth, MathF.Min(projRight, halfWidth));

                        // 최근접점에서 원 중심까지 거리 제곱 계산
                        float deltaForward = projForward - clampedForward;
                        float deltaRight = projRight - clampedRight;

                        float distSq = deltaForward * deltaForward + deltaRight * deltaRight;

                        // 거리 <= 반지름이면 충돌
                        return distSq <= go.Radius * go.Radius;
                    }
                case SkillShape.Point:
                    {
                        Vector2 center = hitbox.MousePos;
                        Vector2 playerPos = new Vector2(hitbox.Creature.PosInfo.PosX, hitbox.Creature.PosInfo.PosZ);
                        Vector2 forward = Vector2.Normalize(center - playerPos);
                        Vector2 right = new Vector2(-forward.Y, forward.X);

                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - center.X, go.PosInfo.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = hitbox.Data.Height * 0.5f;
                        float halfWidth = hitbox.Data.Width * 0.5f;

                        float clampedForward = MathF.Max(-halfHeight, MathF.Min(projForward, halfHeight));
                        float clampedRight = MathF.Max(-halfWidth, MathF.Min(projRight, halfWidth));

                        float deltaForward = projForward - clampedForward;
                        float deltaRight = projRight - clampedRight;

                        float distSq = deltaForward * deltaForward + deltaRight * deltaRight;

                        return distSq <= go.Radius * go.Radius;
                    }

                case SkillShape.Ray:
                    {
                        Vector2 origin = new Vector2(hitbox.PosX, hitbox.PosZ);
                        Vector2 forward = Vector2.Normalize(hitbox.MousePos - origin);
                        Vector2 right = new Vector2(-forward.Y, forward.X);
                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - origin.X, go.PosInfo.PosZ - origin.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        if (!Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                            return false;

                        float range = hitbox.Data.MaxRange;
                        if (type == SkillType.SkillTrack)
                            range = hitbox.Data.MinRange + (hitbox.Data.MaxRange - hitbox.Data.MinRange) * hitbox.ChargeRatio;

                        float halfWidth = hitbox.Data.Width * 0.5f;

                        float clampedForward = MathF.Max(0f, MathF.Min(projForward, range));
                        float clampedRight = MathF.Max(-halfWidth, MathF.Min(projRight, halfWidth));

                        float deltaForward = projForward - clampedForward;
                        float deltaRight = projRight - clampedRight;
                        float distSq = deltaForward * deltaForward + deltaRight * deltaRight;

                        return distSq <= go.Radius * go.Radius;
                    }
                case SkillShape.Sector:
                    {
                        Vector2 center = new Vector2(hitbox.PosX, hitbox.PosZ);
                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - center.X, go.PosInfo.PosZ - center.Y);

                        float dist = toTarget.Length();
                        if (dist > hitbox.Data.Radius + go.Radius)
                            return false;

                        if (dist <= go.Radius)
                            return true;

                        Vector2 mouseDir = Vector2.Normalize(new Vector2(hitbox.MousePos.X - center.X, hitbox.MousePos.Y - center.Y));
                        Vector2 targetDir = toTarget / dist;

                        float dot = Math.Clamp(Vector2.Dot(mouseDir, targetDir), -1f, 1f);
                        float angleRad = MathF.Acos(dot);

                        float halfAngleRad = (hitbox.Data.Angle * 0.5f) * (MathF.PI / 180f);
                        if (angleRad <= halfAngleRad)
                            return true;

                        float sin = MathF.Sin(halfAngleRad);
                        float cos = MathF.Cos(halfAngleRad);

                        Vector2 leftDir = new Vector2(mouseDir.X * cos - mouseDir.Y * sin, mouseDir.X * sin + mouseDir.Y * cos);
                        Vector2 rightDir = new Vector2(mouseDir.X * cos + mouseDir.Y * sin, -mouseDir.X * sin + mouseDir.Y * cos);

                        float leftDist = MathF.Abs(toTarget.X * leftDir.Y - toTarget.Y * leftDir.X);
                        float rightDist = MathF.Abs(toTarget.X * rightDir.Y - toTarget.Y * rightDir.X);

                        return (leftDist <= go.Radius || rightDist <= go.Radius);
                    }
            }
            return false;
        }

        void ApplyDamage(Hitbox hitbox, GameObject target, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            float dmg = CalcDamage(hitbox.Creature, target.Stat, hitbox.KeyCode);

            if (target is Player)
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Creature.Id}, Target:{target.Info.Player.CharType}_{target.Id}, Damage:{dmg}");
            else if (target is Monster)
            {
                Monster monster = target as Monster;
                monster.OnHit(hitbox.Creature);
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Creature.Id}, Target:{target.Info.Monster.MonsterType}_{target.Id}, Damage:{dmg}");
            }
            else
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Creature.Id}, Target:Env_{target.Id}, Damage:{dmg}");

            if (damageDict.ContainsKey(target.Id))
            {
                if (damageDict[target.Id].ContainsKey(hitbox.Creature.Id))
                {
                    damageDict[target.Id][hitbox.Creature.Id] += dmg;
                }
                else
                    damageDict[target.Id][hitbox.Creature.Id] = dmg;
            }
            else
            {
                damageDict[target.Id] = new Dictionary<int, float>();
                damageDict[target.Id][hitbox.Creature.Id] = dmg;
            }
            hitbox.HitObjs.TryAdd(target.Id, 0);
        }

        public float CalcDamage(Creature attacker, Player target, KeyCode keyCode)
        {
            // 플레이어가 플레이어 때릴 때
            StatInfo info = target.Stat.Clone();
            info.Defense = target.Defense;
            info.MaxHp = target.MaxHp;
            return CalcDamage(attacker, info, keyCode);
        }

        public float CalcDamage(Creature attacker, StatInfo target, KeyCode keyCode)
        {
            // 플레이어가 몬스터 때릴 때
            // TODO 버프 디버프 정보도 가지고 와야함. 예를 들면 방깍 디버프 같은거 
            Player playerAttacker = attacker as Player;
            if (playerAttacker == null) return 0f;

            Skill skill = playerAttacker.GetSkill(keyCode);

            float damage = skill.GetSkillDamage()
                + skill.SkillData.scaling.adRatio * playerAttacker.Attack * 0.01f
                + skill.SkillData.scaling.apRatio * playerAttacker.SkillAmplification * 0.01f
                + skill.SkillData.scaling.dstCurHpRatio * target.Hp * 0.01f
                + skill.SkillData.scaling.dstMaxHpRatio * target.MaxHp * 0.01f
                + skill.SkillData.scaling.srcCurHpRatio * playerAttacker.Hp * 0.01f
                + skill.SkillData.scaling.srcMaxHpRatio * playerAttacker.MaxHp * 0.01f;

            // 그냥 예시 : 추가 데미지를 입힐 시에
            if (playerAttacker.IsSkillAmplification)
                damage += skill.GetSkillBonusDamage();

            float result = damage;

            return result;
        }
        void SendChangeHpPkts(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            foreach (var kvp in damageDict)
            {
                GameObject hitTarget = ObjectManager.Instance.Find(kvp.Key);
                if (hitTarget == null)
                    continue;

                foreach (var attakerKvp in kvp.Value)
                {
                    GameObject attacker = ObjectManager.Instance.Find(attakerKvp.Key);
                    if (attacker == null)
                        continue;

                    float damage = attakerKvp.Value;
                    hitTarget.Room.Push(hitTarget.OnDamaged, attacker, damage, false);
                }
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                foreach (Hitbox pendingHitbox in _pendingHitboxes)
                {

                    if (!_hitboxDict.TryGetValue(pendingHitbox.Creature.Id, out var set))
                    {
                        set = new HashSet<Hitbox>();
                        _hitboxDict[pendingHitbox.Creature.Id] = set;
                    }

                    pendingHitbox.StartTick = CurTick + (int)((pendingHitbox.Data.StartFrame / (float)pendingHitbox.Data.Fps) * 1000);
                    pendingHitbox.EndTick = CurTick + (int)((pendingHitbox.Data.EndFrame / (float)pendingHitbox.Data.Fps) * 1000);

                    set.Add(pendingHitbox);
                }
                _pendingHitboxes.Clear();
            }
        }

        void SetUpAllyHitSkills() // 아군 대상 스킬 
        {
            foreach(var nestedKvp in DataManager.SkillDict)
            {
                foreach(var kvp in nestedKvp.Value)
                {
                    if(kvp.Value.levels.TryGetValue(1, out SkillLevel skillLevel))
                    {
                        if (skillLevel.effects == null)
                            continue;

                        foreach (EffectData effect in skillLevel.effects)
                        {
                            if (effect.condition == "AllyHit") // 아군 적중
                            {
                                if (!_allyHitSkillDict.ContainsKey(nestedKvp.Key))
                                    _allyHitSkillDict[nestedKvp.Key] = new HashSet<KeyCode>();

                                _allyHitSkillDict[nestedKvp.Key].Add(kvp.Key); // Key: CharactorType, Value: Keycode
                            }
                        }
                    }
                }
            }
        }

        void HandleAllyHit(Hitbox hitbox, Dictionary<int, Player> targets)
        {
            foreach (var targetKvp in targets)
            {
                Player target = targetKvp.Value;
                if (hitbox.HitObjs.ContainsKey(targetKvp.Key) || true == hitbox.IsUsed)
                    continue;

                if (CheckCollision(hitbox, target))
                {
                    Player skillOwner = hitbox.Creature as Player;
                    Skill skill = skillOwner.GetSkill(hitbox.KeyCode);
                    SkillLevel skillLevel = skill.SkillData.levels[skill.CurLevel];
                    if (skillLevel.effects.Count == 0)
                        continue;

                    foreach (EffectData effect in skillLevel.effects)
                    {
                        if(effect.type == "Heal")
                            target.Room.Push(target.OnHeal, target, effect.value);
                    }

                    hitbox.HitObjs.TryAdd(target.Id, 0);
                }
            }
        }

        #region StatusEffects(버프, 디버프, 방어막)
        void SetUpStatusEffects() // 조건이 Hit인 효과들만 추려내기
        {
            foreach (var nestedKvp in DataManager.SkillDict)
            {
                foreach (var kvp in nestedKvp.Value)
                {
                    foreach(var levelKvp in kvp.Value.levels)
                    {
                        if (levelKvp.Value.effects == null)
                            continue;

                        foreach(EffectData effectData in levelKvp.Value.effects)
                        {
                            CharacterType charType = nestedKvp.Key;
                            KeyCode keyCode = kvp.Key;
                            int level = levelKvp.Key;

                            if (effectData.condition == "Hit")
                            {
                                if (!_statusEffects.TryGetValue(charType, out var skillDict))
                                    _statusEffects[charType] = skillDict = new Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>();

                                if (!skillDict.TryGetValue(keyCode, out var levelDict))
                                    skillDict[keyCode] = levelDict = new Dictionary<int, List<StatusEffect>>();

                                if (!levelDict.TryGetValue(level, out var effects))
                                    levelDict[level] = effects = new List<StatusEffect>();

                                StatusEffect newEffect = new StatusEffect
                                {
                                    type = effectData.type,
                                    stat = effectData.stat,
                                    duration = effectData.duration,
                                    value = effectData.value,
                                    subject = Enum.TryParse(effectData.subject, true, out Subject temp) ? temp : Subject.Subject_None,
                                    coeff = effectData.coeff,
                                    ratioPerTarget = effectData.ratioPerTarget,
                                    maxRatio = effectData.maxRatio
                                };

                                effects.Add(newEffect);
                            }
                        }
                    }
                }
            }
        }

        void HandleStatusEffects<T>(Hitbox hitbox, List<T> hitTargets) where T : GameObject, new ()
        {
            if (!(hitbox.Creature is Player))
                return;

            if (!_statusEffects.TryGetValue(hitbox.CharType, out var nestedDict))
                return;

            if (!nestedDict.TryGetValue(hitbox.KeyCode, out var dict))
                return;

            Player player = hitbox.Creature as Player;

            if (!dict.TryGetValue(player.GetSkillLevel(hitbox.KeyCode), out var statusEffectList))
                return;

            foreach (var effect in statusEffectList)
            {
                effect.targetCnt = hitTargets.Count;
                effect.attacker = hitbox.Creature;

                switch (effect.subject)
                {
                    case Subject.Self:
                        player.Room.Push(player.Room.AddStatusEffect, player, effect);
                        break;
                    case Subject.Ally: // 이건 아군대상 스킬에만 있을거같긴해서 생략
                        break;
                    case Subject.Enemy:
                        foreach(var enemy in hitTargets.OfType<Creature>()) // Creature 일때만
                            enemy.Room.Push(enemy.Room.AddStatusEffect, enemy, effect);
                        break;
                    case Subject.T:
                        if(effect.type == "CDR")
                            player.Skill.Reduce(KeyCode.T, effect.value);
                        break;
                }
            }
        }
        #endregion

        #region 추가
        public Hitbox AddHitbox(Creature creature, MonsterSkill skilltype, Vector2 targetPos = new Vector2(), float chargeRatio = 0)
        {
            Hitbox hitbox = null;
            lock (_lock)
            {
                SkillHitbox skillHitbox = DataManager.MonstSkillHitboxDict[creature.Info.Monster.MonsterType][skilltype];
                if (skillHitbox.EndFrame <= 0)
                    return null;

                hitbox = new Hitbox
                {
                    Creature = creature,
                    PosX = creature.PosInfo.PosX,
                    PosZ = creature.PosInfo.PosZ,
                    ChargeRatio = chargeRatio,
                    MonstType = creature.Info.Monster.MonsterType,
                    Data = skillHitbox,
                    MousePos = targetPos,
                    OffsetPos = new Vector2(skillHitbox.RightOffset, skillHitbox.LookOffset),
                    Interactions = ConvertProtoInteractionsToKeyCodeDictionary(skillHitbox.Interactions)
                };

                SettingPointType(hitbox);
                _pendingHitboxes.Add(hitbox);
            }
            return hitbox;
        }
        void SettingPointType(Hitbox hitbox)
        {
            if (System.Enum.TryParse<SkillShape>(hitbox.Data.Shape, out var shape))
            {
                if (shape == SkillShape.Point)
                {
                    Vector2 center = hitbox.MousePos;
                    Vector2 playerPos = new Vector2(hitbox.Creature.PosInfo.PosX, hitbox.Creature.PosInfo.PosZ);

                    hitbox.Forward = Vector2.Normalize(center - playerPos);
                    hitbox.Right = new Vector2(-hitbox.Forward.Y, hitbox.Forward.X);

                    hitbox.PosX = hitbox.MousePos.X;
                    hitbox.PosZ = hitbox.MousePos.Y;
                }
            }

        }
        private void UpdateTransform(Hitbox hitbox)
        {
            if (hitbox.Creature == null || hitbox.Data == null)
                return;
            if (false == System.Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                return;
            if (false == Enum.TryParse<SkillShape>(hitbox.Data.Shape, out var shape))
                return;

            // =============== [위치] ==================== 
            // 마우스 기준 생성
            if (shape == SkillShape.Point)
            {
                Vector2 origin = new Vector2(hitbox.Creature.PosInfo.PosX, hitbox.Creature.PosInfo.PosZ);
                Vector2 forward = new Vector2();

                hitbox.PosX = hitbox.MousePos.X + hitbox.OffsetPos.X;
                hitbox.PosZ = hitbox.MousePos.Y + hitbox.OffsetPos.Y;
                forward = Vector2.Normalize(hitbox.MousePos - origin);
                return;
            }

            // =============== [회전] ==================== 
            Vector2 mforward = new Vector2();
            RotationInfo currentRotInfo = null;

            if (hitbox.Creature is Player player)
            {
                Vector2 or = new Vector2(player.PosInfo.PosX, player.PosInfo.PosZ);
                mforward = Vector2.Normalize(hitbox.MousePos - or);

                float yawAngleRadians = MathF.Atan2(mforward.X, mforward.Y);
                Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yawAngleRadians);
                currentRotInfo = new RotationInfo { Qx = rotation.X, Qy = rotation.Y, Qz = rotation.Z, Qw = rotation.W };
            }
            else if (hitbox.Creature is Monster monster)
            {
                Quaternion monsterRot = new Quaternion(
                    monster.RotInfo.Qx, monster.RotInfo.Qy,
                    monster.RotInfo.Qz, monster.RotInfo.Qw
                );
                Vector3 forward3D = Vector3.Transform(new Vector3(0, 0, 1), monsterRot);
                mforward = Vector2.Normalize(new Vector2(forward3D.X, forward3D.Z));
                currentRotInfo = monster.RotInfo;
            }
            if (currentRotInfo == null) return;

            Vector2 right = new Vector2(-mforward.Y, mforward.X);
            hitbox.Rot = currentRotInfo;
            hitbox.Forward = mforward;
            hitbox.Right = right;
        }

        private void UpdatePosProjectile(Hitbox hitbox)
        {
            if (Enum.TryParse<SkillType>(hitbox.Data.Type, out var type) && type == SkillType.SkillProjectile)
            {
                Quaternion rot = new Quaternion(
                  hitbox.Creature.RotInfo.Qx,
                  hitbox.Creature.RotInfo.Qy,
                  hitbox.Creature.RotInfo.Qz,
                  hitbox.Creature.RotInfo.Qw
                );

                // TODO : 움직임 속도 예시 (데이터로 변경 예정)
                // : 움직임 플레이어에 영향 받지 못하게 고정해야 함
                const float moveSpeed = 10;
                Vector3 toForward = Vector3.Transform(new Vector3(0, 0, 1), rot);
                const float TickInterval = 1.0f / 70.0f;
                float deltaMove = moveSpeed * TickInterval;

                hitbox.PosX += toForward.X * deltaMove;
                hitbox.PosZ += toForward.Z * deltaMove;
            }
        }
        bool CheckCollision(Hitbox myHitbox, Hitbox targetHitbox)
        {
            if (!System.Enum.TryParse<SkillShape>(myHitbox.Data.Shape, out var shape))
                return false;

            switch (shape)
            {
                case SkillShape.Circle:
                    {
                        float dx = targetHitbox.PosX - myHitbox.PosX;
                        float dz = targetHitbox.PosZ - myHitbox.PosZ;
                        float distanceSq = dx * dx + dz * dz;

                        float targetRadius = targetHitbox.Data.Radius;
                        float effectiveRadius = myHitbox.Data.Radius + targetRadius;

                        return distanceSq <= effectiveRadius * effectiveRadius;
                    }
                case SkillShape.Rectangle:
                case SkillShape.Point:
                case SkillShape.Ray:
                    return CheckPointRayCollision(myHitbox, targetHitbox);

                case SkillShape.Sector:
                    {
                        Vector2 center = new Vector2(myHitbox.PosX, myHitbox.PosZ);
                        Vector2 toTarget = new Vector2(targetHitbox.PosX - center.X, targetHitbox.PosZ - center.Y);

                        if (toTarget.LengthSquared() > myHitbox.Data.Radius * myHitbox.Data.Radius)
                            return false;

                        Vector2 mouseDir = Vector2.Normalize(new Vector2(myHitbox.MousePos.X - center.X, myHitbox.MousePos.Y - center.Y));
                        Vector2 targetDir = Vector2.Normalize(toTarget);

                        float dot = Math.Clamp(Vector2.Dot(mouseDir, targetDir), -1f, 1f);
                        float angleDeg = MathF.Acos(dot) * (180f / MathF.PI);

                        return angleDeg <= myHitbox.Data.Angle * 0.5f;
                    }
            }

            return false;
        }

        // 충돌체 끼리의 충돌
        void CheckCollisionHit()
        {
            List<Hitbox> allHitboxes = new List<Hitbox>();
            foreach (HashSet<Hitbox> hitboxSet in _hitboxDict.Values)
                allHitboxes.AddRange(hitboxSet);

            for (int i = 0; i < allHitboxes.Count; i++)
            {
                for (int j = i + 1; j < allHitboxes.Count; j++)
                {
                    Hitbox hitboxA = allHitboxes[i];
                    Hitbox hitboxB = allHitboxes[j];

                    if (hitboxA.InteractedHitboxes.Contains(hitboxB) || hitboxB.InteractedHitboxes.Contains(hitboxA))
                        continue;

                    if (!hitboxA.IsInteracted || !hitboxB.IsInteracted)
                        continue;

                    if (CheckCollision(hitboxA, hitboxB))
                        HandlerInteraction(hitboxA, hitboxB);
                }
            }
        }

        bool CheckPointRayCollision(Hitbox pointHitbox, Hitbox rayHitbox)
        {
            Vector2 centerA = pointHitbox.MousePos;
            Vector2 playerPosA = new Vector2(pointHitbox.Creature.PosInfo.PosX, pointHitbox.Creature.PosInfo.PosZ);
            Vector2 forwardA = Vector2.Normalize(centerA - playerPosA);
            Vector2 rightA = new Vector2(-forwardA.Y, forwardA.X);
            float halfHeightA = pointHitbox.Data.Height * 0.5f;
            float halfWidthA = pointHitbox.Data.Width * 0.5f;


            Vector2 originB = new Vector2(rayHitbox.PosX, rayHitbox.PosZ);
            Vector2 forwardB = Vector2.Normalize(rayHitbox.MousePos - originB);
            float rangeB = rayHitbox.Data.Height;
            float halfHeightB = rangeB * 0.5f;
            float halfWidthB = rayHitbox.Data.Width * 0.5f;
            Vector2 centerB = originB + forwardB * halfHeightB;
            Vector2 rightB = new Vector2(-forwardB.Y, forwardB.X);

            // 1. OBB 대 OBB 충돌
            Vector2 toTarget = new Vector2(centerB.X - centerA.X, centerB.Y - centerA.Y);

            // 2.OBB A (Point) 축 검사
            float projCenterA1 = Vector2.Dot(toTarget, forwardA);
            float projRadiusA1 = halfHeightA + MathF.Abs(Vector2.Dot(forwardA, forwardB)) * halfHeightB + MathF.Abs(Vector2.Dot(forwardA, rightB)) * halfWidthB;
            if (MathF.Abs(projCenterA1) > projRadiusA1) return false;

            float projCenterA2 = Vector2.Dot(toTarget, rightA);
            float projRadiusA2 = halfWidthA + MathF.Abs(Vector2.Dot(rightA, forwardB)) * halfHeightB + MathF.Abs(Vector2.Dot(rightA, rightB)) * halfWidthB;
            if (MathF.Abs(projCenterA2) > projRadiusA2) return false;

            // 3. OBB B 축 검사
            float projCenterB1 = Vector2.Dot(toTarget, forwardB);
            float projRadiusB1 = halfHeightB + MathF.Abs(Vector2.Dot(forwardB, forwardA)) * halfHeightA + MathF.Abs(Vector2.Dot(forwardB, rightA)) * halfWidthA;
            if (MathF.Abs(projCenterB1) > projRadiusB1) return false;

            float projCenterB2 = Vector2.Dot(toTarget, rightB);
            float projRadiusB2 = halfWidthB + MathF.Abs(Vector2.Dot(rightB, forwardA)) * halfHeightA + MathF.Abs(Vector2.Dot(rightB, rightA)) * halfWidthA;
            if (MathF.Abs(projCenterB2) > projRadiusB2) return false;

            Vector2 toPointCenter = new Vector2(centerA.X - originB.X, centerA.Y - originB.Y);
            float projPointForward = Vector2.Dot(toPointCenter, forwardB);

            if (!System.Enum.TryParse<SkillType>(rayHitbox.Data.Type, out SkillType type))
                return true;

            float range = rayHitbox.Data.MaxRange;
            if (type == SkillType.SkillTrack)
                range = rayHitbox.Data.MinRange + (rayHitbox.Data.MaxRange - rayHitbox.Data.MinRange) * rayHitbox.ChargeRatio;

            float pointHalfHeight = pointHitbox.Data.Height * 0.5f;

            bool isWithinStart = projPointForward >= (0 - pointHalfHeight);

            bool isWithinEnd = projPointForward <= (range + pointHalfHeight);

            return isWithinStart && isWithinEnd;
        }

        void HandlerInteraction(Hitbox hitboxA, Hitbox hitboxB)
        {
            hitboxA.InteractedHitboxes.Add(hitboxB);
            hitboxB.InteractedHitboxes.Add(hitboxA);

            _interactionManager.HandleInteraction(hitboxA, hitboxB);
            _interactionManager.HandleInteraction(hitboxB, hitboxA);
        }

        void HandlerInteraction(Hitbox hitbox, GameObject target)
        {
            _interactionManager.HandleInteraction(hitbox, target);
        }

        //public void SendDrawMesh(Hitbox hitbox)
        //{
        //    Vector2 playerPos2D = new Vector2(hitbox.Creature.PosInfo.PosX, hitbox.Creature.PosInfo.PosZ);

        //    S_Drawmesh pkt = new S_Drawmesh
        //    {
        //        ObjectId = hitbox.Creature.Id,
        //        OffsetRadius = hitbox.OffsetRadius,
        //        PosInfo = new PositionInfo
        //        {
        //            PosX = hitbox.PosX,
        //            PosY = 0,
        //            PosZ = hitbox.PosZ
        //        },

        //        RotInfo = hitbox.Rot,

        //        Hitbox = hitbox.Data,

        //        Forward = new PositionInfo
        //        {
        //            PosX = hitbox.Forward.X,
        //            PosY = 0,
        //            PosZ = hitbox.Forward.Y
        //        },

        //        Right = new PositionInfo
        //        {
        //            PosX = hitbox.Right.X,
        //            PosY = 0,
        //            PosZ = hitbox.Right.Y
        //        }
        //    };

        //    hitbox.Creature.Room.Broadcast(pkt);
        //}
        #endregion
    }
}
