using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogOfWarVision : MonoBehaviour
{
    public int _rayCount = 200;          // 레이 개수
    private float _viewDistance = 8.5f;     // 시야 거리
    public float ViewDistance { get { return _viewDistance; } set { _viewDistance = value; } }
    public LayerMask _obstacleMask;      // 구조물 마스크

    Mesh _mesh;
    Vector3 _origin;

    private MeshRenderer _meshRenderer;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();

        _obstacleMask = LayerMask.GetMask("Map");
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _origin = transform.position;
        GetComponent<MeshRenderer>().material = Managers.Resource.Load<Material>("Material/FogMesh");

        //Color whiteTransparent = new Color(1f, 1f, 1f, 1f); 
        //_mat.color = whiteTransparent;
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

        vertices.Add(Vector3.zero); // 원점 추가

        for (int i = 0; i <= _rayCount; i++)
        {
            Vector3 dir = DirFromAngle(angle);
            Vector3 vertex;

            if (NavMesh.Raycast(_origin, _origin + dir * _viewDistance, out NavMeshHit hit, NavMesh.AllAreas))
                vertex = hit.position;
            else
                vertex = _origin + dir * _viewDistance;

            // 레이캐스팅 후 나온 위치에 정점 추가
            vertices.Add(transform.InverseTransformPoint(vertex));

            if (i > 0)
            {
                // 삼각형 추가
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
