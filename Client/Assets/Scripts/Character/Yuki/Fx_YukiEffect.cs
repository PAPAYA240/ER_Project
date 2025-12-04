using UnityEngine;

public class Fx_YukiEffect : MonoBehaviour, IEffect
{
    private ParticleSystem ps;

    private Material particleMaterial; // 파티클 시스템에 사용되는 머티리얼

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"ParticleSystem not found on {gameObject.name}");
        }
        else
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        particleMaterial = ps.GetComponent<Renderer>().material;
    }

    public void Play()
    {
        gameObject.SetActive(true);
        ps?.Play();
    }

    public void Stop()
    {
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }



    void Update()
    {
        if (ps != null && particleMaterial != null && ps.isPlaying)
        {
            // 파티클 시스템이 재생 중일 때만 _EffectTime을 업데이트
            particleMaterial.SetFloat("_EffectTime", ps.time);
        }
        // 파티클 시스템이 멈추면 _EffectTime을 업데이트하지 않으므로, 펄스 애니메이션도 멈춥니다.
        // 재생 시작 시 targetParticleSystem.time은 0부터 시작하므로 펄스도 초기화됩니다.
    }
}
