using Google.Protobuf.Protocol;

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
                case EnvType.HealPack:
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
