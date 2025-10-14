using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public interface ISkillHandler
{
    void OnEnter(Player p, SkillSpec spec, SkillContext ctx);   // 선딜
    void OnHit(Player p, SkillSpec spec, SkillContext ctx);     // 히트
    void OnExit(Player p, SkillSpec spec, SkillContext ctx);    // 후딜 종료
}
