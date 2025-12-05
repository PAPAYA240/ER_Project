using UnityEngine;

public class FX_Abigail : MonoBehaviour, IEffect
{
    private ParticleSystem[] systems;
    private ParticleSystemRenderer[] renderers;
    private MaterialPropertyBlock mpb;
    private int effectTimeID;
    private float startTime;

    void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        mpb = new MaterialPropertyBlock();
        effectTimeID = Shader.PropertyToID("_EffectTime");
        startTime = Time.time;
        foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Play()
    {
        gameObject.SetActive(true);
        startTime = Time.time;
        foreach (var ps in systems) ps.Play();
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(effectTimeID, 0f);
            rend.SetPropertyBlock(mpb);
        }
    }

    void Update()
    {
        float elapsed = Time.time - startTime;
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(effectTimeID, elapsed);
            rend.SetPropertyBlock(mpb);
        }
        Debug.Log(elapsed);
    }

    public void Stop()
    {
        foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(effectTimeID, 0f);
            rend.SetPropertyBlock(mpb);
        }
        gameObject.SetActive(false);
    }
}