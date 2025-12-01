using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;

namespace Server.Game
{
    public class PickRoom : Room
    {
        const int _maxPlayer = 8;

        PickPlayer[] _pickPlayers = new PickPlayer[_maxPlayer];

        int _playerCnt = 0;

        int _count = 30;
        float _accTime = 0;

        bool _isAllReady = false;
        int _enterGame = 0; // 0: 아직 호출되지 않음, 1: 이미 호출됨

        private readonly object _lock = new object();

        public override void Init()
        {
             
        }

        public override void Update()
        {
            Flush();
            CheckLastPing();
            CheckAllReady();
            Countdown();
        }

        public void EnterPick(PickPlayer pp)
        {
            _pickPlayers[pp.PickIdx] = pp;
            pp.Session.CurRoom = RoomId;
            _playerCnt++;
        }

        public void LeavePick(int pickIdx)
        {
            _pickPlayers[pickIdx] = null;

            bool allNull = true;
            foreach (var p in _pickPlayers) 
            {
                if(p != null)
                {
                    allNull = false;
                    break;
                }
            }

            if (allNull)
                RoomManager.Instance.Remove(RoomId);
        }

        public void Broadcast(IMessage packet)
        {
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                _pickPlayers[i].Session.Send(packet);
            }
        }

        public void BroadcastToTeam(IMessage packet, int pickIdx)
        {
            if (_pickPlayers[pickIdx] == null)
                return;

            if (_isAllReady)
            {
                Broadcast(packet);
                return;
            }               

            int team = _pickPlayers[pickIdx].Team;

            for (int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;
                if (_pickPlayers[i].Team != team)
                    continue;
                _pickPlayers[i].Session.Send(packet);
            }
        }

        void Countdown()
        {
            lock (_lock)
            {
                if (_isAllReady && _count <= 0)
                {
                    if (Interlocked.Exchange(ref _enterGame, 1) == 0)
                    {
                        S_LoadGameScene loadGameScenePkt = new S_LoadGameScene();
                        Broadcast(loadGameScenePkt);

                        GameRoom gr = RoomManager.Instance.AddRoom<GameRoom>();
                        EnterGame(gr);
                    }
                }

                _accTime += TimeUtil.Instance.DeltaTime;

                if (_accTime >= 1f)
                {
                    _accTime -= 1f;
                    _count--;

                    if (_count < 0 && false == _isAllReady)
                    {
                        PickRandomChar();

                        _isAllReady = true;
                        _count = 5;

                        SendPickSoundPkts();

                        SendPickAllReadyPkt(0, 4, 4, _maxPlayer);
                        SendPickAllReadyPkt(4, _maxPlayer, 0, 4);
                    }

                    S_Countdown countdownPkt = new S_Countdown();
                    countdownPkt.Count = _count;
                    Broadcast(countdownPkt);
                }
            }            
        }

        void CheckAllReady()
        {
            lock (_lock)
            {
                if (_playerCnt <= 0 || _isAllReady)
                    return;

                int readyCnt = 0;
                for (int i = 0; i < _maxPlayer; ++i)
                {
                    if (_pickPlayers[i] == null)
                        continue;

                    if (_pickPlayers[i].IsReady)
                        readyCnt++;
                }

                if (readyCnt == _playerCnt)
                {
                    _isAllReady = true;
                    _count = 5;

                    SendPickAllReadyPkt(0, 4, 4, _maxPlayer);
                    SendPickAllReadyPkt(4, _maxPlayer, 0, 4);
                }
            }
        }

        public void OnReadyBtnClick(ClientSession session)
        {
            PickPlayer pp = null;
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                if (_pickPlayers[i].Session != session)
                    continue;

                pp = _pickPlayers[i];
                break;
            }

            if (pp == null)
                return;

            if (pp.Session.MyCharacter == CharacterType.CharacterNone)
                return;

            pp.IsReady = true;

            S_ReadyBtn readyBtn = new S_ReadyBtn();
            pp.Session.Send(readyBtn);

            SendPickSoundPkt(pp.Session);
        }

        void EnterGame(GameRoom room)
        {
            if (room == null)
                return;

            for (int i = 0;  i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                ClientSession clientSession = _pickPlayers[i].Session;

                // create hyunwoo
                if (clientSession.MyCharacter == CharacterType.Hyunwoo)
                    clientSession.MyPlayer = ObjectManager.Instance.Add<Hyunwoo>();
                else
                    clientSession.MyPlayer = ObjectManager.Instance.Add<Player>();
                
                {
                    clientSession.MyPlayer.Info.Name = $"Player_{clientSession.MyPlayer.Info.ObjectId}";
                    clientSession.MyPlayer.Info.PosInfo.State = CreatureState.Idle;
                    clientSession.MyPlayer.Info.PosInfo.PosX = 0;
                    clientSession.MyPlayer.Info.PosInfo.PosY = 0;
                    clientSession.MyPlayer.Info.Player = new PlayerInfo();
                    clientSession.MyPlayer.Info.Player.CharType = clientSession.MyCharacter;
                    clientSession.MyPlayer.Info.Player.Nickname = _pickPlayers[i].UserName;

                    StatInfo stat = null;
                    DataManager.StatDict.TryGetValue(clientSession.MyCharacter, out stat);
                    clientSession.MyPlayer.Stat.MergeFrom(stat);
                    clientSession.MyPlayer.Hp = clientSession.MyPlayer.MaxHp;
                    clientSession.MyPlayer.Stamina = clientSession.MyPlayer.MaxStamina;
                    clientSession.MyPlayer.Session = clientSession;
                }

                Player player = clientSession.MyPlayer;
                if (player == null)
                    continue;
                
                clientSession.CurRoom = room.RoomId;
                room.Push(room.EnterGame, player, _pickPlayers[i].Team);
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

        static readonly Random _rand = new Random();

        CharacterType GetRandomCharacter()
        {
            int last = Enum.GetValues(typeof(CharacterType))
               .Cast<int>()
               .Max();

            int rand = _rand.Next(1, last);
            return (CharacterType)rand;
        }

        void PickRandomChar()
        {
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                if (_pickPlayers[i].Session == null)
                    continue;

                if (_pickPlayers[i].Session.MyCharacter != CharacterType.CharacterNone)
                    continue;

                _pickPlayers[i].Session.MyCharacter = GetRandomCharacter();

                S_RandomPick randomPickPkt = new S_RandomPick();
                randomPickPkt.CharType = _pickPlayers[i].Session.MyCharacter;
                _pickPlayers[i].Session.Send(randomPickPkt);
            }
        }

        public void SetWeapon(Weapon weapon, int pickIdx)
        {
            if (_pickPlayers[pickIdx] == null)
                return;
            _pickPlayers[pickIdx].Weapon = weapon;
        }

        public void SetTrait(TraitType trait, int pickIdx)
        {
            if (_pickPlayers[pickIdx] == null)
                return;
            _pickPlayers[pickIdx].Trait = trait;
        }

        void SendPickAllReadyPkt(int startIdx, int endIdx, int opponentStartIdx, int opponentEndIdx)
        {
            List<CharacterType> opponentCharList = new List<CharacterType>();
            List<Weapon> opponentWeaponList = new List<Weapon>();
            List<TraitType> opponentTraitList = new List<TraitType>();

            for (int i = opponentStartIdx; i < opponentEndIdx; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                opponentCharList.Add(_pickPlayers[i].Session.MyCharacter);
                opponentWeaponList.Add(_pickPlayers[i].Weapon);
                opponentTraitList.Add(_pickPlayers[i].Trait);
            }

            if (opponentCharList.Count == 0)
                return;

            S_PickAllReady pickAllReadyPkt = new S_PickAllReady();
            if (startIdx == 0)
                pickAllReadyPkt.StartIdx = 4; // 상대방 
            else
                pickAllReadyPkt.StartIdx = 0; // 상대방 
            pickAllReadyPkt.CharList.Add(opponentCharList);
            pickAllReadyPkt.WeaponList.Add(opponentWeaponList);
            pickAllReadyPkt.TraitList.Add(opponentTraitList);

            for (int i = startIdx; i < endIdx; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;

                _pickPlayers[i].Session.Send(pickAllReadyPkt);
            }
        }

        public bool IsReady(int pickIdx)
        {
            if (_pickPlayers[pickIdx] == null)
                return false;

            return _pickPlayers[pickIdx].IsReady;
        }

        void SendPickSoundPkts()
        {
            for(int i = 0; i < _maxPlayer; ++i)
            {
                if (_pickPlayers[i] == null)
                    continue;
                if (_pickPlayers[i].IsReady)
                    continue;

                SendPickSoundPkt(_pickPlayers[i].Session);
            }
        }

        void SendPickSoundPkt(ClientSession session)
        {
            S_PickSound pickSound = new S_PickSound();
            pickSound.CharType = session.MyCharacter;
            session.Send(pickSound);
        }
    }
}