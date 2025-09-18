using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.Monster;
using static Server.Data.DataUtils;

namespace Server.Game
{
    class Hitbox
    {
        public Player Player { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ {  get; set; }
        public SkillHitbox Data { get; set; }

        public int StartTick { get; set; } // 스킬 발동 시각 (TickCount 기준)
    }

    class CollisionManager
    {
        int _teamCnt = 2;

        // Key: ObjectId
        Dictionary<int, HashSet<Hitbox>> _hitboxDict = new Dictionary<int, HashSet<Hitbox>>();

        public void AddHitbox(Player player, CharacterType charType, KeyCode keyCode)
        {
            Hitbox hitbox = new Hitbox
            {
                Player = player,
                PosX = player.PosInfo.PosX,
                PosY = player.PosInfo.PosY,
                PosZ = player.PosInfo.PosZ,
                Data = DataManager.SkillHitboxDict[charType][keyCode],
                StartTick = Environment.TickCount
            };

            if (!_hitboxDict.TryGetValue(player.Id, out var set))
            {
                set = new HashSet<Hitbox>();
                _hitboxDict[player.Id] = set;
            }

            set.Add(hitbox);
        }

        public void Update()
        {
            RemoveExpired();
            UpdatePos();            
        }

        public void CheckCollision(
            Dictionary<int, Player> players,
            ConcurrentDictionary<int, Monster> monsters,
            Dictionary<int, Projectile> projectiles)
        {
            
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

            // remove
        }

        public void UpdatePos()
        {
            //foreach(Hitbox hitbox in _hitboxes)
            //{
            //    if (hitbox.Player == null || hitbox.Data == null)
            //        continue;
            //    if (false == Enum.TryParse<SkillType>(hitbox.Data.Type, out SkillType type))
            //        continue;
            //    if (type != SkillType.SkillTrack) 
            //        continue;

            //    Quaternion rot = new Quaternion(
            //        hitbox.Player.RotInfo.Qx,
            //        hitbox.Player.RotInfo.Qy,
            //        hitbox.Player.RotInfo.Qz,
            //        hitbox.Player.RotInfo.Qw
            //    );

            //    Vector3 offset = new Vector3(hitbox.Data.RightOffset, 0, hitbox.Data.LookOffset);

            //    Vector3 rotatedOffset = Vector3.Transform(offset, rot);

            //    hitbox.PosX = hitbox.Player.PosInfo.PosX + rotatedOffset.X;
            //    hitbox.PosY = hitbox.Player.PosInfo.PosY + rotatedOffset.Y;
            //    hitbox.PosZ = hitbox.Player.PosInfo.PosZ + rotatedOffset.Z;
            //}
        }
    }
}
