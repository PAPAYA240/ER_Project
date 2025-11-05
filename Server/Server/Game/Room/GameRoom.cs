using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using static Server.Data.DataUtils;
using System.Threading;
using static Server.Game.GameObject;

namespace Server.Game
{
    public partial class GameRoom : Room
    {
        ConcurrentDictionary<int, Player> _players = new ConcurrentDictionary<int, Player>();
        ConcurrentDictionary<int, EnvironmentObject> _envs = new ConcurrentDictionary<int, EnvironmentObject>();
        ConcurrentDictionary<int, Monster> _monsters = new ConcurrentDictionary<int, Monster>();
        ConcurrentDictionary<int, Projectile> _projectiles = new ConcurrentDictionary<int, Projectile>();

        MonsterManager _monsterManager = new MonsterManager();
        CollisionManager _collisionManager = new CollisionManager();
        EnvironmentManager _envManager = new EnvironmentManager();

        Dictionary<int, Dictionary<int, Player>> _teams = new Dictionary<int, Dictionary<int, Player>>();
        Dictionary<CharacterType, SkillHandler> _skillHandlers = new Dictionary<CharacterType, SkillHandler>();

        bool _teamToggle = false;
        bool _dummyAdded = true;    // TEMP : Dummy

        public EnvironmentManager GetEnvManager { get { return _envManager; } private set { _envManager = value; } }

        public int CurTick { get; set; }

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

            Console.WriteLine($"Phase Change : {CurPhase}");

            // 특별한 페이즈에 대한 추가 로직
            switch (newPhase)
            {
                case 0:
                    // 게임 시작 시 필요한 초기화 (예: 맵 생성, 플레이어 스폰)
                    break;
                case 1:
                    {
                        foreach (var p in _players)
                            Push(p.Value.EquipItemSet, p.Value.Info.Player.CharType, CurPhase - 1);
                    }
                    break;
                case 2:
                    {
                        foreach (var p in _players)
                            Push(p.Value.EquipItemSet, p.Value.Info.Player.CharType, CurPhase - 1);
                    }
                    break;
                case 3:
                    {
                        foreach (var p in _players)
                            Push(p.Value.EquipItemSet, p.Value.Info.Player.CharType, CurPhase - 1);
                    }
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
        #endregion

        public bool TryGetMonster(int objectId, out Monster monster)
        {
            return _monsters.TryGetValue(objectId, out monster);
        }
        public CollisionManager CollManager { get { return _collisionManager; } private set { } }

        public PathfindInstance PathFind { get; set; }



        public void Init(int mapId)
        {
            PathFind = new PathfindInstance(0);
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

            _collisionManager.Init();

            // Skill Register
            SkillRegistry.InitRegister();
        }

        public override void Update()
        {
            CurTick = Environment.TickCount;
            TimeUtil.Update(CurTick);

            foreach (Projectile projectile in _projectiles.Values)
            {
                projectile.Update();
            }

            foreach (Player player in _players.Values)
            {
                player.Update();
            }
            foreach (Monster monster in _monsters.Values)
            {   
                monster.Update();
            }
            foreach (EnvironmentObject env in _envs.Values)
            {
                env.Update();
            }

            foreach (Player player in _players.Values)
            {
                List<int> visibleObjs = new List<int>();
                AddVisibleObjects(visibleObjs, _players, player);
                AddVisibleObjects(visibleObjs, _monsters, player);
                AddVisibleObjects(visibleObjs, _projectiles, player);
                AddVisibleObjects(visibleObjs, _envs, player);
                player.SendVisibleObjsPkt(visibleObjs);
            }

            foreach (Player player in _players.Values)
                player.RemoveExpiredStatusEffects();

            foreach (var monster in _monsters.Values)
                monster.RemoveExpiredStatusEffects();

            Flush();

            _collisionManager.CurTick = CurTick;
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
                _players.TryAdd(gameObject.Id, player);
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
                player.Init();

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

                    // 페이즈에 해당하는 아이템 장착
                    if (CurPhase > 0)
                        player.EquipItemSet(player.Info.Player.CharType, CurPhase - 1);

                    // 시간 동기화
                    SyncTimer();
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;

                monster.Room = this;
                _monsters.TryAdd(gameObject.Id, monster);
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                projectile.Room = this;
                _projectiles.TryAdd(gameObject.Id, projectile);
            }
            else if (type == GameObjectType.Environment)
            {
                EnvironmentObject env = gameObject as EnvironmentObject;

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
                projectile.Owner = null;
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

        #region Handler
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
            //// 나영아 도와줘
            //if (player == null) return;

            //ObjectInfo info = player.Info;
            //S_Skill skill = new S_Skill() { SkillInfo = new SkillInfo() };
            //KeyCode keyCode = (KeyCode)skillPacket.SkillInfo.KeyCode;

            //skill.ChargeRatio = skillPacket.ChargeRatio;

            //if (!player.CanUseSkill(keyCode))
            //{
            //    skill.CanUse = false;
            //    player.Session.Send(skill);
            //    return;
            //}
            //else
            //    player.CommitSkillUsage(keyCode);

            ////foreach (int targetid in skillPacket.TargetsId)
            ////{
            ////    if (TryGetMonster(targetid, out Monster target))
            ////    {
            ////        player.Target = target;
            ////        player.SkillTarget = target;
            ////        player.UsedTargetingSkill = keyCode;

            ////    }
            ////    else if (_players.TryGetValue(targetid, out Player skillTarget))
            ////    {
            ////        player.SkillTarget = skillTarget;
            ////        player.UsedTargetingSkill = keyCode;
            ////    }
            ////}

            //info.PosInfo.State = CreatureState.Skill;
            //skill.CanUse = true;
            //skill.ObjectId = info.ObjectId;
            //skill.SkillInfo = new SkillInfo
            //{
            //    SkillId = skillPacket.SkillInfo.SkillId,
            //    KeyCode = skillPacket.SkillInfo.KeyCode,
            //};
            //skill.CostInfo = new CostInfo
            //{
            //    CoolTime = player.GetCoolTime(keyCode),
            //    Stamina = player.Stamina,
            //};
            //Broadcast(skill);

            ////float damage = 0f;

            ////if(player.SkillTarget.ObjectType == GameObjectType.Player)
            ////    damage = _collisionManager.CalcDamage(player, player.SkillTarget as Player, player.UsedTargetingSkill);
            ////else
            ////    damage = _collisionManager.CalcDamage(player, player.SkillTarget.Stat, player.UsedTargetingSkill);   

            //// 프로젝타일
            //Projectile proj= FindProjectile(player);
            //if (proj != null)
            //    proj.IsActive = true;

            //_collisionManager.AddHitbox(player, info.Player.CharType, (KeyCode)skillPacket.SkillInfo.KeyCode,
            //    new Vector2(skillPacket.MousePosX, skillPacket.MousePosZ), skillPacket.ChargeRatio);
        }

        #endregion

        public void AttackSkillTarget(Player player, GameObject target, KeyCode keyCode) // 타게팅 스킬. 대상 1명.
        {
            if (player == null)
                return;

            float damage = 0f;

            if (target.ObjectType == GameObjectType.Player)
                damage = _collisionManager.CalcDamage(player, target as Player, keyCode);
            else
                damage = _collisionManager.CalcDamage(player, target.Stat, keyCode);

            if(player.Info.Player.CharType == CharacterType.Abigail && keyCode == KeyCode.E)
            {
                S_RemoveAbigailCoord removeAbigailCoordPkt = new S_RemoveAbigailCoord();
                removeAbigailCoordPkt.ObjectId = target.Id;
                player.Room.Broadcast(removeAbigailCoordPkt);

                int removeCnt = target.RemoveStatusEffects("Coord");
                if (removeCnt > 0) // 표식 있는 적에게 E 사용시 E 쿨타임 초기화
                    player.Skill.SetCooldown(keyCode, 0);
            }

            target.OnDamaged(player, damage);
        }

        public void HandleMoveSync(Player player, C_MoveSync movePacket)
        {
            if (player == null)
                return;

            //var clientPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);

            //if (movePacket.IsSkillMotion || player.Flags.IsInSkillMotion)
            //{
            //    // 감시 모드: 브로드캐스트 X
            //    // 가벼운 보정 1회
            //    player.SendMovePacket(new PositionInfo { PosX = player.PosInfo.PosX, PosY = player.PosInfo.PosY, PosZ = player.PosInfo.PosZ },
            //                         new RotationInfo(player.RotInfo));

            //    return;
            //}

            player.PosInfo.MergeFrom(movePacket.PosInfo);
            player.RotInfo.MergeFrom(movePacket.RotInfo);

            player.SendMovePacket(new PositionInfo(player.PosInfo), new RotationInfo(player.RotInfo));
        }

        public void HandleAnim(Player player, C_Anim animPacket)
        {
            if (player == null)
                return;

            S_Anim anim = new S_Anim() { AnimInfo = new AnimInfo() };
            anim.ObjectId = player.Id;
            anim.AnimInfo = animPacket.AnimInfo;
            Broadcast(anim);
        }

        public void HandleRest(Player player, C_Rest pkt)
        {
            if (player == null)
                return;
            if (player.IsDead)
                return;

            player.ChangeState(new Player_RestState(pkt.IsRest));
        }

        public void HandleDeath(Player player, C_Death pkt)
        {
            if (player == null)
                return;

            player.Hp = 0;
            player.IsDeath = true;
        }

        public void HandleKeyInputForTest(Player player, C_KeyInputForTest pkt)
        {
            if (player == null)
                return;

            player.Skill.SetCooldown(KeyCode.R, 0f);
        }

        #endregion
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
                AddVisibleObjects(visibleObjs, _players, player);
                AddVisibleObjects(visibleObjs, _monsters, player);
                AddVisibleObjects(visibleObjs, _projectiles, player);
                AddVisibleObjects(visibleObjs, _envs, player);
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
                    _players.TryAdd(dummyPlayer.Id, dummyPlayer);
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

        #region Search
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
        public Projectile FindProjectile(Creature owner)
        {
            foreach (Projectile projectile in _projectiles.Values)
            {
                if (projectile.Owner == owner)
                    return projectile;
            }
            return null;
        }
        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }
            return null;
        }

        public GameObject FindNearest(int id, Vector2 pos, float radius)
        {
            GameObject nearest = null;
            float nearestDistSq = radius * radius;

            foreach (var kvp in _players)
            {
                if (kvp.Key == id)
                    continue;
                var player = kvp.Value;
                Vector2 playerPos = new Vector2(player.PosInfo.PosX, player.PosInfo.PosZ);
                float distSq = Vector2.DistanceSquared(pos, playerPos);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = player;
                }
            }

            foreach (var kvp in _monsters)
            {
                if (kvp.Key == id)
                    continue;
                var monster = kvp.Value;
                Vector2 monsterPos = new Vector2(monster.PosInfo.PosX, monster.PosInfo.PosZ);
                float distSq = Vector2.DistanceSquared(pos, monsterPos);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = monster;
                }
            }

            return nearest;
        }

        #endregion

        public void AddStatusEffect(Creature creature, StatusEffect statusEffect)
        {
            creature.AddStatusEffect(statusEffect);
        }

        public void BehindDash(Player player)
        {
            if (player.CurrentState is Player_SkillState skillState)
                skillState.Handler.OnCollision(player);
        }
    }
}
