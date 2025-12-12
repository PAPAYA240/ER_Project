using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using J2N.Text;

namespace Server.Game
{
    public class LobbyRoom : Room
    {
        const int _maxPlayer = 10;

        LobbyPlayer[] _lobbyPlayers = new LobbyPlayer[_maxPlayer];

        private bool _isPickRoomCreating = false;

        int _userNameCnt = 0;

        public override void Update()
        {
            TimeUtil.Instance.Update(Environment.TickCount64);

            Flush();
            CheckLastPing();
        }

        public void EnterLobby(LobbyPlayer lobbyPlayer, int slotIdx)
        {
            if(_lobbyPlayers[slotIdx] != null)
            {
                Console.WriteLine($"Slot{slotIdx} already occupied.");
                return;
            }

            if (lobbyPlayer.UserName == null || lobbyPlayer.UserName.Length == 0)
                lobbyPlayer.UserName = "UserName" + (_userNameCnt++).ToString();

            lobbyPlayer.Session.CurRoom = RoomId;
            lobbyPlayer.Session.UserName = lobbyPlayer.UserName;
            _lobbyPlayers[slotIdx] = lobbyPlayer;

            S_Nickname nicknamePkt = new S_Nickname();
            nicknamePkt.Nickname = lobbyPlayer.UserName;
            lobbyPlayer.Session.Send(nicknamePkt);

            S_EnterSlot enterSlotPkt = new S_EnterSlot();
            enterSlotPkt.SlotIdx = slotIdx;
            enterSlotPkt.Nickname = lobbyPlayer.UserName;
            BroadcastSlot(enterSlotPkt);

            S_SpawnSlot spawnSlotPkt = new S_SpawnSlot();
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null || i == slotIdx)
                    continue;
                spawnSlotPkt.SlotIdxs.Add(i);
                spawnSlotPkt.Nicknames.Add(_lobbyPlayers[i].UserName);
            }
            lobbyPlayer.Session.Send(spawnSlotPkt);

            BroadcastPlayerCntPkt();
        }

        public void OnSlotClick(int sessionId, int newIdx)
        {
            if (_lobbyPlayers[newIdx] != null)
                return;

            int prevIdx = -1;
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null)
                    continue;

                if(_lobbyPlayers[i].Session.SessionId == sessionId)
                {
                    prevIdx = i;
                    break;
                }
            }

            if (prevIdx == -1)
                return;

            SwapSlot(prevIdx, newIdx);

            S_EnterSlot newEnterSlotPkt = new S_EnterSlot();
            newEnterSlotPkt.SlotIdx = newIdx;
            newEnterSlotPkt.Nickname = _lobbyPlayers[newIdx].UserName;
            BroadcastSlot(newEnterSlotPkt);

            S_EnterSlot prevEnterSlotPkt = new S_EnterSlot();
            prevEnterSlotPkt.SlotIdx = prevIdx;
            prevEnterSlotPkt.SlotType = Slot.Empty;
            Broadcast(prevEnterSlotPkt);
        }

        public void SwapSlot(int prevIdx, int newIdx)
        {
            if (_lobbyPlayers[newIdx] != null || _lobbyPlayers[prevIdx] == null)
                return;

            LobbyPlayer temp = _lobbyPlayers[prevIdx];
            _lobbyPlayers[prevIdx] = null;
            _lobbyPlayers[newIdx] = temp;

            BroadcastPlayerCntPkt();
        }

        int ExitSlot(string nickName)
        {
            for (int i = 0; i < _maxPlayer; i++)
            {
                if (_lobbyPlayers[i] == null)
                    continue;

                if (_lobbyPlayers[i].UserName == nickName)
                {
                    _lobbyPlayers[i] = null;
                    return i;
                }                    
            }

            return -1;
        }

        public int GetEmptySlotIdx()
        {
            for (int i = 0; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null)
                    return i;
            }

            return -1;
        }

        public void LeaveLobby(string nickName)
        {
            int slotIdx = ExitSlot(nickName);
            if (slotIdx == -1)
                return;

            S_LeaveLobby leaveLobbyPkt = new S_LeaveLobby();
            leaveLobbyPkt.SlotIdx = slotIdx;
            Broadcast(leaveLobbyPkt);
        }

        public void Broadcast(IMessage packet, string excluded = "") // Broadcast에서 제외시킬 이름
        {
            for (int i = 0; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null)
                    continue;
                if (_lobbyPlayers[i].UserName == excluded)
                    continue;
                
                _lobbyPlayers[i].Session.Send(packet);
            }
        }

        public void BroadcastSlot(S_EnterSlot packet)
        {
            packet.SlotType = Slot.Other;

            for (int i = 0; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null)
                    continue;

                if (i == packet.SlotIdx)
                {
                    var tmpPkt = new S_EnterSlot();
                    tmpPkt.SlotIdx = i;
                    tmpPkt.Nickname = packet.Nickname;
                    tmpPkt.SlotType = Slot.Player;
                    _lobbyPlayers[i].Session.Send(tmpPkt);
                }
                else
                    _lobbyPlayers[i].Session.Send(packet);
            }
        }

        public override void CheckLastPing()
        {
            foreach (var player in _lobbyPlayers)
            {
                if (player == null) continue;

                if (player.Session.CheckTimeout())
                    player.Session.Disconnect();
            }
        }

        public void AddPickRoom(int sessionId)
        {
            for (int i = 8; i <= 9; ++i)
            {
                if (_lobbyPlayers[i] != null && _lobbyPlayers[i].Session.SessionId == sessionId)
                    return; // 관전자일 경우에는
            }

            lock (this)
            {
                if (_isPickRoomCreating)
                    return;

                _isPickRoomCreating = true; // 생성 중 플래그

                PickRoom pr = RoomManager.Instance.AddRoom<PickRoom>();

                S_SpawnPick spawnPickPkt = new S_SpawnPick();

                int team1Idx = 0;
                int team2Idx = 4;

                for (int i = 0; i < 8; ++i)
                {
                    if (_lobbyPlayers[i] == null)
                        continue;

                    PickScenePlayerInfo playerInfo = new PickScenePlayerInfo();
                    playerInfo.UserName = _lobbyPlayers[i].UserName;
                    if (i < 4)
                    {
                        playerInfo.Team = 1;
                        playerInfo.PickIdx = team1Idx;
                    }
                    else
                    {
                        playerInfo.Team = 2;
                        playerInfo.PickIdx = team2Idx;
                    }
                    spawnPickPkt.Players.Add(playerInfo);

                    SendEnterPickPkt(_lobbyPlayers[i].Session, playerInfo.Team, playerInfo.PickIdx);

                    AddPickPlayer(_lobbyPlayers[i].Session, pr, playerInfo.UserName, playerInfo.Team, playerInfo.PickIdx);

                    if (playerInfo.Team == 1)
                        team1Idx++;
                    else
                        team2Idx++;
                }

                Broadcast(spawnPickPkt, "");

                for (int i = 0; i < 8; ++i)
                {
                    if (_lobbyPlayers[i] == null)
                        continue;

                    LeaveLobby(_lobbyPlayers[i].UserName);
                }

                BroadcastPlayerCntPkt();

                _isPickRoomCreating = false; // 생성 완료 후 플래그 해제
            }            
        }

        void AddPickPlayer(ClientSession session, PickRoom pr, string userName, int team, int pickIdx)
        {
            PickPlayer pp = new PickPlayer();
            pp.PickIdx = pickIdx;
            pp.Team = team;
            pp.UserName = userName;
            pp.Session = session;

            pr.EnterPick(pp);
        }

        void SendEnterPickPkt(ClientSession session, int team, int pickIdx)
        {
            S_EnterPick pickInfoPkt = new S_EnterPick();
            pickInfoPkt.Team = team;
            pickInfoPkt.PickIdx = pickIdx;
            session.Send(pickInfoPkt);
        }

        public void BroadcastPlayerCntPkt()
        {
            int playerCnt = 0;
            int observerCnt = 0;
            for(int i = 0; i < 8; ++i)
            {
                if( _lobbyPlayers[i] == null)
                    continue;
                playerCnt++;
            }
            for(int i = 8; i < _maxPlayer; ++i)
            {
                if (_lobbyPlayers[i] == null)
                    continue;
                observerCnt++;
            }

            S_LobbyCnt lobbyCntPkt = new S_LobbyCnt();
            lobbyCntPkt.PlayerCnt = playerCnt;
            lobbyCntPkt.ObserverCnt = observerCnt;
            Broadcast(lobbyCntPkt);
        }
    }
}