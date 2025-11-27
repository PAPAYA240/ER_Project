using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using static Player_StunState;
using static Server.Data.DataUtils;
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
        BeaconManager _beaconManager = new BeaconManager();

        Dictionary<int, Dictionary<int, Player>> _teams = new Dictionary<int, Dictionary<int, Player>>();
        Dictionary<CharacterType, SkillHandler> _skillHandlers = new Dictionary<CharacterType, SkillHandler>();

        public EnvironmentManager GetEnvManager { get { return _envManager; } private set { _envManager = value; } }


        #region Spawn
        public SpawnPointRegistry SpawnRegistry { get; private set; }
        public SpawnSystem Spawn { get; private set; }
        public TeleportSystem Teleport { get; private set; }
        #endregion

        #region Phase, Time

        private long _startTick;         // 게임 시작 기준 Tick
        private long _phaseStartTick;    // 현재 페이즈 시작 Tick
        private long _phaseEndTick;      // 현재 페이즈 종료 Tick

        public int CurPhase { get; private set; } = 0;

        public void StartPhase()
        {
            _startTick = TimeUtil.Instance.LastTick;
            _phaseStartTick = _startTick;
            ChangePhase(0);
            if (DataManager.PhaseDict.TryGetValue(0, out int durationSec))
                _phaseEndTick = TimeUtil.Instance.LastTick + durationSec * 1000L;
        }

        public long GameElapsedMs => TimeUtil.Instance.LastTick - _startTick;
        public long PhaseElapsedMs => TimeUtil.Instance.LastTick - _phaseStartTick;

        public void ChangePhase(int newPhase)
        {
            CurPhase = newPhase;
            _phaseStartTick = TimeUtil.Instance.LastTick;

            // 페이즈 지속 시간 가져오기
            if (DataManager.PhaseDict.TryGetValue(newPhase, out int durationSec))
            {
                _phaseEndTick = TimeUtil.Instance.LastTick + durationSec * 1000L;
            }
            else
            {
                _phaseEndTick = long.MaxValue; // 지속시간 정의 안되면 수동 종료
            }

            _monsterManager.Add(CurPhase, this);

            // 클라이언트 동기화
            SyncTimer();

            Console.WriteLine($"Phase Change : {CurPhase}");

            // 특별한 페이즈 로직
            switch (newPhase)
            {
                case 1:
                case 2:
                case 3:
                    foreach (var p in _players)
                    {
                        p.Value.AcquireItem(new WardInfo()/*DataManager.ItemDict[502212] as WardInfo*/);
                        Push(p.Value.EquipItemSet, p.Value.Info.Player.CharType, CurPhase - 1);
                    }
                    break;
            }
        }

        public void SyncTimer(object state = null)
        {
            S_SyncTimer syncTimerPacket = new S_SyncTimer();

            syncTimerPacket.Phase = CurPhase;
            syncTimerPacket.CurrentTick = TimeUtil.Instance.LastTick; 
            syncTimerPacket.PhaseEndTime = _phaseEndTick; 

            Push(Broadcast, syncTimerPacket);
        }
        #endregion

        public bool TryGetMonster(int objectId, out Monster monster)
        {
            return _monsters.TryGetValue(objectId, out monster);
        }
        public CollisionManager CollManager { get { return _collisionManager; } private set { } }
        public BeaconManager BeaconManager { get { return _beaconManager; } private set { } }

        public PathfindInstance PathFind { get; set; }

        #region Score
        private int[] _teamScores = new int[3] { 40, 40, 40 }; // 1번, 2번 팀 사용

        public int ReduceScore(int team, int amount)
        {
            int oldValue, newValue;
            do
            {
                oldValue = _teamScores[team];
                newValue = Math.Max(0, oldValue - amount);
            } while (Interlocked.CompareExchange(ref _teamScores[team], newValue, oldValue) != oldValue);

            return newValue; // 감소 후 점수 반환
        }
        public int GetScore(int team) { return _teamScores[team]; }
        #endregion

        #region StatusEffect
        Dictionary<CharacterType, Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>> _statusEffects // Buffs & Debuffs
            = new Dictionary<CharacterType, Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>>();
        #endregion

        public override void Init()
        {
            PathFind = new PathfindInstance(0);

            // Spawn Env
            _envManager.Init(this);

            _skillHandlers[CharacterType.Theodore] = new TheodoreSkillHandler();

            _collisionManager.Init();

            // Skill Register
            SkillRegistry.InitRegister();
            SetUpStatusEffectDict(); // StatusEffectDict 초기화 

            // Spawn Register
            SpawnRegister();

            StartPhase();
        }

        public override void Update()
        {
            if (_phaseEndTick > 0 && TimeUtil.Instance.LastTick >= _phaseEndTick)
                ChangePhase(CurPhase + 1);

            Flush();

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

            //Flush();

            _collisionManager.CurTick = TimeUtil.Instance.LastTick;
            _collisionManager.Flush();
            _collisionManager.CheckAllCollisions(_teams, _monsters, _projectiles);
            _collisionManager.Update();

            _beaconManager.Update(this);

            BroadcastVisibleObjs();
            CheckLastPing();
        }
       
        public void EnterGame(GameObject gameObject, int team)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.TryAdd(gameObject.Id, player);
                player.Info.Player.Team = team;
                player.Info.Player.Weapon = FindWeapon(player.Info.Player.CharType);
                player.WeaponAttackRange = DataManager.WeaponDict[player.Info.Player.Weapon].Range;

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
                    if (Spawn == null)
                        SpawnRegister();
                    player.Info.PosInfo = Spawn.GetSpawnPoint(player.Team).ToPositionInfo();

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
                    player.SendChangeAttackRangePacket();
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

                if(projectile.Info.Projectile == null)
                    projectile.Info.Projectile = new ProjectileInfo();

                projectile.Info.Projectile.ProjectileType = projectile.ProjectileType;
                projectile.Info.Projectile.OwnerId = projectile.Owner?.Id ?? -1;
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
                    {
                        p.Session.Send(spawnPacket);
                    }

                    //p.SendItemStat();
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
                ObjectManager.Instance.Remove(player.Id);

                player.Room = null;
                player.OnDestroy();

                // 본인한테 정보 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }

                if (_players.IsEmpty)
                    RoomManager.Instance.Remove(RoomId);
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = null;
                if (_monsters.Remove(objectId, out monster) == false)
                    return;

                ObjectManager.Instance.Remove(objectId);
                monster.Room = null;
                _monsterManager.Add(-1, this);
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
        public void HandleAmplificationSkill(Player player, S_Interact skillPacket)
        {
            if (_skillHandlers.TryGetValue(player.Info.Player.CharType, out var handler))
                handler.CanUse(player, skillPacket);
        }

        public void AttackSkillTarget(Player player, GameObject target, KeyCode keyCode) // 타게팅 스킬. 대상 1명.
        {
            if (player == null)
                return;

            float damage = 0f;

            if (target.ObjectType == GameObjectType.Player)
                damage = _collisionManager.CalcDamage(player, target as Player, keyCode);
            else
                damage = _collisionManager.CalcDamage(player, target.Stat, keyCode);

            if (player.Info.Player.CharType == CharacterType.Abigail && keyCode == KeyCode.E)
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

        #region StatusEffect
        public void AddStatusEffect(Player player, GameObject target, KeyCode keyCode, string allowedCondition) // 타게팅 스킬 StatusEffect 적용
        {
            List<StatusEffect> statusEffectList = GetStatusEffectList(player.Info.Player.CharType, keyCode, player.GetSkillLevel(keyCode));

            foreach (var effect in statusEffectList)
            {
                if (effect.condition != allowedCondition)
                    continue;

                effect.attacker = player;

                switch (effect.subject)
                {
                    case Subject.Self:
                        Push(player.AddStatusEffect, effect);
                        break;
                    case Subject.Ally:
                        break;
                    case Subject.Enemy:
                        Push(target.AddStatusEffect, effect);
                        break;
                }                
            }
        }

        void SetUpStatusEffectDict()
        {
            foreach (var nestedKvp in DataManager.SkillDict)
            {
                foreach (var kvp in nestedKvp.Value)
                {
                    foreach (var levelKvp in kvp.Value.levels)
                    {
                        if (levelKvp.Value.effects == null)
                            continue;

                        foreach (EffectData effectData in levelKvp.Value.effects)
                        {
                            CharacterType charType = nestedKvp.Key;
                            KeyCode keyCode = kvp.Key;
                            int level = levelKvp.Key;

                            if (!_statusEffects.TryGetValue(charType, out var skillDict))
                                _statusEffects[charType] = skillDict = new Dictionary<KeyCode, Dictionary<int, List<StatusEffect>>>();

                            if (!skillDict.TryGetValue(keyCode, out var levelDict))
                                skillDict[keyCode] = levelDict = new Dictionary<int, List<StatusEffect>>();

                            if (!levelDict.TryGetValue(level, out var effects))
                                levelDict[level] = effects = new List<StatusEffect>();

                            StatusEffect newEffect = new StatusEffect
                            {
                                type = effectData.type,
                                stat = effectData.stat,
                                duration = effectData.duration,
                                value = effectData.value,
                                subject = Enum.TryParse(effectData.subject, true, out Subject temp) ? temp : Subject.Subject_None,
                                valueType = Enum.TryParse(effectData.valueType, true, out ValueType type) ? type : ValueType.ValueType_None,
                                coeff = effectData.coeff,
                                ratioPerTarget = effectData.ratioPerTarget,
                                maxRatio = effectData.maxRatio,
                                condition = effectData.condition
                            };

                            effects.Add(newEffect);
                        }
                    }
                }
            }
        }

        List<StatusEffect> GetStatusEffectList(CharacterType charType, KeyCode keyCode, int skillLevel)
        {
            if (!_statusEffects.TryGetValue(charType, out var keyDict))
                return null;

            if (!keyDict.TryGetValue(keyCode, out var skillData))
                return null;

            if (!skillData.TryGetValue(skillLevel, out var statusEffectsList))
                return null;

            return statusEffectsList;
        }
        #endregion

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

            //player.PosInfo.PosX = movePacket.PosInfo.PosX;
            //player.PosInfo.PosY = movePacket.PosInfo.PosY;
            //player.PosInfo.PosZ = movePacket.PosInfo.PosZ;

            player.PosInfo.SetPosInfoFromVector3(movePacket.PosInfo.ToVector());
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

            //player.Skill.SetCooldown(KeyCode.R, 0f);
            //StunStateDesc desc = new StunStateDesc();
            //desc.Duration = 5;
            ////desc.Speed = 17;
            //desc.EndPos = Vector3.Zero;
            //player.ChangeState(new Player_StunState(desc));
            player.Hp += 100f;
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

        private void AddVisibleObjects<T>(List<int> visibleObjs, ConcurrentDictionary<int, T> dict, Player player) where T : GameObject
        {
            foreach (var pair in dict)
            {
                GameObject go = pair.Value;
                if (go.Id == player.Id)
                    continue;

                if (go.IsVisionShare() || (go is Player p && p.Info.Player.Team == player.Info.Player.Team))
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
        public Projectile FindProjectile(Creature owner, ProjectileType type = ProjectileType.ProjectileNone)
        {
            foreach (Projectile projectile in _projectiles.Values)
            {
                if (projectile.Owner == owner)
                {
                    if(type != ProjectileType.ProjectileNone && type != projectile.ProjectileType)
                        continue;

                    return projectile;
                }
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
                if (kvp.Key == id || kvp.Value.IsUntargetable())
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

        public void CallOnCollision<T>(Player player, List<T> hitTargets, StatusEffect effect) where T : GameObject, new()
        {
            if (player.CurrentState is Player_SkillState skillState)
                skillState.Handler.OnCollision(player, hitTargets, effect);
        }
        public void CallOnCollision<T>(Player player, T nearTarget, StatusEffect effect) where T : GameObject, new()
        {
            if (player.CurrentState is Player_SkillState skillState)
                skillState.Handler.OnCollision(player, nearTarget, effect);
        }

        public void HandleOperate(Player player, Beacon beacon, float posX, float posZ)
        {
            player.Beacon = beacon;

            bool canOperate = BeaconManager.IsOperatable(player.Info.Player.Team, beacon);
            bool canOccupy = BeaconManager.IsOccupiable(player.Info.Player.Team, beacon);
            bool inRange = BeaconManager.IsInRange(player.Position, beacon);

            if (canOperate && canOccupy && inRange) // 점령 가능 && 사거리 내 -> 점령
            {
                player.ChangeState(new Player_OperateState());
                return;
            }

            var move = new C_Move
            {
                IsTargetOn = false,
                TargetPosition = new PositionInfo
                {
                    PosX = posX,
                    PosY = 0,
                    PosZ = posZ
                }
            };

            player.ChangeState(new Player_MovingState(move));
            player.SendMoveSyncPacket(move.TargetPosition);

            if (canOccupy) // 점령 가능하지만 사거리 밖이면 이동종료됐을 때의 상태 예약
                player.ReservedState = new Player_OperateState();
        }

        public void HandlerChat(Player player, C_Chat chatPkt)
        {
            if (string.IsNullOrWhiteSpace(chatPkt.Message))
                return;

            S_Chat sendPkt = new S_Chat()
            {
                ObjectId = player.Id,
                Message = chatPkt.Message
            };
            Push(Broadcast, sendPkt);
        }

        public void HandleUseItem(Player player, C_UseItem packet)
        {
            player.UseItem(packet.InventoryIndex, new Vector3(packet.MouseX, 0, packet.MouseZ));
        }

        private void SpawnRegister()
        {
            SpawnRegistry = new SpawnPointRegistry(spawnCooldownSec: 5.0);

            // JSON 로드해서 스폰 포인트 채우기
            SpawnPointLoader.LoadSpawnPoints("Data/json/SpawnPoints.json", SpawnRegistry);

            Spawn = new SpawnSystem(SpawnRegistry);
            Teleport = new TeleportSystem(SpawnRegistry);

        }
    }
}
