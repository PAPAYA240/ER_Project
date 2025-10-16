using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class InteractionManager
    {
        private Dictionary<string, Action<Hitbox, Hitbox>> _interactionDispatchers;

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
    }
}
