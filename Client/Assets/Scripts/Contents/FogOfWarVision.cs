using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogOfWarVision : MonoBehaviour
{
    public int _rayCount = 200;          // ���� ���� (360���� ���� ����)
    public float _viewDistance = 12f;     // �þ� �Ÿ�
    public LayerMask _obstacleMask;      // ��/��ֹ� ���̾�

    Mesh _mesh;
    Vector3 _origin;

    private MeshRenderer _meshRenderer;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        //SetVisionVisible(false);

        _obstacleMask = LayerMask.GetMask("Map");
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _origin = transform.position;
        Material _mat = GetComponent<MeshRenderer>().material;

        Color whiteTransparent = new Color(1f, 1f, 1f, 1f); 
        _mat.color = whiteTransparent;
    }
    public void SetVisionVisible(bool isVisible)
    {
        // TODO : (ny) 카메라를 바꿨는데 끄는 방법을 몰라서 일단 추가했는데 문제 생기면 알려주세요 ;^;
        if (_meshRenderer != null)
            _meshRenderer.enabled = isVisible;
    }

    void LateUpdate()
    {
        _origin = transform.position;
        GenerateVisionMesh();
    }

    void GenerateVisionMesh()
    {
        float angleIncrement = 360f / _rayCount;
        float angle = 0f;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero); // �߽���(�÷��̾� ��ġ)

        for (int i = 0; i <= _rayCount; i++)
        {
            Vector3 dir = DirFromAngle(angle);
            Vector3 vertex;

            // Raycast�� �þ� ���� üũ
            //nMA.Raycast(/*����ġ, Ÿ����ġ,�ƿ� ��Ʈ, �׺�޽��� �� ������*/);
            if (NavMesh.Raycast(_origin, _origin + dir * _viewDistance, out NavMeshHit hit, NavMesh.AllAreas))
                vertex = hit.position;
            else
                vertex = _origin + dir * _viewDistance;

            // ���� ��ǥ ��ȯ
            vertices.Add(transform.InverseTransformPoint(vertex));

            if (i > 0)
            {
                // �ﰢ��(�÷��̾�, ������, ������)
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            angle -= angleIncrement;
        }

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
    }

    Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
    }
}
