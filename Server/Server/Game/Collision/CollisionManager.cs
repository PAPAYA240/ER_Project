using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    class Hitbox
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

        // Key: ObjectId, Value: Nothing
        public ConcurrentDictionary<int, byte> HitObjs = new ConcurrentDictionary<int, byte>();
    }

    class HitboxChain
    {
        public KeyCode KeyCode { get; set; }
        public int MilliSecs { get; set; }
    }

    class CollisionManager
    {
        object _lock = new object();

        // Key: ObjectId
        Dictionary<int, HashSet<Hitbox>> _hitboxDict = new Dictionary<int, HashSet<Hitbox>>();

        Dictionary<CharacterType, Dictionary<KeyCode, HitboxChain>> _hitboxChainDict = new Dictionary<CharacterType, Dictionary<KeyCode, HitboxChain>>();

        public void Init()
        {
            // 히트박스 특정 시간 후 자동 생성
            AddHitboxChain(CharacterType.Abigail, KeyCode.Q, new HitboxChain { KeyCode = KeyCode.F1, MilliSecs = 333 });
        }

        public void AddHitbox(Player player, CharacterType charType, KeyCode keyCode, Vector2 mousePos = new Vector2())
        {
            lock (_lock)
            {
                Vector3 forward = player.RotInfo.Forward();

                Hitbox hitbox = new Hitbox
                {
                    Player = player,
                    PosX = player.PosInfo.PosX,
                    PosZ = player.PosInfo.PosZ,
                    CharType = charType,
                    KeyCode = keyCode,
                    Team = player.Info.Player.Team,
                    Data = DataManager.SkillHitboxDict[charType][keyCode],
                    StartTick = Environment.TickCount,
                    MousePos = mousePos
                };

                if (!_hitboxDict.TryGetValue(player.Id, out var set))
                {
                    set = new HashSet<Hitbox>();
                    _hitboxDict[player.Id] = set;
                }

                set.Add(hitbox);

                if (_hitboxChainDict.TryGetValue(charType, out Dictionary<KeyCode, HitboxChain> dict))
                {
                    if(dict.TryGetValue(keyCode, out HitboxChain hitboxChain))
                    {
                        // 딜레이 이후 자동으로 2타 히트박스 추가
                        _ = AddHitboxAfterDelay(player, charType, hitboxChain.KeyCode, hitboxChain.MilliSecs);
                    }
                }
            }            
        }

        public void Update()
        {
            RemoveExpired();
            UpdatePos();            
        }

        public void CheckAllCollisions(
            Dictionary<int, Dictionary<int, Player>> teams,
            ConcurrentDictionary<int, Monster> monsters,
            Dictionary<int, Projectile> projectiles)
        {
            Dictionary<int, Dictionary<int, float>> damageDict = new Dictionary<int, Dictionary<int, float>>();
            CheckPlayerHit(teams, damageDict);
            CheckHit(monsters, damageDict);
            SendChangeHpPkts(teams, damageDict);
        }

        public void RemoveExpired()
        {
            List<Hitbox> removeQueue = new List<Hitbox>();
            int now = Environment.TickCount; 

            foreach (HashSet<Hitbox> hitboxSet in _hitboxDict.Values)
            {
                foreach (Hitbox hitbox in hitboxSet)
                {
                    if (now - hitbox.StartTick >= (int)(hitbox.Data.Duration * 1000))
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

        void CheckPlayerHit(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, Dictionary<int, float>> damageDict)
        {
            foreach(var nestedKvp in _hitboxDict)
            {
                int ownerId = nestedKvp.Key;
                HashSet<Hitbox> hitboxes = nestedKvp.Value;
                if (hitboxes.Count == 0)
                    continue;

                int myTeam = ObjectManager.Instance.GetTeam(ownerId);
                foreach (var teamKvp in teams)
                {
                    int teamId = teamKvp.Key;
                    if (teamId == myTeam)
                        continue;

                    Dictionary<int, Player> enemyPlayers = teamKvp.Value;

                    foreach (var playerKvp in enemyPlayers)
                    {
                        Player target = playerKvp.Value;

                        // Collision Check
                        foreach (var hitbox in hitboxes)
                        {
                            if (hitbox.HitObjs.ContainsKey(playerKvp.Key))
                                continue;

                            if (CheckCollision(hitbox, target))
                            {
                                float dmg = CalcDamage(hitbox.Player, target, hitbox.KeyCode);
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
                        }
                    }
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

                foreach (var targetKvp in targets)
                {
                    T target = targetKvp.Value;
                    // Collision Check
                    foreach (var hitbox in hitboxes)
                    {
                        if (hitbox.HitObjs.ContainsKey(targetKvp.Key))
                            continue;

                        if (CheckCollision(hitbox, target))
                        {
                            float dmg = CalcDamage(hitbox.Player, target.Stat, hitbox.KeyCode);
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
                    }
                }
            }
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

        public void AddHitboxChain(CharacterType character, KeyCode key, HitboxChain chain)
        {
            if (!_hitboxChainDict.ContainsKey(character))
                _hitboxChainDict[character] = new Dictionary<KeyCode, HitboxChain>();

            _hitboxChainDict[character][key] = chain;
        }

        async Task AddHitboxAfterDelay(Player player, CharacterType charType, KeyCode keyCode, int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);

            AddHitbox(player, charType, keyCode);
        }
    }
}
