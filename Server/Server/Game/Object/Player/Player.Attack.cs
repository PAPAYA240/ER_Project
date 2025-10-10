using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class Player : Creature
    {
        public int AttackRange { get; set; } = 3;

        public bool IsDead => this.State == CreatureState.Dead;

        // 이동 중지
        public void StopMove()
        {
            // 필요시 속도 0, 목적지 클리어, 이동 애니 중지 등
        }

        // 공격 중지
        public void CancelAttack()
        {
            if (this.CurrentState is Player_AttackState)
                this.ChangeState(new Player_IdleState());
        }

        // 주변 가장 가까운 적
        public GameObject FindNearestEnemy(int range)
        {
            return this.Room?.FindNearestEnemy(this, range);
        }

        // 타겟을 바라보기(타겟 ID로)
        public void FaceToTarget(int targetId)
        {
            var t = FindTarget(targetId);
            if (t == null)
                return;
            FaceTo(t.Position);
        }

        // 임의의 월드 좌표를 바라보기(Yaw만 보정)
        public void FaceTo(Vector3 worldPos)
        {
            Vector3 dir = worldPos - this.Position;
            if (dir.X == 0 && dir.Y == 0 && dir.Z == 0)
                return;

            // 서버 좌표계에 맞게 Yaw 계산 (X-Z 평면 기준 예시)
            float yawRad = MathF.Atan2(dir.X, dir.Z);
            float yawDeg = yawRad * (180.0f / MathF.PI);

            // PositionInfo에 RotY가 있다고 가정
            //this.RotInfo.Qy = yawDeg;

            // (선택) 회전 변경을 클라에 싱크하고 싶으면 Move/State 패킷에 포함해서 브로드캐스트
            // BroadcastPositionRotation();
        }      
    }
}
