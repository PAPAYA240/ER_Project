using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using static Server.Data.DataUtils;

namespace Server.Game
{
    class Hitbox
    {
        public Player Player { get; set; }
        public float PosX { get; set; } = 0;
        public float PosZ {  get; set; } = 0;
        public float DirX { get; set; } = 0;
        public float DirZ { get; set; } = 0;
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

        public void AddHitbox(Player player, CharacterType charType, KeyCode keyCode)
        {
            lock (_lock)
            {
                Vector3 forward = player.RotInfo.Forward();

                Hitbox hitbox = new Hitbox
                {
                    Player = player,
                    PosX = player.PosInfo.PosX,
                    PosZ = player.PosInfo.PosZ,
                    DirX = forward.X,
                    DirZ = forward.Z,
                    CharType = charType,
                    KeyCode = keyCode,
                    Team = player.Info.Player.Team,
                    Data = DataManager.SkillHitboxDict[charType][keyCode],
                    StartTick = Environment.TickCount
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
            Dictionary<int, int> damageDict = new Dictionary<int, int>();
            CheckPlayerHit(teams, damageDict);

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
                    if (false == Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
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

        void CheckPlayerHit(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, int> damageDict)
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
                                int dmg = CalcDamage(hitbox.Player.Stat, target.Stat, DataManager.SkillDict[hitbox.CharType][hitbox.KeyCode]);
                                if (damageDict.ContainsKey(target.Id))
                                    damageDict[target.Id] += dmg;
                                else
                                    damageDict[target.Id] = dmg;
                                hitbox.HitObjs.TryAdd(target.Id, 0);
                            }
                        }
                    }
                }
            }
        }

        bool CheckCollision(Hitbox hitbox, GameObject go)
        {
            if (!Enum.TryParse<SkillShape>(hitbox.Data.Shape, out var shape))
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
                        Vector3 toTarget = new Vector3(go.PosInfo.PosX - hitbox.PosX, 0, go.PosInfo.PosZ - hitbox.PosZ);
                        Vector3 forward = new Vector3(hitbox.DirX, 0, hitbox.DirZ);
                        Vector3 right = new Vector3(-forward.Z, 0, forward.X);

                        float projForward = Vector3.Dot(toTarget, forward);
                        float projRight = Vector3.Dot(toTarget, right);

                        return MathF.Abs(projForward) <= hitbox.Data.Height * 0.5f &&
                               MathF.Abs(projRight) <= hitbox.Data.Width * 0.5f;
                    }
                case SkillShape.Ray:
                    {
                        Vector3 toTarget = new Vector3(go.PosInfo.PosX - hitbox.PosX, 0, go.PosInfo.PosZ - hitbox.PosZ);
                        Vector3 forward = new Vector3(hitbox.DirX, 0, hitbox.DirZ);
                        Vector3 right = new Vector3(-forward.Z, 0, forward.X);
                        float projForward = Vector3.Dot(toTarget, forward);
                        float projRight = Vector3.Dot(toTarget, right);

                        if (false == Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
                            return false;
                        float range = hitbox.Data.MaxRange;
                        if(type == SkillType.SkillTrack)
                            range = hitbox.Data.MinRange + (hitbox.Data.MaxRange - hitbox.Data.MinRange) * hitbox.ChargeRatio;

                        return projForward >= 0 && projForward <= range &&
                               MathF.Abs(projRight) <= hitbox.Data.Width * 0.5f;
                    }
                case SkillShape.Sector:
                    {
                        Vector3 toTarget = new Vector3( go.PosInfo.PosX - hitbox.PosX,
                        0, go.PosInfo.PosZ - hitbox.PosZ);

                        float distanceSq = toTarget.X * toTarget.X + toTarget.Z * toTarget.Z;
                        if (distanceSq > hitbox.Data.Radius * hitbox.Data.Radius)
                            return false;

                        Vector3 hitboxDir = Vector3.Normalize(new Vector3(hitbox.DirX, 0, hitbox.DirZ));
                        Vector3 targetDir = Vector3.Normalize(toTarget);

                        float dot = hitboxDir.X * targetDir.X + hitboxDir.Z * targetDir.Z;
                        float angleDeg = MathF.Acos(dot) * (180f / MathF.PI);

                        return angleDeg <= hitbox.Data.Angle * 0.5f;
                    }
            }
            
            return false;
        }

        int CalcDamage(StatInfo attackter, StatInfo target, SkillData skill)
        {
            // temp dmg
            return 30;
        } 

        void SendChangeHpPkts(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, int> damageDict)
        {
            foreach (var kvp in damageDict)
            {
                GameObject obj = ObjectManager.Instance.Find(kvp.Key);
                Player player = obj as Player;

                if (player == null)
                    continue;

                player.Info.StatInfo.Hp -= damageDict[kvp.Key];
                player.Info.StatInfo.Hp = Math.Max(0, player.Info.StatInfo.Hp);

                IMessage packet;
                if(player.Info.StatInfo.Hp > 0)
                {
                    packet = new S_ChangeHp()
                    {
                        ObjectId = kvp.Key,
                        Hp = player.Info.StatInfo.Hp,
                    };
                }
                else
                {
                    packet = new S_Die()
                    {
                        ObjectId = kvp.Key,
                        //AttackerId = kvp.Key,
                    };
                }

                foreach (var nestedKvp in teams)
                {
                    foreach(var keyValuePair in nestedKvp.Value)
                    {
                        Player p = keyValuePair.Value;
                        if (p == null) 
                            continue;

                        // 모든 플레이어들한테 체력 변경 알림
                        p.Session.Send(packet);
                    }
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
