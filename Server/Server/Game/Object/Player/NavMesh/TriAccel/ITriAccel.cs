using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public interface ITriAccel
{
    // 전처리(빌드)
    void Build(TriCache cache, Vector3 min, Vector3 max);

    // 점/반경 주변 후보 삼각형(Clamp에 사용)
    IEnumerable<int> QueryCandidatesNearPoint(Vector3 p, float radius = 0f);

    // 선분(또는 캡슐) 경로 주변 후보 삼각형(Sweep에 사용)
    IEnumerable<int> QueryCandidatesAlongSegment(Vector3 a, Vector3 b, float radius = 0f);
}
