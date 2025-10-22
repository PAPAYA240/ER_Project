using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class GameRoom : Room
    {
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        ConcurrentDictionary<int, EnvironmentObject> _envs = new ConcurrentDictionary<int, EnvironmentObject>();
        ConcurrentDictionary<int, Monster> _monsters = new ConcurrentDictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();


        MonsterManager _monsterManager = new MonsterManager();
        CollisionManager _collisionManager = new CollisionManager();
        EnvManager _envManager = new EnvManager();

        Dictionary<int, Dictionary<int, Player>> _teams = new Dictionary<int, Dictionary<int, Player>>();
        Dictionary<CharacterType, SkillHandler> _skillHandlers = new Dictionary<CharacterType, SkillHandler>();

        bool _teamToggle = false;
        bool _dummyAdded = false;

        #region Phase, Time
        
        public TimeSpan TimeStamp { get { return _timeStampStopwatch.Elapsed; } }

        //private DateTime _timeStamp; // 게임이 시작된 시간
        private Stopwatch _timeStampStopwatch; // 게임 시작부터 얼마나 시간이 흘렀는지 측정하는 스톱워치

        //private DateTime _phaseStartTime; // 현재 페이즈가 시작된 시간
        private Stopwatch _phaseStopwatch; // 현재 페이즈가 얼마나 진행되었는가를 측정하는 스톱워치
        private Timer _phaseTransitionTimer; // 페이즈 전환을 예약하는 타이머
        private Timer _syncTimer; // 일정 주기마다 싱크 타이머를 호출하는 타이머
        public int CurPhase { get; private set; } = 5;

        public void ChangePhase(int newPhase)
        {
            if (CurPhase == newPhase) 
                return; 

            CurPhase = newPhase;
            //_phaseStartTime = DateTime.UtcNow; // 페이즈 시작 시간 기록 

            _phaseStopwatch.Restart(); // 페이즈 경과 시간 측정 시작/재시작

            // 현재 페이즈의 지속 시간을 가져옴
            int duration;
            if (DataManager.PhaseDict.TryGetValue(newPhase, out duration))
            {
                // 다음 페이즈 전환을 위한 타이머 설정
                // 이전 타이머가 있으면 Dispose 후 새로 생성

                TimeSpan newPhaseDuration = TimeSpan.FromSeconds(duration);

                _phaseTransitionTimer?.Dispose();
                _phaseTransitionTimer = new Timer(OnPhaseTimerElapsed, null, (int)newPhaseDuration.TotalMilliseconds, Timeout.Infinite); // 한 번만 실행되도록 설정
            }
            else
            {
                // 지속 시간이 정의되지 않은 페이즈 (수동 전환 필요)
                _phaseTransitionTimer?.Dispose(); // 혹시 모를 이전 타이머 정리
            }

            // 클라이언트들에게 페이즈 변경 사실을 통보 (네트워크 전송)
            SyncTimer();

            // 특별한 페이즈에 대한 추가 로직
            switch (newPhase)
            {
                case 0:
                    // 게임 시작 시 필요한 초기화 (예: 맵 생성, 플레이어 스폰)
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
            }
        }

        public void SyncTimer(object state = null)
        {
            S_SyncTimer syncTimerPacket = new S_SyncTimer();

            syncTimerPacket.Phase = CurPhase;
            syncTimerPacket.CurrentTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); 
            syncTimerPacket.PhaseEndTime = CalculatePhaseServerEndTime(CurPhase); 

            Push(Broadcast, syncTimerPacket);
        }

        private long CalculatePhaseServerEndTime(int phase)
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)GetRemainingPhaseTime().TotalMilliseconds;
        }


        public TimeSpan GetRemainingPhaseTime()
        {
            // 페이즈 지속 시간이 정의되지 않았다면 남은 시간 없음으로 처리
            if (!DataManager.PhaseDict.TryGetValue(CurPhase, out int totalDuration))
            {
                return TimeSpan.Zero;
            }

            // 스톱워치로 측정한 경과 시간
            TimeSpan elapsedTime = _phaseStopwatch.Elapsed;
            TimeSpan remainingTime = TimeSpan.FromSeconds(totalDuration) - elapsedTime;

            return remainingTime.TotalSeconds > 0 ? remainingTime : TimeSpan.Zero;
        }

        private void OnPhaseTimerElapsed(object state)
        {
            int nextPhase = CurPhase + 1;
            if (nextPhase != 5)
            {
                ChangePhase(nextPhase);
            }
            else
            {
                //모든 페이즈 종료
                _phaseTransitionTimer?.Dispose();
                _phaseStopwatch.Stop();
            }
        }

        public CollisionManager CollisionManager { get { return _collisionManager; } private set { } }
        #endregion

        public bool TryGetMonster(int objectId, out Monster monster)
        {
            return _monsters.TryGetValue(objectId, out monster);
        }
         public CollisionManager CollManager { get { return _collisionManager; } private set { } }

        public void Init(int mapId)
        {
            // Spawn NavMesh
            Pathfinding.Initialize();

            // Spawn Monster
            _monsterManager.Init(this);
             
            // Spawn Env
            _envManager.Init(this);

            // Timer
            _timeStampStopwatch = new Stopwatch(); 
            _timeStampStopwatch.Restart(); // 게임 시간 측정 시작.
            _phaseStopwatch = new Stopwatch();
            ChangePhase(0);
            _syncTimer = new Timer(SyncTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); //주기적으로 동기화

            _skillHandlers[CharacterType.Theodore] = new TheodoreSkillHandler();
        }

        public override void Update()
        {
            foreach (Projectile projectile in _projectiles.Values)
            {
                projectile.Update();
            }

            foreach (Player player in _players.Values)
            {
                player.Update();
            }

            foreach (Player player in _players.Values)
            {
                List<int> visibleObjs = new List<int>();
                visibleObjs.AddRange(GetObjectsInRange(_players, player));
                visibleObjs.AddRange(GetObjectsInRange(_projectiles, player));
                AddVisibleObjects(visibleObjs, _envs, player);
                AddVisibleObjects(visibleObjs, _monsters, player);
                player.SendVisibleObjsPkt(visibleObjs);
            }

            List<Monster> monstersToUpdate = new List<Monster>(_monsters.Values);
            foreach (Monster monster in monstersToUpdate)
                monster.Update();

            Flush();

            _collisionManager.CurTick = Environment.TickCount;
            _collisionManager.Flush();
            _collisionManager.CheckAllCollisions(_teams, _monsters, _projectiles);
            _collisionManager.Update();
            
            BroadcastVisibleObjs();
            CheckLastPing();
        }

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Init();
                player.Info.Player.Team = AssignTeam();
                player.Info.Player.Weapon = FindWeapon(player.Info.Player.CharType);

                if (!_teams.TryGetValue(player.Info.Player.Team, out var teamPlayers))
                {
                    teamPlayers = new Dictionary<int, Player>(); 
                    _teams[player.Info.Player.Team] = teamPlayers;
                }
                teamPlayers.Add(player.Id, player);

                ObjectManager.Instance.RegisterTeam(gameObject.Id, player.Info.Player.Team);

                player.Room = this;
                
                // 본인한테 정보 전송
                {
                    // Temp Cobalt Exp
                    player.Info.StatInfo.Exp = 15800;

                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                            spawnPacket.Objects.Add(p.Info);
                    }

                    foreach (Monster m in _monsters.Values)
                        spawnPacket.Objects.Add(m.Info);

                    foreach (Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);

                    foreach (EnvironmentObject env in _envs.Values)
                        spawnPacket.Objects.Add(env.Info);

                    player.Session.Send(spawnPacket);

                    int levelUpCnt = player.CheckLevelUp();
                    if (levelUpCnt > 0)
                        BroadcastLevelUp(player.Id, levelUpCnt, player.Info.Player.CharType);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                if (_monsters == null)
                    _monsters = new ConcurrentDictionary<int, Monster>();

                monster.Room = this;
                _monsters.TryAdd(gameObject.Id, monster);
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;
            }
            else if (type == GameObjectType.Environment)
            {
                EnvironmentObject env = gameObject as EnvironmentObject;
                if (env == null)
                    _envs = new ConcurrentDictionary<int, EnvironmentObject>();

                env.Room = this;
                _envs.TryAdd(gameObject.Id, env);
            }

            // 타인한테 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPacket);
                }
            }

            SyncTimer();
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;
                var myTeam = _teams[player.Info.Player.Team];
                myTeam.Remove(player.Id);

                player.Room = null;
                player.OnDestroy();

                // 본인한테 정보 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = null;
                if (_monsters.Remove(objectId, out monster) == false)
                    return;

                monster.Room = null;
                _monsterManager.Add(-1);
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if (_projectiles.Remove(objectId, out projectile) == false)
                    return;

                projectile.Room = null;
            }

            // 타인한테 정보 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != objectId)
                        p.Session.Send(despawnPacket);
                }
            }
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            // todo : 검증

            // 일단 서버에서 좌표 이동
            player.Info.PosInfo.PosX = movePacket.PosInfo.PosX;
            player.Info.PosInfo.PosY = movePacket.PosInfo.PosY;
            player.Info.PosInfo.PosZ = movePacket.PosInfo.PosZ;
            player.Info.RotInfo.Qx = movePacket.RotInfo.Qx;
            player.Info.RotInfo.Qy = movePacket.RotInfo.Qy;
            player.Info.RotInfo.Qz = movePacket.RotInfo.Qz;
            player.Info.RotInfo.Qw = movePacket.RotInfo.Qw;

            // 다른 플레이어한테도 알려준다
            S_Move resMovePacket = new S_Move();
            resMovePacket.ObjectId = player.Info.ObjectId;
            resMovePacket.PosInfo = movePacket.PosInfo;
            resMovePacket.RotInfo = movePacket.RotInfo;
            resMovePacket.IsWarp = movePacket.IsWarp;
            Broadcast(resMovePacket);
        }

        public void HandleVF(Player player, C_Fx skillPacket)
        {
            if (player == null)
                return;

            S_Fx effect = new S_Fx() {
                ObjectId = player.Info.ObjectId,
                FxInfo = skillPacket.FxInfo,
            };
            Broadcast(effect);
        }

        #region Handler Skill
        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null)
                return;

            if (skillPacket.SkillInfo.Amplification)
            {
                HandleAmplificationSkill(player, skillPacket);
            }
            else
            {
                HandleNormalSkill(player, skillPacket);
            }
        }

        private void HandleAmplificationSkill(Player player, C_Skill skillPacket)
        {
            ObjectInfo info = player.Info;

            info.PosInfo.State = CreatureState.Skill;
            S_Skill skillPacketToSend = new S_Skill()
            {
                CanUse = true,
                ObjectId = player.Info.ObjectId,
                SkillInfo = skillPacket.SkillInfo
            };
            Broadcast(skillPacketToSend);

            if (_skillHandlers.TryGetValue(player.Info.Player.CharType, out var handler))
                handler.CanUse(player, skillPacketToSend);
        }

        private void HandleNormalSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) return;

            ObjectInfo info = player.Info;
            S_Skill skill = new S_Skill() { SkillInfo = new SkillInfo() };
            KeyCode keyCode = (KeyCode)skillPacket.SkillInfo.KeyCode;

            skill.ChargeRatio = skillPacket.ChargeRatio;

            if (!player.CanUseSkill(keyCode))
            {
                skill.CanUse = false;
                player.Session.Send(skill);
                return;
            }
            else
                player.CommitSkillUsage(keyCode);

            foreach (int targetid in skillPacket.TargetsId)
            {
                if (TryGetMonster(targetid, out Monster target))
                {
                    player.Target = target;
                    player.SkillTarget = target;
                    player.UsedTargetingSkill = keyCode;

                }
                else if (_players.TryGetValue(targetid, out Player skillTarget))
                {
                    player.SkillTarget = skillTarget;
                    player.UsedTargetingSkill = keyCode;
                }
            }

            info.PosInfo.State = CreatureState.Skill;
            skill.CanUse = true;
            skill.ObjectId = info.ObjectId;
            skill.SkillInfo = new SkillInfo
            {
                SkillId = skillPacket.SkillInfo.SkillId,
                KeyCode = skillPacket.SkillInfo.KeyCode,
            };
            skill.CostInfo = new CostInfo
            {
                CoolTime = player.GetCoolTime(keyCode),
                Stamina = player.Stamina,
            };
            Broadcast(skill);

            SkillData skillData = null;
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[info.Player.CharType];

            if (skills.TryGetValue((KeyCode)skillPacket.SkillInfo.KeyCode, out skillData) == false)
                return;

            _collisionManager.AddHitbox(player, info.Player.CharType, (KeyCode)skillPacket.SkillInfo.KeyCode,
                new Vector2(skillPacket.TargetPosX, skillPacket.TargetPosZ), skillPacket.ChargeRatio);
        }
        #endregion
        public void HandleAnim(Player player, C_Anim animPacket)
        {
            if (player == null)
                return;

            S_Anim anim = new S_Anim() { AnimInfo = new AnimInfo() };
            anim.ObjectId = player.Id;
            anim.AnimInfo = animPacket.AnimInfo;
            Broadcast(anim);           
        }

        public void HandleAttackSkillTarget(Player player, C_AttackSkillTarget attackSkillTarget)
        {
            if (player == null || player.SkillTarget == null)
                return;

            float damage = 0f;

            if(player.SkillTarget.ObjectType == GameObjectType.Player)
                damage = _collisionManager.CalcDamage(player, player.SkillTarget as Player, player.UsedTargetingSkill);
            else
                damage = _collisionManager.CalcDamage(player, player.SkillTarget.Stat, player.UsedTargetingSkill);   

            player.SkillTarget.OnDamaged(player, damage);
        }

        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach(Player player in _players.Values)
            {
                if(condition.Invoke(player))
                    return player;
            }
            return null;
        }

        public void Broadcast(IMessage packet)
        {
            foreach (Player p in _players.Values)
            {
                p.Session.Send(packet);
            }
        }

        public override void CheckLastPing()
        {
            foreach(Player p in _players.Values)
            {
                if (p.Session.CheckTimeout())
                    p.Session.Disconnect();
            }
        }

        int AssignTeam()
        {
            _teamToggle = !_teamToggle;
            return _teamToggle ? 1 : 2;
        }
        private void AddVisibleObjects<T>(List<int> visibleObjs, ConcurrentDictionary<int, T> dict, Player player, int range = 8) where T : GameObject
        {
            foreach (var pair in dict)
            {
                GameObject go = pair.Value;
                if (go.Id == player.Id)
                    continue;

                if (go.PosInfo.Distance(player.PosInfo) < range)
                {
                    visibleObjs.Add(go.Id);
                }
            }
        }

        List<int> GetObjectsInRange<T>(Dictionary<int, T> dict, Player player, int range = 8) where T : GameObject
        {
            List<int> result = new List<int>();

            foreach (GameObject go in dict.Values)
            {
                if (go.PosInfo.Distance(player.PosInfo) < range)
                {
                    if (go.Id == player.Id)
                        continue;
                    result.Add(go.Id);
                }

            }
            return result;
        }


        void BroadcastVisibleObjs()
        {
            foreach (Player player in _players.Values)
            {
                List<int> visibleObjs = new List<int>();
                visibleObjs.AddRange(GetObjectsInRange(_players, player));
                AddVisibleObjects(visibleObjs, _monsters, player);
                AddVisibleObjects(visibleObjs, _envs, player);
                visibleObjs.AddRange(GetObjectsInRange(_projectiles, player));
                player.SendVisibleObjsPkt(visibleObjs);
            }
        }

        void BroadcastLevelUp(int objectId, int levelUpCnt, CharacterType charType)
        {
            S_LevelUp levelUpPkt = new S_LevelUp();
            levelUpPkt.ObjectId = objectId;
            levelUpPkt.LevelUpCnt = levelUpCnt;

            StatInfo statInfo = new StatInfo(DataManager.StatGrowthDict[charType]);
            statInfo.MultiplyForGrowth(levelUpCnt);
            levelUpPkt.StatGrowth = statInfo;

            Broadcast(levelUpPkt);
        }

        public void SkillLevelUp(int id, int key)
        {
            S_SkillLevelUp skillLevelUpPacket = new S_SkillLevelUp();
            skillLevelUpPacket.KeyCode = key;

            Player player = FindPlayer(p =>
            {
                if (p.Id == id) return true;
                return false;
            });

            player.Session.Send(skillLevelUpPacket);
        }

        public void AddDummyPlayers(ClientSession clientSession,  List<CharacterType> dummyPlayers)
        {
            if (_dummyAdded)
                return;

            S_Spawn spawnPacket = new S_Spawn();
            Random rand = new Random();
            foreach (CharacterType charType in dummyPlayers)
            {
                Player dummyPlayer = ObjectManager.Instance.Add<Player>();
                {
                    dummyPlayer.Info.Name = $"DummyPlayer_{dummyPlayer.Id}";
                    dummyPlayer.Info.PosInfo.State = CreatureState.Idle;
                    dummyPlayer.Info.PosInfo.PosX = rand.Next(-4,4);
                    dummyPlayer.Info.PosInfo.PosY = 0;
                    dummyPlayer.Info.PosInfo.PosZ = rand.Next(-4, 4);
                    dummyPlayer.Info.Player = new PlayerInfo();
                    dummyPlayer.Info.Player.CharType = charType;
                    dummyPlayer.Init();

                    StatInfo stat = null;
                    DataManager.StatDict.TryGetValue(charType, out stat);
                    dummyPlayer.Stat.MergeFrom(stat);
                    dummyPlayer.Hp = dummyPlayer.MaxHp;
                    dummyPlayer.Stamina = dummyPlayer.MaxStamina;
                    dummyPlayer.Session = clientSession;
                    _players.Add(dummyPlayer.Id, dummyPlayer);
                    dummyPlayer.Info.Player.Team = AssignTeam();

                    if (!_teams.TryGetValue(dummyPlayer.Info.Player.Team, out var teamPlayers))
                    {
                        teamPlayers = new Dictionary<int, Player>();
                        _teams[dummyPlayer.Info.Player.Team] = teamPlayers;
                    }
                    teamPlayers.Add(dummyPlayer.Id, dummyPlayer);

                    ObjectManager.Instance.RegisterTeam(dummyPlayer.Id, dummyPlayer.Info.Player.Team);

                    dummyPlayer.Room = this;
                }
                spawnPacket.Objects.Add(dummyPlayer.Info);
            }
            clientSession.Send(spawnPacket);

            _dummyAdded = true;
        }

        private Weapon FindWeapon(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Rozzi:
                    return Weapon.Pistol;
                case CharacterType.Yuki:
                    return Weapon.TwoHandSword;
                case CharacterType.Abigail:
                    return Weapon.Axe;
                case CharacterType.Theodore:
                    return Weapon.SniperRifle;
                case CharacterType.Hyunwoo:
                    return Weapon.Glove;
            }

            return Weapon.Pistol;
        }
    }
}
