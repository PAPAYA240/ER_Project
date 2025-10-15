using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using static Lucene.Net.Index.SegmentReader;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class GameRoom : Room
    {
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        ConcurrentDictionary<int, EnvironmentObject> _envs = new ConcurrentDictionary<int, EnvironmentObject>();
        ConcurrentDictionary<int, Monster> _monsters = new ConcurrentDictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

        MonsterManager _monsterManager = new MonsterManager();
        CollisionManager _collisionManager = new CollisionManager();
        EnvManager _envManager = new EnvManager();

        Dictionary<int, Dictionary<int, Player>> _teams = new Dictionary<int, Dictionary<int, Player>>();

        string _navmeshPath = "../../../Resources/Data/NavmeshData.json"; // 배포 경로

        bool _teamToggle = false;
        bool _dummyAdded = true;    // TEMP : Dummy

        public bool TryGetMonster(int objectId, out Monster monster)
        {
            return _monsters.TryGetValue(objectId, out monster);
        }

        public void Init(int mapId)
        {
            // Spawn NavMesh
            Pathfinding.Initialize();

            // Spawn Monster
            _monsterManager.Init(this);
            //_monsterManager.Add(1, MonsterType.Gamma);

            // Spawn Env
            _envManager.Init(this);

            _collisionManager.Init();

            // NavMesh
            InitNavmeshPipeline();
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

            _collisionManager.Update();
            _collisionManager.CheckAllCollisions(_teams, _monsters, _projectiles);

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
                player.Info.Player.Team = AssignTeam();

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

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null)
                return;

            // 1) 스펙 로드(JSON DB에 맞춰 구현)
            var key = (KeyCode)skillPacket.SkillInfo.KeyCode;
            //SkillSpec spec = SkillDatabase.Resolve(player.Info.Player.CharType, key);

            // TEMP
            SkillSpec spec = new SkillSpec
            {
                AnimName = "SKILL_Q",
                Windup = 0.5f,
                Backswing = 0.5f,

                Move = new MoveSpec { Distance = 3.0f, Speed = 8.0f, },
                Collision = new CollisionSpec { StopOnWall = true, },
            };

            // 2) 컨텍스트 구성(마우스 XZ/타겟)
            var ctx = new SkillContext
            {
                MousePos = new Vector2(skillPacket.MousePosX, skillPacket.MousePosZ),
                TargetId = 0, // 필요하면 패킷에 포함
                Key = key
            };

            // 3) 핸들러 결정
            ISkillHandler handler = SkillRegistry.Resolve(player.Info.Player.CharType, key);

            // 4) SkillState로 전환
            player.ChangeState(new Player_SkillState(handler, spec, ctx));
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

        public void HandleAttackSkillTarget(Player player, C_AttackSkillTarget attackSkillTarget)
        {
            //if (player == null || player.SkillTarget == null)
            //    return;

            //float damage = 0f;

            //if(player.SkillTarget.ObjectType == GameObjectType.Player)
            //    damage = _collisionManager.CalcDamage(player, player.SkillTarget as Player, player.UsedTargetingSkill);
            //else
            //    damage = _collisionManager.CalcDamage(player, player.SkillTarget.Stat, player.UsedTargetingSkill);   

            //player.SkillTarget.OnDamaged(player, damage);
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

        public void InitNavmeshPipeline()
        {
            // 1) 클라에서 Export한 NavMesh JSON 불러오기        
            NavmeshData nav = NavmeshImporter.LoadFromJson(_navmeshPath);

            // 2) TriCache(전처리) 빌드
            TriCache triCache = TriCacheBuilder.Build(nav);   // 아래 1-1 참고

            // 3) Uniform Grid 가속구조 빌드
            var accel = new UniformGridAccel(cell: 0.5f);     // 셀 크기 튜닝 포인트
            accel.Build(triCache, nav.Min, nav.Max);

            // 4) NavmeshService에 장착
            NavmeshService.Instance.Init(triCache, accel);

            // (선택) 버전 로그/검증
            Console.WriteLine($"[Navmesh] loaded: {nav.Version}, tris={triCache.Tris.Length}");
        }
    }
}
