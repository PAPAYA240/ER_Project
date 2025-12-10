using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogOfWarVision : MonoBehaviour
{
    public int _rayCount = 360;          // 레이 개수
    private float _viewDistance = 8.5f;     // 시야 거리
    public float ViewDistance { get { return _viewDistance; } set { _viewDistance = value; } }

    Mesh _mesh;
    Vector3 _origin;

    private MeshRenderer _meshRenderer;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _origin = transform.position;
        GetComponent<MeshRenderer>().material = Managers.Resource.Load<Material>("Material/FogMesh");

        //Color whiteTransparent = new Color(1f, 1f, 1f, 1f); 
        //_mat.color = whiteTransparent;
    }

    void LateUpdate()
    {
        _origin = transform.position + Vector3.up * 1.5f;
        GenerateVisionMesh();
    }

    void GenerateVisionMesh()
    {
        float angleIncrement = 360f / _rayCount;

        // 정점 개수 증가 (더 부드럽게)
        int vertexCount = _rayCount * 2;  // 2배로 증가
        Vector3[] vertices = new Vector3[vertexCount + 1];
        int[] triangles = new int[vertexCount * 3];

        vertices[0] = Vector3.zero;

        float angle = 0f;
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 dir = DirFromAngle(angle);
            Vector3 worldHit = GetVisionPoint(dir);
            Vector3 localHit = transform.InverseTransformPoint(worldHit);
            localHit.y = 0.02f;

            vertices[i + 1] = localHit;
            angle -= angleIncrement / 2f;  // 각도를 절반으로
        }

        for (int i = 0; i < vertexCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;

            int nextIndex = (i == vertexCount - 1) ? 1 : i + 2;
            triangles[triangleIndex + 2] = nextIndex;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    Vector3 GetVisionPoint(Vector3 direction)
    {
        float maxDistance = _viewDistance;

        LayerMask blockingLayers = LayerMask.GetMask("VisionWall");

        if (Physics.Raycast(_origin, direction, out RaycastHit highHit, maxDistance, blockingLayers))
        {
            return highHit.point;
        }

        return _origin + direction * maxDistance;
    }

    Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
    }
}
