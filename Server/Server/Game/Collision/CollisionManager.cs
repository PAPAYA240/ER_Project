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
            Dictionary<int, float> damageDict = new Dictionary<int, float>();
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

        void CheckPlayerHit(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, float> damageDict)
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
                                float dmg = CalcDamage(hitbox.Player, target.Stat, hitbox.KeyCode );
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

        void CheckHit<T>(IDictionary<int, T> targets, Dictionary<int, int> damageDict) where T : GameObject, new() 
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

                        if (!Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
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

        float CalcDamage(Player attacker, StatInfo target, KeyCode key)
        {
            // temp dmg
            // 여기서 어떤 정보를 받아와야하는가?
            // 1 어태커의 스킬 정보(스킬 기본 데미지랑 스킬 데미지계수가 포함된 매커니즘?)
            // 2 어태커의 스탯 정보(기본 스탯이랑 아이템 스탯 더해진....값?)
            // 3 타겟의 스탯 정보 (기본 스탯이랑 아이템 스탯 더해진....값?)

            //스킬 타입을 가져옴
            //attacker.GetSkilltype();

            //해당 스킬의 레벨별 기본 데미지를 가져옴.
            float damage = attacker.GetSkillDamage(key);

            float defense = target.Defense;

            //스킬의 계수 적용?

            //대상의 방어력을 가져와서 방어력 관통을 적용 시킨다.
            //이렇게 
            //방어력 적용?

            //프로퍼티를 어떻게 만들까.
            // 플레이어 한테 프로퍼티

            return 120;
        } 

        void SendChangeHpPkts(Dictionary<int, Dictionary<int, Player>> teams, Dictionary<int, float> damageDict)
        {
            foreach (var kvp in damageDict)
            {
                GameObject hitTarget = ObjectManager.Instance.Find(kvp.Key);
                if (hitTarget == null)
                    continue;

                hitTarget.Info.StatInfo.Hp -= damageDict[kvp.Key];
                hitTarget.Info.StatInfo.Hp = Math.Max(0, hitTarget.Info.StatInfo.Hp);

                IMessage packet;
                if(hitTarget.Info.StatInfo.Hp > 0)
                {
                    packet = new S_ChangeHp()
                    {
                        ObjectId = kvp.Key,
                        Hp = hitTarget.Info.StatInfo.Hp,
                    };
                }
                else
                {
                    hitTarget.OnDead(hitTarget);
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
