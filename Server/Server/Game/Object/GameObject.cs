using System;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
        public int Id
        {
            get { return Info.ObjectId; }
            set { Info.ObjectId = value; }
        }

        public GameRoom Room { get; set; }

        ObjectInfo _objectInfo = new ObjectInfo()
        {
            StatInfo = new StatInfo(),
            PosInfo = new PositionInfo(),
            RotInfo = new RotationInfo() { Qw = 1f }
        };

        public ObjectInfo Info
        {
            get { return _objectInfo; }
            set { _objectInfo = value; PosInfo = value.PosInfo; RotInfo = value.RotInfo; Stat = value.StatInfo; }
        }

        public PositionInfo PosInfo
        {
            get { return Info.PosInfo; }
            set
            {
                if (Info.PosInfo.Equals(value))
                    return;

                PosInfo = value;
                State = value.State;
            }
        }

        public RotationInfo RotInfo
        {
            get { return Info.RotInfo; }
            set
            {
                if (value == null)
                    return;

                if (Info.RotInfo.Equals(value))
                    return;

                Info.RotInfo.Qx = value.Qx;
                Info.RotInfo.Qy = value.Qy;
                Info.RotInfo.Qz = value.Qz;
                Info.RotInfo.Qw = value.Qw;
            }
        }

        public StatInfo Stat 
        {
            get
            {
                if (Info.StatInfo == null)
                    Info.StatInfo = new StatInfo();
                return Info.StatInfo;
            }
            set
            {
                if (Info.StatInfo == null)
                    Info.StatInfo = new StatInfo();

                if (!Info.StatInfo.Equals(value))
                    Info.StatInfo.MergeFrom(value);
            }
        }
        public Monster Target { get; internal set; }

        public float Speed
        {
            get { return Stat.MoveSpeed; }
            set { Stat.MoveSpeed = value; }
        }

        public float Hp
        {
            get { return Stat.Hp; }
            set { Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp); }
        }

        public float Stamina
        {
            get { return Stat.Stamina; }
            set { Stat.Stamina = Math.Clamp(value, 0, Stat.MaxStamina); }
        }

        public CreatureState State
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }

        public virtual void Update()
        {
            UpdateController();
        }

        protected virtual void UpdateController()
        {
            //switch (State)
            //{
            //    case CreatureState.Idle:
            //        break;
            //    case CreatureState.Moving:
            //        break;
            //    case CreatureState.Attack:
            //        break;
            //    case CreatureState.Skill:
            //        break;
            //    case CreatureState.Dead:
            //        break;
            //    case CreatureState.Rest:
            //        break;
            //}
        }

        public virtual void OnDamaged(GameObject attacker, float damage)
        {
            if (Room == null || State == CreatureState.Dead)
                return;

            Stat.Hp = Math.Max((int)(Stat.Hp - damage), 0);

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Stat.Hp;
            Room.Broadcast(changePacket);

            if(Stat.Hp <= 0)
            {
                OnDead(attacker);
            }
        }

        public virtual void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;

            S_Die diePacket = new S_Die();
            diePacket.ObjectId = Id;
            diePacket.AttackerId = attacker.Id;
            Room.Broadcast(diePacket);

            GameRoom room = Room;
            room.LeaveGame(Id);

            Stat.Hp = Stat.MaxHp;
            Stat.Stamina = Stat.MaxStamina;
            State = CreatureState.Idle;
            PosInfo.PosX = 0;
            PosInfo.PosY = 0;
            PosInfo.PosZ = 0;
            RotInfo.Qx = 0;
            RotInfo.Qy = 0;
            RotInfo.Qz = 0;
            RotInfo.Qw = 1;

            room.EnterGame(this);
        }
    }
}
