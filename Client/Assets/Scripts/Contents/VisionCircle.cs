using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisionCircle : MonoBehaviour
{
    private float _radius = 1.5f;           // 원의 반지름
    [Range(3, 100)]
    public int _segments = 54;           // 원 둘레를 구성하는 세그먼트(분할) 수 (최소 3개)

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        GenerateCircleMesh();
    }

    void OnValidate() // 인스펙터에서 값 변경 시 메시를 업데이트
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        GenerateCircleMesh();
    }

    void GenerateCircleMesh()
    {
        Mesh mesh = new Mesh();
        _meshFilter.mesh = mesh;

        // 1. 정점(Vertices) 배열 생성
        // 중심점 + 둘레 세그먼트 수만큼의 정점
        Vector3[] vertices = new Vector3[_segments + 1];
        vertices[0] = Vector3.zero; // 원의 중심

        for (int i = 0; i <= _segments; i++)
        {
            float angle = i * 2 * Mathf.PI / _segments;
            float x = _radius * Mathf.Sin(angle);
            float z = _radius * Mathf.Cos(angle);
            vertices[i] = new Vector3(x, 0f, z); // Y축은 0으로 고정하여 평면 원형
        }

        // 2. 삼각형(Triangles) 배열 생성
        // 각 세그먼트마다 중심점과 2개의 둘레 정점으로 1개의 삼각형을 만듦
        int[] triangles = new int[_segments * 3]; // _segments * 3개의 정점 인덱스
        for (int i = 0; i < _segments; i++)
        {
            triangles[i * 3] = 0;             // 중심점 인덱스
            triangles[i * 3 + 1] = i + 1;     // 현재 둘레 정점 인덱스
            triangles[i * 3 + 2] = i + 2 > _segments ? 1 : i + 2; // 다음 둘레 정점 인덱스 (마지막은 첫 번째와 연결)
        }

        // 3. UV (텍스처 좌표) 배열 생성
        // 중심은 (0.5, 0.5), 둘레 정점들은 원형으로 매핑
        Vector2[] uv = new Vector2[vertices.Length];
        uv[0] = new Vector2(0.5f, 0.5f); // 중심점의 UV

        for (int i = 0; i <= _segments; i++)
        {
            float angle = i * 2 * Mathf.PI / _segments;
            float u = 0.5f + 0.5f * Mathf.Sin(angle);
            float v = 0.5f + 0.5f * Mathf.Cos(angle);
            uv[i] = new Vector2(u, v);
        }

        // 메시에 데이터 할당
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // 법선 벡터 재계산 (라이팅 계산에 필요)
        mesh.RecalculateNormals();
        // 바운딩 박스 재계산 (컬링 등에 사용)
        mesh.RecalculateBounds();

        // 머티리얼 설정 (선택 사항, 기본 머티리얼 할당)
        if (_meshRenderer.sharedMaterial == null)
        {
            GetComponent<MeshRenderer>().material = Managers.Resource.Load<Material>("Material/FogMesh");
            //_meshRenderer.sharedMaterial = new Material(Shader.Find("FogShader")); // URP에서는 "Universal Render Pipeline/Lit"
        }
    }

    public void SetActivate(bool isActivate)
    {
        gameObject.SetActive(isActivate);
        _meshRenderer.enabled = isActivate;
    }
}