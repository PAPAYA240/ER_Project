using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.Monster;
using Server.Game.Object.Monster.AStar;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class GameRoom : Room
    {
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

        MonsterManager _monsterManager = new MonsterManager();

        bool _teamToggle = false;

        public bool TryGetMonster(int objectId, out Monster monster)
        {
            return _monsters.TryGetValue(objectId, out monster);
        }
        public void Init(int mapId)
        {
            Pathfinding.Initialize();

            // Spawn Monster
            _monsterManager.Init(this);
           // _monsterManager.Add(1, MonsterType.Gamma);
        }

        public override void Update()
        {
            foreach (Monster monster in _monsters.Values)
            {
                monster.Update();
            }

            foreach (Projectile projectile in _projectiles.Values)
            {
                projectile.Update();
            }

            foreach(Player player in _players.Values)
            {
                List<int> visibleObjs = new List<int>();
                visibleObjs.AddRange(GetObjectsInRange(_players, player));
                visibleObjs.AddRange(GetObjectsInRange(_monsters, player));
                visibleObjs.AddRange(GetObjectsInRange(_projectiles, player));
                player.SendVisibleObjsPkt(visibleObjs);
            }

            Flush();

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
                player.Room = this;
                player.Info.Team = AssignTeam();

                // 본인한테 정보 전송
                {
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

                    foreach(Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);

                    player.Session.Send(spawnPacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                if (_monsters == null)
                    return;
                _monsters.Add(gameObject.Id, monster); 
                monster.Room = this;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;
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
            if(player == null) 
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

            //switch (skillData.type)
            //{
            //    case SkillType.SkillAuto:
            //        {
            //            //// 데미지 판정
            //            //Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
            //            //GameObject target = Map.Find(skillPos);
            //            //if (target != null)
            //            //{
            //            //    Console.WriteLine("Hit GameObject!");
            //            //}
            //        }
            //        break;
            //    case SkillType.SkillProjectile:
            //        {
            //            Arrow arrow = ObjectManager.Instance.Add<Arrow>();
            //            if (arrow == null)
            //                return;

            //            arrow.Owner = player;
            //            arrow.Data = skillData;
            //            arrow.PosInfo.State = CreatureState.Moving;
            //            arrow.PosInfo.PosX = player.PosInfo.PosX;
            //            arrow.PosInfo.PosY = player.PosInfo.PosY;
            //            arrow.Speed = skillData.projectile.speed;
            //            Push(EnterGame, arrow);
            //        }
            //        break;
            //}
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
    }
}
