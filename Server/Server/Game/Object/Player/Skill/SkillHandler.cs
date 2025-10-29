using Google.Protobuf.Protocol;


namespace Server.Game
{
    public interface ISkillHandler
    {
        // 스킬 사용이 가능한지 확인
        bool CanUse(Player player, S_Skill skillPacket);
    }
    public abstract class SkillHandler : ISkillHandler
    {
        public abstract bool CanUse(Player player, S_Skill skillPacket);
    }
}
