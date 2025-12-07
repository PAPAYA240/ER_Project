using UnityEngine;

public class FX_Abigail : MonoBehaviour, IEffect
{
    private ParticleSystem[] systems;
    private ParticleSystemRenderer[] renderers;
    private MaterialPropertyBlock mpb;
    private int startTimeID;

    void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        mpb = new MaterialPropertyBlock();
        startTimeID = Shader.PropertyToID("_StartTime");
        foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Play()
    {
        gameObject.SetActive(true);

        foreach (var ps in systems) ps.Play();
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(startTimeID, Time.time);
            rend.SetPropertyBlock(mpb);
        }
    }

    public void Stop()
    {
        foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }
}