using Google.Protobuf.Protocol;
using Server.Game;
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
            _interactionDispatchers = new Dictionary<string, Action<Hitbox, Hitbox>>{
                 {
                    "Interaction", (interactor, target) =>
                    {
                        S_Interact packet = new S_Interact()
                        {
                            ObjectId = interactor.Player.Id,
                            KeyCode = (int)interactor.KeyCode,
                            TargetKeyCode = (int)target.KeyCode
                        };
                    }
                 }
             };
        }

        public void HandleInteraction(Hitbox myHitbox, Hitbox targetHitbox)
        {
            if (myHitbox.Player == null || myHitbox.Player != targetHitbox.Player)
                return;

            Dictionary<KeyCode, List<string>> interactions = myHitbox.Interactions;
            if (interactions == null)
                return;

            if (interactions.TryGetValue(targetHitbox.KeyCode, out List<string> interactionNames))
            {
                foreach (string name in interactionNames)
                {
                    if (_interactionDispatchers.TryGetValue(name, out var action))
                        action(myHitbox, myHitbox);
                }
            }
        }
    }
}
