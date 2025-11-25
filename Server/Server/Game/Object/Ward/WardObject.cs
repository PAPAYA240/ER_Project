using Server.Game;

public class WardObject : GameObject
{
    public int TeamIndex;

    public WardObject()
    {
        ObjectType = Google.Protobuf.Protocol.GameObjectType.Ward;
    }
}

