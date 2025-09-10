using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogOfWarVision : MonoBehaviour
{
    public int _rayCount = 200;          // 레이 개수 (360도를 나눌 개수)
    public float _viewDistance = 12f;     // 시야 거리
    public LayerMask _obstacleMask;      // 벽/장애물 레이어

    Mesh _mesh;
    Vector3 _origin;

    void Start()
    {
        _obstacleMask = LayerMask.GetMask("Map");
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _origin = transform.position;
        Material _mat = GetComponent<MeshRenderer>().material;

        Color whiteTransparent = new Color(1f, 1f, 1f, 1f); 
        _mat.color = whiteTransparent;
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

        vertices.Add(Vector3.zero); // 중심점(플레이어 위치)

        for (int i = 0; i <= _rayCount; i++)
        {
            Vector3 dir = DirFromAngle(angle);
            Vector3 vertex;

            // Raycast로 시야 차단 체크
            //nMA.Raycast(/*내위치, 타겟위치,아웃 히트, 네비메시점 올 에리어*/);
            if (NavMesh.Raycast(_origin, _origin + dir * _viewDistance, out NavMeshHit hit, NavMesh.AllAreas))
                vertex = hit.position;
            else
                vertex = _origin + dir * _viewDistance;

            // 로컬 좌표 변환
            vertices.Add(transform.InverseTransformPoint(vertex));

            if (i > 0)
            {
                // 삼각형(플레이어, 이전점, 현재점)
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
