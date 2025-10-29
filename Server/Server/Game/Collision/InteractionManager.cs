using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class InteractionManager
    {
        private Dictionary<string, Action<Hitbox, Hitbox>> _interactionDispatchers;
        private Dictionary<string, Action<Hitbox, GameObject>> _interactionObjDispatchers;

        public InteractionManager() => Initialize();
        public void Initialize()
        {
            // 충돌체끼리 부딪혔을 때,
            _interactionDispatchers = new Dictionary<string, Action<Hitbox, Hitbox>>{
                 {
                    // 필요한 조건이 있다면 추가
                    "Attack", (interactor, target) =>
                    {
                        S_Interact packet = new S_Interact()
                        {
                            ObjectId = interactor.Creature.Id,
                            KeyCode = (int)interactor.KeyCode,
                            TargetKeyCode = (int)target.KeyCode
                        };

                        Player player = interactor.Creature as Player;
                        if(player == null) return;

                        ClientSession session = player.Session;
                        GameRoom room = interactor.Creature.Room;

                        GameObject hitTarget = ObjectManager.Instance.Find(interactor.Creature.Id);
                        if (room != null && session != null)
                            room.Push(session.Send, packet);
                    }
                 }
             };

            // 어떤 충돌체와 어떤 오브젝트가 충돌했는 지 알아야 할 때 사용
            // TODO : F1을 임시로 만들어놨으니까 나중에 바꿀 것
            _interactionObjDispatchers = new Dictionary<string, Action<Hitbox, GameObject>>{
                 {
                    "Interaction", (interactor, targetObj) =>
                    {
                        S_Interact packet = new S_Interact()
                        {
                            ObjectId = interactor.Creature.Id, // 충돌체 주인
                            KeyCode = (int)interactor.KeyCode, // 충돌체 키코드
                            TargetKeyCode = (int)KeyCode.F1,
                            TargetId = targetObj.Id, // 맞은 놈 아이디
                        };

                        Player player = interactor.Creature as Player;
                        if(player == null) return;

                        ClientSession session = player.Session;
                        GameRoom room = interactor.Creature.Room;

                        GameObject hitTarget = ObjectManager.Instance.Find(interactor.Creature.Id);
                        if (room != null && session != null)
                            room.Push(session.Send, packet);
                    }
                 },
                {
                    "Knockback", (interactorHitbox, targetObj) =>
                     {
                         //if (interactorHitbox.Creature is Monster caster)
                         //    caster.OnSkillHit(targetObj);
                     }
                }
             };
        }

        public void HandleInteraction(Hitbox myHitbox, Hitbox targetHitbox)
        {
            if (myHitbox.Creature == null || myHitbox.Creature != targetHitbox.Creature)
                return;

            Dictionary<KeyCode, List<string>> interactions = myHitbox.Interactions;
            if (interactions == null)
                return;

            if (interactions.TryGetValue(targetHitbox.KeyCode, out List<string> interactionNames))
            {
                foreach (string name in interactionNames)
                {
                    if (_interactionDispatchers.TryGetValue(name, out var action))
                        action(myHitbox, targetHitbox);
                }
            }
        }

        public void HandleInteraction(Hitbox myHitbox, GameObject targetObj)
        {
            Dictionary<KeyCode, List<string>> interactions = myHitbox.Interactions;
            if (interactions == null)
                return;

            if (interactions.TryGetValue(KeyCode.F1, out List<string> interactionNames))
            {
                foreach (string name in interactionNames)
                {
                    if (_interactionObjDispatchers.TryGetValue(name, out var action))
                        action(myHitbox, targetObj);
                }
            }
        }
    }
}
