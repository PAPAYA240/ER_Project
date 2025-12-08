using UnityEngine;

public class Fx_YukiEffect : MonoBehaviour, IEffect
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
            Debug.LogWarning($"ParticleSystem not found on {gameObject.name}");
        else
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Play()
    {
        ps?.Play();
    }

    public void Stop()
    {
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
