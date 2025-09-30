using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        public int EndTick { get; set; } // Skill End Time

        public bool IsUsed { get; set; } = false;

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

        List<Hitbox> _pendingHitboxes = new List<Hitbox>();

        public int CurTick { get; set; }

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
                    MousePos = mousePos
                };

                _pendingHitboxes.Add(hitbox);
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

                foreach (var hitbox in hitboxes)
                {
                    if (CurTick < hitbox.StartTick || CurTick > hitbox.EndTick)
                        continue;

                    List<Player> hitPlayers = new List<Player>();

                    foreach (var teamKvp in teams)
                    {
                        int teamId = teamKvp.Key;
                        if (teamId == myTeam) continue;

                        HandleCollision<Player>(hitbox, teamKvp.Value, hitPlayers, damageDict);
                    }

                    if(hitPlayers.Count > 0)
                        HandleDamage<Player>(hitbox, hitPlayers, damageDict);
                }
            }
        }

        void CheckHit<T>(IDictionary<int, T> targets, Dictionary<int, float> damageDict) where T : GameObject, new() 
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

        void HandleCollision<T>(Hitbox hitbox, IDictionary<int, T> targets, List<T> hitTargets, Dictionary<int, float> damageDict) where T : GameObject, new()
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

        void HandleDamage<T>(Hitbox hitbox, List<T> hitTargets, Dictionary<int, float> damageDict) where T : GameObject, new()
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
         
        void ApplyDamage(Hitbox hitbox, GameObject target, Dictionary<int, float> damageDict)
        {
            float dmg = CalcDamage(hitbox.Player, target.Stat, hitbox.KeyCode);
            if(target is Player)
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:{target.Info.Player.CharType}_{target.Id}, Damage:{dmg}");
            else if(target is Monster)
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:{target.Info.Monster.MonsterType}_{target.Id}, Damage:{dmg}");
            else
                Console.WriteLine($"Attacker:{hitbox.CharType}_{hitbox.Player.Id}, Target:Env_{target.Id}, Damage:{dmg}");

            if (damageDict.ContainsKey(target.Id))
                damageDict[target.Id] += dmg;
            else
                damageDict[target.Id] = dmg;
            hitbox.HitObjs.TryAdd(target.Id, 0);
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
    }
}
