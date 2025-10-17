using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

namespace Server.Game
{
    // TODO : 더 좋은 방안 모색하기
    public class TheodoreSkillHandler : SkillHandler
    {
        public override bool CanUse(Player player, S_Skill skillPacket)
        {
            CollisionManager coll = player.Room.CollManager;
            if (coll == null) 
                return false;
            HandleEffect(player, skillPacket, coll);
            return true;
        }

        public void HandleEffect(Player player, S_Skill skillPacke, CollisionManager collisionManager)
        {
            switch ((KeyCode)skillPacke.SkillInfo.AmplifiKeyCode)
            {
                case KeyCode.Q:
                    AmplifyWithQ(player, skillPacke, collisionManager);
                    break;

                case KeyCode.E:
                    AmplifyWithE(player, skillPacke, collisionManager);
                    break;

                default:
                    break;
            }
        }

         // 기존에 존재하던 W스킬 충돌체가 Q 스킬을 사용할 수 있도록 추가해야 함
        private void AmplifyWithQ(Player player, S_Skill skillPacket, CollisionManager _collisionManager)
        {
            Hitbox hitbox = _collisionManager.FindCollision(player.Id, (KeyCode)skillPacket.SkillInfo.KeyCode);
            if (hitbox == null)
                return;

            Hitbox targetHitbox = _collisionManager.FindCollision(player.Id, KeyCode.W);
            if (targetHitbox == null)
                return;

            Hitbox createHitbox = _collisionManager.AddHitbox(player, player.Info.Player.CharType, (KeyCode)skillPacket.SkillInfo.AmplifiKeyCode,
           new Vector2(targetHitbox.PosX, targetHitbox.PosZ), skillPacket.ChargeRatio, false);

            createHitbox.trackingHitbox = targetHitbox;
            createHitbox.MousePos = targetHitbox.MousePos;
            createHitbox.PosX = targetHitbox.PosX;
            createHitbox.PosZ = targetHitbox.PosZ;
            createHitbox.Rot = targetHitbox.Rot;
        }

        // 기존에 존재하던 E 스킬의 범위를 넒혀야 함
        private void AmplifyWithE(Player player, S_Skill skillPacket, CollisionManager _collisionManager)
        {
            Hitbox hitbox = _collisionManager.FindCollision(player.Id, (KeyCode)skillPacket.SkillInfo.KeyCode);
            if (hitbox == null)
                return;

            Hitbox targetHitbox = _collisionManager.FindCollision(player.Id, KeyCode.E);
            if (targetHitbox == null)
                return;

            targetHitbox.OffsetRadius += 1.0f;
        }
    }
}
    
