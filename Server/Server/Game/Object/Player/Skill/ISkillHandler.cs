using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public interface ISkillHandler
{
    int LastSeq { get; set; }
    SkillCollisionProposal Latest { get; set; }

    void OnEnter(Player p, SkillSpec spec, SkillContext ctx);   // 선딜
    void OnHit(Player p, SkillSpec spec, SkillContext ctx);     // 히트
    void OnExit(Player p, SkillSpec spec, SkillContext ctx);    // 후딜 종료

    void OnTick(Player p, SkillSpec spec, SkillContext ctx);    // 매틱 실행용

    void OnPropose(Player p, in SkillCollisionProposal proposal)
    {
        if (proposal.Seq <= LastSeq)
            return; 
        LastSeq = proposal.Seq;            
        Latest = proposal;              
    }

    public struct SkillCollisionProposal
    {
        public int Seq;                             // 최신만
        public Vector3 EndBlocked;                  // 벽 앞 후보(hit-skin)
        public Vector3 EndPass;                     // 통과 가정 후보
        public Vector3 BehindBlocked;               // 타겟 뒤 벽 앞 

        public int CandidateTargetId;
        public float Speed;
        //public float Radius, MaxDistance;    // 힌트(서버 스펙으로 재검증)
    }
}
