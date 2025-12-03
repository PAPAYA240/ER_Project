using UnityEngine;

public class Fx_YukiEffect : MonoBehaviour, IEffect
{
    private ParticleSystem ps;

    private Material particleMaterial; // ��ƼŬ �ý��ۿ� ���Ǵ� ��Ƽ����

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
            // ��ƼŬ �ý����� ��� ���� ���� _EffectTime�� ������Ʈ
            particleMaterial.SetFloat("_EffectTime", ps.time);
        }
        // ��ƼŬ �ý����� ���߸� _EffectTime�� ������Ʈ���� �����Ƿ�, �޽� �ִϸ��̼ǵ� ����ϴ�.
        // ��� ���� �� targetParticleSystem.time�� 0���� �����ϹǷ� �޽��� �ʱ�ȭ�˴ϴ�.
    }
}
