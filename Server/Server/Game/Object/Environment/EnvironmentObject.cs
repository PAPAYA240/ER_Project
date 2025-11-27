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
    }
}
