using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCameraController : MonoBehaviour
{
    public RenderTexture FogTexture = null;
    public Camera _fogOfWarCamera; // Vision Map을 렌더링하는 카메라
    public float _fogDarkness = 0.5f; // 어두워지는 정도 (0 = 검정, 1 = 보통)

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

        _fogOfWarCamera = GetComponent<Camera>();
        _fogOfWarCamera.targetTexture = FogTexture;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (_fogOfWarCamera == null || FogTexture == null) return;

        // Vision Map 카메라의 중심을 플레이어 위치에 맞춰 업데이트
        // (플레이어 움직임에 따라 Vision Map도 움직이도록)
        //if (FindObjectOfType<FogOfWarVision>() != null) // 예시로 플레이어 오브젝트를 찾음
        //{
        //    Vector3 playerPos = FindObjectOfType<FogOfWarVision>().transform.position; // 플레이어 위치 (_origin)
        //    _fogOfWarCamera.transform.position = new Vector3(playerPos.x, _fogOfWarCamera.transform.position.y, playerPos.z);
        //}


        // 모든 FogOfWarLit 셰이더에 비전 맵과 카메라 정보 전달
        Shader.SetGlobalTexture("_VisionMapTex", FogTexture);
        Shader.SetGlobalFloat("_FogDarkness", _fogDarkness);
        Shader.SetGlobalVector("_FogMapCenter", _fogOfWarCamera.transform.position);
        Shader.SetGlobalFloat("_FogMapScale", _fogOfWarCamera.orthographicSize * 1.4125f);
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
