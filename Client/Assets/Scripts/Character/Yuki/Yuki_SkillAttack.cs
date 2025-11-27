using UnityEngine;

public class Yuki_SkillAttack : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem ps;

    private void Awake()
    {
        // 프리팹 내부 파티클 자동 탐색
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
    }

    // 외부(스킬 코드/애니메이션)에서 호출할 함수
    public void PlayEffect()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }
}
