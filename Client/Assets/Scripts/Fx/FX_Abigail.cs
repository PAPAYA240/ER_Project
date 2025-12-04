using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class FX_Abigail : MonoBehaviour, IEffect
{
    public Renderer targetRenderer; // Inspector에 Quad의 Renderer 할당
    public Material baseMaterial;   // 프로젝트에 저장된 Abigail/W_Inner 머티리얼 (선택사항)
    Material instMaterial;
    float startTime;

    void Awake()
    {
        if (baseMaterial != null)
        {
            instMaterial = Instantiate(baseMaterial);
            if (targetRenderer != null) targetRenderer.material = instMaterial;
        }
        else if (targetRenderer != null)
        {
            instMaterial = targetRenderer.material; // Unity가 자동으로 인스턴스 반환
        }
    }

    public void Play()
    {
        gameObject.SetActive(true);
        startTime = Time.time;
        targetRenderer.material.SetFloat("_EffectTime", 0f);
    }

    void Update()
    {
        float elapsed = Time.time - startTime;
        if (targetRenderer != null) 
            targetRenderer.material.SetFloat("_EffectTime", elapsed);
    }

    public void Stop()
    {
        gameObject.SetActive(false);
    }
}
