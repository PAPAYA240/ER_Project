using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
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

        bool _teamToggle = false;
        bool _dummyAdded = false;

        #region Phase, Time

        public float NextPhaseTime { get; private set; }
        int _curPhase;

        public int CurPhase 
        { 
            get { return _curPhase; }
            set { _curPhase = value; }
        }

        #endregion

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
                player.Init();
                player.Info.Player.Team = AssignTeam();

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

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if(player == null) 
                return;

            ObjectInfo info = player.Info;
            S_Skill skill = new S_Skill() { SkillInfo = new SkillInfo() };

            KeyCode keyCode = (KeyCode)skillPacket.SkillInfo.KeyCode;
            float SkillDuration = skillPacket.ChargeRatio;

            // 스킬 사용이 불가능하면 바로 실패 패킷 전송
            if (!player.CanUseSkill(keyCode))
            {
                skill.CanUse = false;
                player.Session.Send(skill);
                return; 
            }
            // 스킬 사용이 가능하면 자원 소모f
            else
            {
                player.CommitSkillUsage(keyCode);
            }

            // TODO : (임시) 몬스터 찾아주기, 공격 범위에 나간다면 target 은 null로 전달해야 함
            if(TryGetMonster(skillPacket.TargetId, out Monster target))
            {
                player.Target = target;
                player.SkillTarget = target;
                player.UsedTargetingSkill = keyCode;

            }
            else if(_players.TryGetValue(skillPacket.TargetId, out Player skillTarget))
            {
                player.SkillTarget = skillTarget;
                player.UsedTargetingSkill = keyCode;
            }

            // 스킬 사용이 가능하다 판단되면 패킷 전송
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
            player.Session.Send(skill);

            SkillData skillData = null;
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[info.Player.CharType];

            if (skills.TryGetValue((KeyCode)skillPacket.SkillInfo.KeyCode, out skillData) == false)
                return;

            _collisionManager.AddHitbox(player, info.Player.CharType, (KeyCode)skillPacket.SkillInfo.KeyCode, 
                new Vector2(skillPacket.MousePosX, skillPacket.MousePosZ));
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
    }
}
