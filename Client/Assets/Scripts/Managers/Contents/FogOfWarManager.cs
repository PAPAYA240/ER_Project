using System;
using UnityEngine;

public class FogOfWarManager 
{
    public RenderTexture _visionMapTexture;
    public Camera _fogOfWarCamera; // Vision Map을 렌더링하는 카메라
    public float _fogDarkness = 0.5f; // 어두워지는 정도 (0 = 검정, 1 = 보통)

    void Update()
    {

    }
}