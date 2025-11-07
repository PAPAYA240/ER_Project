using Google.Protobuf.Protocol;


namespace Server.Game
{
    public interface ISkillHandler
    {
        bool CanUse(Player player, S_Interact skillPacket); // 스킬 사용이 가능한지 확인
    }
    public abstract class SkillHandler : ISkillHandler
    {
        public abstract bool CanUse(Player player, S_Interact skillPacket);
    }
}
