using Google.Protobuf.Protocol;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class TheodoreSkillHandler : SkillHandler
    {
        // 확장 스킬 처리
        public override bool CanUse(Player player, S_Interact skillPacket)
        {
            CollisionManager coll = player.Room.CollManager;
            if (coll == null) 
                return false;
            HandleEffect(player, skillPacket, coll);
            return true;
        }

        public void HandleEffect(Player player, S_Interact skillPacket, CollisionManager collisionManager)
        {
            switch ((KeyCode)skillPacket.TargetKeyCode)
            {
                case KeyCode.Q:
                    AmplifyWithQ(player, skillPacket, collisionManager);
                    break;

                case KeyCode.E:
                    AmplifyWithE(player, skillPacket, collisionManager);
                    break;

                default:
                    break;
            }
        }

         // 기존에 존재하던 W스킬 충돌체가 Q 스킬을 사용할 수 있도록 추가해야 함
        private void AmplifyWithQ(Player player, S_Interact skillPacket, CollisionManager _collisionManager)
        {
            Hitbox targetHitbox = _collisionManager.FindCollision(player.Id, KeyCode.W);
            if (targetHitbox == null)
                return;
            Hitbox hitbox = _collisionManager.FindCollision(player.Id, (KeyCode)skillPacket.KeyCode);
            if (hitbox == null)
                return;

            // 회전 방향 계산
            Vector2 fixedtoTarget = new Vector2(targetHitbox.FixedPosition.X, targetHitbox.FixedPosition.Z);
            Vector2 direction = targetHitbox.MousePos - fixedtoTarget;
            Vector2 forward = Vector2.Normalize(direction);

            float angleRad = (float)Math.Atan2(forward.X, forward.Y);

            Quaternion rotation = Quaternion.CreateFromAxisAngle(
                new Vector3(0, 1, 0),
                angleRad
            );

            // Q 확장 장판 Effect
            Player_SkillState skillstate = player.CurrentState as Player_SkillState;
            player.SendSkillEffect(
                skillstate.Ctx.MousePos,
                keyCode: skillstate.Ctx.Key,
                sendLookatMousePacket: true,
                targetPos: new Vector3(targetHitbox.MousePos.X, 1.0f, targetHitbox.MousePos.Y),
                targetRot: rotation,
                type: "Select",
                name: "FX_Shield_linoleum");

            // Hitbox
            Hitbox createHitbox = _collisionManager.AddHitbox(
                player, 
                player.Info.Player.CharType, 
                (KeyCode)skillPacket.TargetKeyCode,
                new Vector2(targetHitbox.PosX, targetHitbox.PosZ));

            createHitbox.trackingHitbox = targetHitbox;
            createHitbox.MousePos = targetHitbox.MousePos;
            createHitbox.FixedPosition = createHitbox.trackingHitbox.FixedPosition;
        }

        // 기존에 존재하던 E 스킬의 범위를 넒혀야 함
        private void AmplifyWithE(Player player, S_Interact skillPacket, CollisionManager _collisionManager)
        {
            Hitbox hitbox = _collisionManager.FindCollision(player.Id, (KeyCode)skillPacket.KeyCode);
            if (hitbox == null)
                return;

            Hitbox targetHitbox = _collisionManager.FindCollision(player.Id, (KeyCode)skillPacket.TargetKeyCode);
            if (targetHitbox == null)
                return;

            targetHitbox.OffsetRadius += 1.2f;
        }
    }
}
    
