using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCameraController : MonoBehaviour
{
    public RenderTexture FogTexture = null;

    private void Awake()
    {
        int width = 2006;
        int height = 2008;

        // RenderTextureFormat과 DepthStencilFormat은 프로젝트와 URP 설정에 맞게 지정합니다.
        RenderTextureFormat colorFormat = RenderTextureFormat.R8; // R8_UNORM과 가장 유사한 Unity 포맷
        int depthBufferBits = 0;  // DepthStencilFormat이 따로 필요 없는 경우 0

        FogTexture = new RenderTexture(width, height, depthBufferBits, colorFormat);

        // 추가 설정
        FogTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        FogTexture.antiAliasing = 1;  // None
        FogTexture.enableRandomWrite = false;
        FogTexture.useMipMap = false;
        FogTexture.wrapMode = TextureWrapMode.Clamp;
        FogTexture.filterMode = FilterMode.Bilinear;

        // RenderTexture를 생성 및 사용할 준비 완료
        FogTexture.Create();

        GetComponent<Camera>().targetTexture = FogTexture;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (FogTexture != null)
        {
            FogTexture.Release();
            Destroy(FogTexture);
            FogTexture = null;
        }
    }
}
