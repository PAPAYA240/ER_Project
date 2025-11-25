
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System.Numerics;

public class WardInfo : ConsumableItemInfo
{
    // position, team, hp?


    public WardInfo()
    {
        ConsumableItemInfo itemInfo = DataManager.ItemDict[502212] as ConsumableItemInfo;
        Name = itemInfo.Name;
        Id = itemInfo.Id;
        Count = itemInfo.Count;
        Grade = itemInfo.Grade;
        Description = itemInfo.Description;
    }

    public override void Use(Vector3 mousePos, Player p)
    {
        // sqawn ward
        WardObject wardObject = ObjectManager.Instance.Add<WardObject>();
        wardObject.Position = mousePos;
        wardObject.TeamIndex = p.Team;
        
        S_Spawn packet = new S_Spawn();
        packet.Objects.Add(wardObject.Info);

        p.Room.Push(p.Room.Broadcast, packet);
    }
}
