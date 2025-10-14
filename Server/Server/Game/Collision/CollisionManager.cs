using Google.Protobuf.Protocol;
using System.Numerics;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static Server.Data.DataUtils;
using Lucene.Net.Index;
using Microsoft.VisualBasic;
using System.Linq;
using Google.Protobuf.Collections;

namespace Server.Game
{
    public class Hitbox
    {
        public Player Player { get; set; }
        public float PosX { get; set; } = 0;
        public float PosZ {  get; set; } = 0;

        public Vector2 MousePos { get; set; } = new Vector2();
        public float ChargeRatio { get; set; } = 1;
        public CharacterType CharType { get; set; }
        public KeyCode KeyCode { get; set; }
        public int Team {  get; set; } 
        
        public SkillHitbox Data { get; set; }
        public int StartTick { get; set; } // Skill Start Time
        public int EndTick { get; set; } // Skill End Time

        public bool IsUsed { get; set; } = false;

        // Key: ObjectId, Value: Nothing
        public ConcurrentDictionary<int, byte> HitObjs = new ConcurrentDictionary<int, byte>();

        // 내 충돌끼리만 가능
        public Dictionary<KeyCode, List<string>> Interactions { get; set; } = new Dictionary<KeyCode, List<string>>();

    }

    class CollisionManager
    {
        object _lock = new object();

        // Key: ObjectId
        Dictionary<int, HashSet<Hitbox>> _hitboxDict = new Dictionary<int, HashSet<Hitbox>>();

        // 2타 
        Dictionary<CharacterType, Dictionary<KeyCode, KeyCode>> _hitboxChainDict = new Dictionary<CharacterType, Dictionary<KeyCode, KeyCode>>();

        // 아군 대상 스킬
        Dictionary<CharacterType, HashSet<KeyCode>> _allyHitSkillDict = new Dictionary<CharacterType, HashSet<KeyCode>>();

        List<Hitbox> _pendingHitboxes = new List<Hitbox>();

        private InteractionManager _interactionManager = new InteractionManager();

        public int CurTick { get; set; }

        public void Init()
        {
            // 2타 hitbox 세팅
            Dictionary<KeyCode, KeyCode> abigailChainDict = new Dictionary<KeyCode, KeyCode> { { KeyCode.Q, KeyCode.F1 } };
            _hitboxChainDict.Add(CharacterType.Abigail, abigailChainDict);

            SetUpAllyHitSkills();
        }

        public void AddHitbox(Player player, CharacterType charType, KeyCode keyCode, Vector2 mousePos = new Vector2(), float chargeRatio = 0)
        {
            lock (_lock)
            {
                SkillHitbox skillHitbox = DataManager.SkillHitboxDict[charType][keyCode];
                if (skillHitbox.EndFrame <= 0)
                    return;

                Hitbox hitbox = new Hitbox
                {
                    Player = player,
                    PosX = player.PosInfo.PosX,
                    PosZ = player.PosInfo.PosZ,
                    ChargeRatio = chargeRatio,
                    CharType = charType,
                    KeyCode = keyCode,
                    Team = player.Info.Player.Team,
                    Data = skillHitbox,
                    MousePos = mousePos,
                };

                hitbox.Interactions = DataUtils.ConvertProtoInteractionsToKeyCodeDictionary(DataManager.SkillHitboxDict[charType][keyCode].Interactions);

                if (System.Enum.TryParse<SkillShape>(hitbox.Data.Shape, out var shape))
                {
                    if (shape == SkillShape.Point)
                    {
                        hitbox.PosX = hitbox.MousePos.X;
                        hitbox.PosZ = hitbox.MousePos.Y;
                    }
                }

                _pendingHitboxes.Add(hitbox);
            }            

            // 2타 hitbox 추가
            if(_hitboxChainDict.TryGetValue(charType, out Dictionary<KeyCode, KeyCode> chainDict))
            {
                if (chainDict.TryGetValue(keyCode, out KeyCode value))
                    AddHitbox(player, charType, value, mousePos, chargeRatio);
            }
        }
       
        public void Update()
        {
            RemoveExpired();
            UpdatePos();
            HandleCollisionMovement();
        }

        public void HandleCollisionMovement()
        {
            foreach (HashSet<Hitbox> hitboxSet in _hitboxDict.Values)
            {
                foreach (Hitbox hitbox in hitboxSet)
                {
                    if (!System.Enum.TryParse<SkillType>(hitbox.Data.Type, out var type))
                        continue;

                    if (type != SkillType.SkillProjectile)
                        continue;

                    Quaternion playerRotation = new Quaternion(
                        hitbox.Player.RotInfo.Qx,
                        hitbox.Player.RotInfo.Qy,
                        hitbox.Player.RotInfo.Qz,
                        hitbox.Player.RotInfo.Qw
                    );

                    const float moveSpeed = 10; // TODO : 예시
                    Vector3 toForward = Vector3.Transform(new Vector3(0, 0, 1), playerRotation);
                    const float TickInterval = 1.0f / 60.0f;
                    float deltaMove = moveSpeed * TickInterval; 

                    hitbox.PosX += toForward.X * deltaMove;
                    hitbox.PosZ += toForward.Z * deltaMove;

                }
            }
        }

        public void CheckAllCollisions(
            Dictionary<int, Dictionary<int, Player>> teams,
            ConcurrentDictionary<int, Monster> monsters,
            Dictionary<int, Projectile> projectiles)
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
                    if (_hitboxDict.TryGetValue(hitbox.Player.Id, out var set))
                        set.Remove(hitbox);
                }
            }
        }

        public void UpdatePos()
        {
            foreach (var set in _hitboxDict.Values)
            {
                foreach(Hitbox hitbox in set)
                {
                    if (hitbox.Player == null || hitbox.Data == null)
                        continue;
                    if (false == System.Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                        continue;
                    if (type != SkillType.SkillTrack)
                        continue; 

                    Quaternion rot = new Quaternion(
                        hitbox.Player.RotInfo.Qx,
                        hitbox.Player.RotInfo.Qy,
                        hitbox.Player.RotInfo.Qz,
                        hitbox.Player.RotInfo.Qw
                    );

                    Vector3 offset = new Vector3(hitbox.Data.RightOffset, 0, hitbox.Data.LookOffset);

                    Vector3 rotatedOffset = Vector3.Transform(offset, rot);

                    hitbox.PosX = hitbox.Player.PosInfo.PosX + rotatedOffset.X;
                    hitbox.PosZ = hitbox.Player.PosInfo.PosZ + rotatedOffset.Z;
                }
            }
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

                    if (CheckCollision(hitboxA, hitboxB))
                    {
                        _interactionManager.HandleInteraction(hitboxA, hitboxB);
                        _interactionManager.HandleInteraction(hitboxB, hitboxA);
                    }
                }
            }
        }

        void CheckPlayerHit(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            foreach(var nestedKvp in _hitboxDict)
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
                        HandleDamage<Player>(hitbox, hitPlayers, damageDict);
                }
            }
        }

        void CheckHit<T>(IDictionary<int, T> targets, Dictionary<int, Dictionary<int, float>> damageDict) where T : GameObject, new() 
        {
            foreach(var nestedKvp in _hitboxDict)
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
                    if(hitTargets.Count > 0)
                        HandleDamage<T>(hitbox, hitTargets, damageDict);
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
                    hitTargets.Add(target);
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
                        return distanceSq <= myHitbox.Data.Radius * myHitbox.Data.Radius;
                    }
                case SkillShape.Rectangle:
                    {
                        Vector2 center = myHitbox.MousePos;

                        Vector2 forward = Vector2.Normalize(new Vector2(center.X - myHitbox.Player.PosInfo.PosX, center.Y - myHitbox.Player.PosInfo.PosZ));

                        Vector2 right = new Vector2(-forward.Y, forward.X);

                        Vector2 toTarget = new Vector2(targetHitbox.PosX - center.X, targetHitbox.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = myHitbox.Data.Height * 0.5f;
                        float halfWidth = myHitbox.Data.Width * 0.5f;

                        return MathF.Abs(projForward) <= halfHeight &&
                               MathF.Abs(projRight) <= halfWidth;
                    }

                case SkillShape.Point:
                    {
                        Vector2 center = myHitbox.MousePos;
                        Vector2 playerPos = new Vector2(myHitbox.Player.PosInfo.PosX, myHitbox.Player.PosInfo.PosZ);
                        Vector2 forward = Vector2.Normalize(center - playerPos);

                        Vector2 right = new Vector2(-forward.Y, forward.X);
                        Vector2 toTarget = new Vector2(targetHitbox.PosX - center.X, targetHitbox.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = myHitbox.Data.Height * 0.5f;
                        float halfWidth = myHitbox.Data.Width * 0.5f;

                        return MathF.Abs(projForward) <= halfHeight && MathF.Abs(projRight) <= halfWidth;
                    }

                case SkillShape.Ray:
                    {
                        Vector2 origin = new Vector2(myHitbox.PosX, myHitbox.PosZ);
                        Vector2 forward = Vector2.Normalize(myHitbox.MousePos - origin);
                        Vector2 right = new Vector2(-forward.Y, forward.X);
                        Vector2 toTarget = new Vector2(targetHitbox.PosX - origin.X, targetHitbox.PosZ - origin.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        if (!System.Enum.TryParse<SkillType>(myHitbox.Data.Type, out SkillType type))
                            return false;

                        float range = myHitbox.Data.MaxRange;
                        if (type == SkillType.SkillTrack)
                            range = myHitbox.Data.MinRange + (myHitbox.Data.MaxRange - myHitbox.Data.MinRange) * myHitbox.ChargeRatio;

                        return projForward >= 0 && projForward <= range && MathF.Abs(projRight) <= myHitbox.Data.Width * 0.5f;
                    }
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
                        return distanceSq <= hitbox.Data.Radius * hitbox.Data.Radius;
                    }
                case SkillShape.Rectangle:
                    {
                        Vector2 center = hitbox.MousePos;

                        Vector2 forward = Vector2.Normalize(new Vector2(center.X - hitbox.Player.PosInfo.PosX, center.Y - hitbox.Player.PosInfo.PosZ));

                        Vector2 right = new Vector2(-forward.Y, forward.X);

                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - center.X, go.PosInfo.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = hitbox.Data.Height * 0.5f;
                        float halfWidth = hitbox.Data.Width * 0.5f;

                        return MathF.Abs(projForward) <= halfHeight &&
                               MathF.Abs(projRight) <= halfWidth;
                    }

                case SkillShape.Point:
                    {
                        Vector2 center = hitbox.MousePos;
                        Vector2 playerPos = new Vector2(hitbox.Player.PosInfo.PosX, hitbox.Player.PosInfo.PosZ);
                        Vector2 forward = Vector2.Normalize(center - playerPos);

                        Vector2 right = new Vector2(-forward.Y, forward.X);
                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - center.X, go.PosInfo.PosZ - center.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        float halfHeight = hitbox.Data.Height * 0.5f;
                        float halfWidth = hitbox.Data.Width * 0.5f;

                        return MathF.Abs(projForward) <= halfHeight && MathF.Abs(projRight) <= halfWidth;
                    }

                case SkillShape.Ray:
                    {
                        Vector2 origin = new Vector2(hitbox.PosX, hitbox.PosZ);
                        Vector2 forward = Vector2.Normalize(hitbox.MousePos - origin);
                        Vector2 right = new Vector2(-forward.Y, forward.X);
                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - origin.X, go.PosInfo.PosZ - origin.Y);

                        float projForward = Vector2.Dot(toTarget, forward);
                        float projRight = Vector2.Dot(toTarget, right);

                        if (!System.Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                            return false;

                        float range = hitbox.Data.MaxRange;
                        if (type == SkillType.SkillTrack)
                            range = hitbox.Data.MinRange + (hitbox.Data.MaxRange - hitbox.Data.MinRange) * hitbox.ChargeRatio;

                        return projForward >= 0 && projForward <= range && MathF.Abs(projRight) <= hitbox.Data.Width * 0.5f;
                    }
                case SkillShape.Sector:
                    {
                        Vector2 center = new Vector2(hitbox.PosX, hitbox.PosZ);
                        Vector2 toTarget = new Vector2(go.PosInfo.PosX - center.X, go.PosInfo.PosZ - center.Y);

                        if (toTarget.LengthSquared() > hitbox.Data.Radius * hitbox.Data.Radius)
                            return false;

                        Vector2 mouseDir = Vector2.Normalize(new Vector2(hitbox.MousePos.X - center.X, hitbox.MousePos.Y - center.Y));
                        Vector2 targetDir = Vector2.Normalize(toTarget);

                        float dot = Math.Clamp(Vector2.Dot(mouseDir, targetDir), -1f, 1f);
                        float angleDeg = MathF.Acos(dot) * (180f / MathF.PI);

                        return angleDeg <= hitbox.Data.Angle * 0.5f;
                    }
            }
            
            return false;
        }
         
        void ApplyDamage(Hitbox hitbox, GameObject target, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            float dmg = CalcDamage(hitbox.Player, target.Stat, hitbox.KeyCode);

            if (target is Player)
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:{target.Info.Player.CharType}_{target.Id}, Damage:{dmg}");
            else if (target is Monster)
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:{target.Info.Monster.MonsterType}_{target.Id}, Damage:{dmg}");
            else
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:Env_{target.Id}, Damage:{dmg}");

            if (damageDict.ContainsKey(target.Id))
            {
                if (damageDict[target.Id].ContainsKey(hitbox.Player.Id))
                {
                    damageDict[target.Id][hitbox.Player.Id] += dmg;
                }
                else
                    damageDict[target.Id][hitbox.Player.Id] = dmg;
            }
            else
            {
                damageDict[target.Id] = new Dictionary<int, float>();
                damageDict[target.Id][hitbox.Player.Id] = dmg;
            }
            hitbox.HitObjs.TryAdd(target.Id, 0);
        }

        public float CalcDamage(Player attacker, Player target, KeyCode keyCode)
        {
            // 플레이어가 플레이어 때릴 때
            StatInfo info = target.Stat.Clone();
            info.Defense = target.Defense;
            info.MaxHp = target.MaxHp;
            return CalcDamage(attacker, info, keyCode);
        }

        public float CalcDamage(Player attacker, StatInfo target, KeyCode keyCode)
        {
            // 플레이어가 몬스터 때릴 때
            // TODO 버프 디버프 정보도 가지고 와야함. 예를 들면 방깍 디버프 같은거 
            Skill skill = attacker.GetSkill(keyCode);
            
            float damage = skill.GetSkillDamage()
                + skill.SkillData.scaling.adRatio * attacker.Attack * 0.01f
                + skill.SkillData.scaling.apRatio * attacker.SkillAmplification * 0.01f
                + skill.SkillData.scaling.dstCurHpRatio * target.Hp * 0.01f
                + skill.SkillData.scaling.dstMaxHpRatio * target.MaxHp * 0.01f
                + skill.SkillData.scaling.srcCurHpRatio * attacker.Hp * 0.01f
                + skill.SkillData.scaling.srcMaxHpRatio * attacker.MaxHp * 0.01f;

            // 그냥 예시 : 추가 데미지를 입힐 시에
            if(attacker.IsSkillAmplification)
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

                foreach(var attakerKvp in kvp.Value)
                {
                    GameObject attacker = ObjectManager.Instance.Find(attakerKvp.Key);
                    if (attacker == null) 
                        continue;

                    float damage = attakerKvp.Value;
                    hitTarget.Room.Push(hitTarget.OnDamaged, attacker, damage);
                }
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                foreach (Hitbox pendingHitbox in _pendingHitboxes)
                {

                    if (!_hitboxDict.TryGetValue(pendingHitbox.Player.Id, out var set))
                    {
                        set = new HashSet<Hitbox>();
                        _hitboxDict[pendingHitbox.Player.Id] = set;
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
                    Skill skill = hitbox.Player.GetSkill(hitbox.KeyCode);
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
    }
}
