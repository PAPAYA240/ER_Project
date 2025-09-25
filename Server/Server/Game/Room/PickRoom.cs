using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class PickRoom : Room
    {
        const int _maxPlayer = 8;

        PickPlayer[] _pickPlayers = new PickPlayer[_maxPlayer];

        public void Init()
        {
            
        }

        public override void Update()
        {
            Flush();
            CheckStartGame();
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
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                {
                    pickIdx = i;
                    break;
                }   
            }
            
            string userName = "UserName_" + pickIdx;

            _pickPlayers[pickIdx] = pp;

            //본인한테 정보 전송
            {
                S_EnterPick enterPickPacket = new S_EnterPick();
                enterPickPacket.PickIdx = pickIdx;
                enterPickPacket.UserName = userName;
                pp.Session.Send(enterPickPacket);
                pp.Session.PickIdx = pickIdx;
                pp.Session.CurRoom = RoomId;
                pp.UserName = userName;

                S_SpawnPick spawnPacket = new S_SpawnPick();

                foreach (PickPlayer player in _pickPlayers)
                {
                    if (player == null)
                        continue;

                    if (player.Session.PickIdx != pickIdx)
                    {
                        PickScenePlayerInfo pspi = new PickScenePlayerInfo();
                        pspi.CharType = player.Session.MyCharacter;
                        pspi.PickIdx = player.Session.PickIdx;
                        pspi.UserName = player.UserName;
                        pspi.WeaponType = player.Session.WeaponType;
                        pspi.TraitType = player.Session.TraitType;

                        spawnPacket.Players.Add(pspi);
                    }
                }

                pp.Session.Send(spawnPacket);
            }

            //타인한테 정보 전송
            {
                PickScenePlayerInfo pspi = new PickScenePlayerInfo();
                pspi.CharType = CharacterType.CharacterNone;
                pspi.PickIdx = pickIdx;
                pspi.UserName = userName;

                S_SpawnPick spawnPacket = new S_SpawnPick();
                spawnPacket.Players.Add(pspi);

                foreach (PickPlayer player in _pickPlayers)
                {
                    if (player == null)
                        continue;

                    if (player.Session.PickIdx != pickIdx)
                        player.Session.Send(spawnPacket);
                }
            }
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
            for(int i = 0; i < _maxPlayer; ++i)
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

        public void CheckStartGame()
        {
            bool everyPlayerStartGame = true;
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;
                if(_pickPlayers[i].Session.MyPlayer == null)
                {
                    everyPlayerStartGame = false;
                    break;
                }
            }

            if (everyPlayerStartGame)
            {
                for (int i = 0; i < _maxPlayer; ++i)
                {
                    if (_pickPlayers[i] == null)
                        continue;
                    LeavePick(i);
                }
            }
        }
    }
}
