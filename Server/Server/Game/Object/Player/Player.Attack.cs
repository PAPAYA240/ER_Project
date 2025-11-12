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
        public int AttackRange { get; set; } = 6;

        public bool IsDead => this.State == CreatureState.Dead;

        // �̵� ����
        public void StopMove()
        {
            // �ʿ�� �ӵ� 0, ������ Ŭ����, �̵� �ִ� ���� ��
        }

        // ���� ����
        public void CancelAttack()
        {
            if (this.CurrentState is Player_AttackState)
                this.ChangeState(new Player_IdleState());
        }

        // �ֺ� ���� ����� ��
        public GameObject FindNearestEnemy(int range)
        {
            return this.Room?.FindNearestEnemy(this, range);
        }

        // Ÿ���� �ٶ󺸱�(Ÿ�� ID��)
        public void FaceToTarget(int targetId)
        {
            var t = FindTarget(targetId);
            if (t == null)
                return;
            FaceTo(t.Position);
        }

        // ������ ���� ��ǥ�� �ٶ󺸱�(Yaw�� ����)
        public void FaceTo(Vector3 worldPos)
        {
            Vector3 dir = worldPos - this.Position;
            if (dir.X == 0 && dir.Y == 0 && dir.Z == 0)
                return;

            // ���� ��ǥ�迡 �°� Yaw ��� (X-Z ��� ���� ����)
            float yawRad = MathF.Atan2(dir.X, dir.Z);
            float yawDeg = yawRad * (180.0f / MathF.PI);

            // PositionInfo�� RotY�� �ִٰ� ����
            //this.RotInfo.Qy = yawDeg;

            // (����) ȸ�� ������ Ŭ�� ��ũ�ϰ� ������ Move/State ��Ŷ�� �����ؼ� ��ε�ĳ��Ʈ
            // BroadcastPositionRotation();
        }      
    }
}
