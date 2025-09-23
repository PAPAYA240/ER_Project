using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object;
using Server.Game.Object.Monster;
using Server.Game.Object.Monster.AStar;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class GameRoom : Room
    {
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        ConcurrentDictionary<int, EnvironmentObj> _envs = new ConcurrentDictionary<int, EnvironmentObj>();
        ConcurrentDictionary<int, Monster> _monsters = new ConcurrentDictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

        MonsterManager _monsterManager = new MonsterManager();
        CollisionManager _collisionManager = new CollisionManager();
        EnvManager _envManager = new EnvManager();

        Dictionary<int, Dictionary<int, Player>> _teams = new Dictionary<int, Dictionary<int, Player>>();

        bool _teamToggle = false;

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
            _monsterManager.Add(1, MonsterType.Gamma);

            // Spawn Env
            _envManager.Init(this);
        }

        public override void Update()
        {
            foreach (Projectile projectile in _projectiles.Values)
            {
                projectile.Update();
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

            foreach (Player player in _players.Values)
            {
                int levelUpCnt = player.CheckLevelUp();
                if(levelUpCnt > 0)
                    BroadcastLevelUp(player.Id, levelUpCnt, player.Info.CharType);
            }

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
                player.Info.Team = AssignTeam();

                if (!_teams.TryGetValue(player.Info.Team, out var teamPlayers))
                {
                    teamPlayers = new Dictionary<int, Player>(); 
                    _teams[player.Info.Team] = teamPlayers;
                }
                teamPlayers.Add(player.Id, player);

                ObjectManager.Instance.RegisterTeam(gameObject.Id, player.Info.Team);

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

                    foreach (EnvironmentObj env in _envs.Values)
                        spawnPacket.Objects.Add(env.Info);

                    player.Session.Send(spawnPacket);
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
                EnvironmentObj env = gameObject as EnvironmentObj;
                if (env == null)
                    _envs = new ConcurrentDictionary<int, EnvironmentObj>();

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
                var myTeam = _teams[player.Info.Team];
                myTeam.Remove(player.Id);

                player.Room = null;

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

            // TODO : 스킬 사용 가능 여부 체크
            if (!player.CanUseSkill(skillPacket))
            {
                skill.CanUse = false;
                Broadcast(skill);
                return; 
            }

            // TODO : (임시) 몬스터 찾아주기, 공격 범위에 나간다면 target 은 null로 전달해야 함
            TryGetMonster(skillPacket.TargetId, out Monster target);
            player.Target = target;

            // 스킬 매니저에 정보를 전달해서 체크
            // 쿨타임, 스테미나 등 체크


            // 스킬 사용이 가능하다 판단되면 패킷 전송
            info.PosInfo.State = CreatureState.Skill;
            skill.CanUse = true;
            skill.ObjectId = info.ObjectId;
            skill.SkillInfo = skillPacket.SkillInfo;
            Broadcast(skill);

            SkillData skillData = null;
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[info.CharType];

            if (skills.TryGetValue((KeyCode)skillPacket.SkillInfo.KeyCode, out skillData) == false)
                return;

            _collisionManager.AddHitbox(player, info.CharType, (KeyCode)skillPacket.SkillInfo.KeyCode);
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

            StatInfo statInfo = DataManager.StatGrowthDict[charType];
            statInfo.MultiplyForGrowth(levelUpCnt);
            levelUpPkt.StatGrowth = statInfo;

            Broadcast(levelUpPkt);
        }
    }
}
