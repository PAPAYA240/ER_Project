using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game.Object.Monster;
using Server.Game.Object.Monster.AStar;

namespace Server.Game
{
    public class PickRoom : Room
    {
        PickPlayer[] _pickPlayers = new PickPlayer[4];

        public void Init()
        {
            
        }

        public override void Update()
        {
            Flush();

            CheckLastPing();
        }

        public bool isRoomFull()
        {
            bool isFull = true;
            foreach(PickPlayer p in _pickPlayers)
            {
                if (p == null)
                    return false;
            }
            return isFull;
        }

        public void EnterPick(PickPlayer pp)
        {
            int pickIdx = 0;
            for(int i = 0; i < 4; ++i)
            {
                if (_pickPlayers[i] == null)
                {
                    pickIdx = i;
                    break;
                }   
            }

            _pickPlayers[pickIdx] = pp;
            S_EnterPick enterPickPacket = new S_EnterPick();
            enterPickPacket.PickIdx = pickIdx;
            pp.Session.Send(enterPickPacket);
            pp.Session.PickIdx = pickIdx;
            pp.Session.CurRoom = RoomId;
        }

        public void LeavePick(int pickIdx)
        {
            S_LeavePick leavePickPacket = new S_LeavePick();
            leavePickPacket.PickIdx = pickIdx;
            Broadcast(leavePickPacket);

            _pickPlayers[pickIdx] = null;
        }

        public void Broadcast(IMessage packet, int pickIdx = -1)
        {
            for(int i = 0; i < 4; ++i)
            {
                if (i == pickIdx) 
                    continue;
                if (_pickPlayers[i] == null)
                    continue;

                _pickPlayers[i].Session.Send(packet);
            }
        }

        public override void CheckLastPing()
        {
            foreach (var player in _pickPlayers)
            {
                if (player == null) continue;

                if (player.Session.CheckTimeout())
                    player.Session.Disconnect();
            }
        }
    }
}
