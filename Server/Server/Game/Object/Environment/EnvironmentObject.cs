using Google.Protobuf.Protocol;
using Server.Game.Object.Monster.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public interface IInteractionStrategy
    {
        void Interact(GameObject user);
    }

    public class EnvironmentObject : GameObject
    {
        private IInteractionStrategy _interactionStrategy;

        public EnvironmentObject()
        {
            ObjectType = GameObjectType.Environment;
        }

        public void Init(EnvType envType)
        {
            // 오브젝트 타입에 따라 다른 전략을 할당
            switch (envType)
            {
                case EnvType.HillObj:
                    _interactionStrategy = new InteractableObject();
                    break;
                case EnvType.Turret:
                    _interactionStrategy = new InteractableObject();
                    break;
            }
        }
        public void OnInteract(GameObject user)
        {
            _interactionStrategy?.Interact(user);
        }
    }
}
