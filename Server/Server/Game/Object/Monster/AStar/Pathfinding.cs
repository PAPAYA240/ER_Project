using Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ServerCore;
using System.Linq;

namespace Server.Game
{
    // 폴리곤 노드임
    public class Node : IComparable<Node>
    {
        public int TriangleIndex; 
        public Vector3 Center;    // 삼각형의 중심점
        public List<Node> Neighbors; // 인접한 삼각형 노드들

        public float G, H; // G 시작점- 나 사이 비용, H 목적지까지 예상 비용
        public float F => G + H;
        public Node Parent;

        public Node(int triangleIndex, Vector3 center)
        {
            TriangleIndex = triangleIndex;
            Center = center;
            Neighbors = new List<Node>();
        }

        public int CompareTo(Node other)
        {
            if (F == other.F) return 0;
            return F < other.F ? -1 : 1;
        }

        public override bool Equals(object obj)
        {
            return obj is Node node && TriangleIndex == node.TriangleIndex;
        }

        public override int GetHashCode()
        {
            return TriangleIndex.GetHashCode();
        }
    }

    public static class Pathfinding
    {
        private static NavMeshExportData _navMeshData;
        private static List<Node> _triangleNodes;

        #region Load Navi
        public static void Initialize()
        {
            string basePath = ConfigManager.Config.dataPaths["monster"];
            string navMeshFilePath = Path.Combine(basePath, "MonsterData/navmesh_data.json");
            string navMeshJsonText = File.ReadAllText(navMeshFilePath);

            _navMeshData = NavMeshExportData.LoadFromJson(navMeshFilePath);
            if (_navMeshData == null)
            {
                Console.WriteLine("실패");
                return;
            }

            BuildTriangleGraph();
            Console.WriteLine($"NavMesh : {_triangleNodes.Count}");
        }

        private static void BuildTriangleGraph()
        {
            _triangleNodes = new List<Node>();

            // 1. 각 삼각형에 대한 Node 객체 생성 및 중심점 계산
            List<int> triangles = _navMeshData.triangles;
            for (int i = 0; i < triangles.Count / 3; i++)
            {
                Vector3 v0Data = _navMeshData.vertices[triangles[i * 3]];
                Vector3 v0 = new Vector3(v0Data.X, v0Data.Y, v0Data.Z);

                Vector3 v1Data = _navMeshData.vertices[triangles[i * 3 + 1]];
                Vector3 v1 = new Vector3(v1Data.X, v1Data.Y, v1Data.Z);

                Vector3 v2Data = _navMeshData.vertices[triangles[i * 3 + 2]];
                Vector3 v2 = new Vector3(v2Data.X, v2Data.Y, v2Data.Z);

                Vector3 center = (v0 + v1 + v2) / 3.0f;
                _triangleNodes.Add(new Node(i, center));
            }

            // 2. 인접한 삼각형 찾기 (간선 연결)
            BuildNeighborGraph();

            // 3. 만약 아무것도 연결되지 않은 폴리곤 존재하면 호출될 것임
            foreach (var node in _triangleNodes)
            {
                if (node.Neighbors.Count == 0)
                    Console.WriteLine($"Failed : 이것은 연결되지 않은 폴리곤 {node.TriangleIndex} .");
            }
        }
        private static void BuildNeighborGraph()
        {
            var edgeToNodeMap = new Dictionary<Tuple<Vector3, Vector3>, Node>();

            for (int i = 0; i < _triangleNodes.Count; i++)
            {
                Node currentNode = _triangleNodes[i];
                int[] triIndices = new int[] { _navMeshData.triangles[i * 3], _navMeshData.triangles[i * 3 + 1], _navMeshData.triangles[i * 3 + 2] };

                Vector3[] triVertices = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    Vector3 sv = _navMeshData.vertices[triIndices[k]];
                    triVertices[k] = new Vector3(sv.X, sv.Y, sv.Z);
                }

                for (int j = 0; j < 3; j++)
                {
                    Vector3 v1 = triVertices[j];
                    Vector3 v2 = triVertices[(j + 1) % 3];

                    Vector3 roundedV1 = new Vector3((float)Math.Round(v1.X, 4), (float)Math.Round(v1.Y, 4), (float)Math.Round(v1.Z, 4));
                    Vector3 roundedV2 = new Vector3((float)Math.Round(v2.X, 4), (float)Math.Round(v2.Y, 4), (float)Math.Round(v2.Z, 4));

                    Tuple<Vector3, Vector3> edgeKey;
                    if (roundedV1.X < roundedV2.X || (roundedV1.X == roundedV2.X && (roundedV1.Y < roundedV2.Y || (roundedV1.Y == roundedV2.Y && roundedV1.Z < roundedV2.Z))))
                    {
                        edgeKey = new Tuple<Vector3, Vector3>(roundedV1, roundedV2);
                    }
                    else
                    {
                        edgeKey = new Tuple<Vector3, Vector3>(roundedV2, roundedV1);
                    }

                    if (edgeToNodeMap.ContainsKey(edgeKey))
                    {
                        Node neighborNode = edgeToNodeMap[edgeKey];
                        currentNode.Neighbors.Add(neighborNode);
                        neighborNode.Neighbors.Add(currentNode);
                        edgeToNodeMap.Remove(edgeKey);
                    }
                    else
                    {
                        edgeToNodeMap.Add(edgeKey, currentNode);
                    }
                }
            }
        }
        #endregion

        public static Node FindNearestNode(Vector3 position)
        {
            float minDistance = float.MaxValue;
            Node nearestNode = null;

            foreach (var node in _triangleNodes)
            {
                float distance = Vector3.Distance(position, node.Center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestNode = node;
                }
            }
            return nearestNode;
        }

        public static List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            Node startNode = FindNearestNode(start);
            Node targetNode = FindNearestNode(end);
            if (startNode == null || targetNode == null)
                return null;

            foreach (var node in _triangleNodes)
                node.Parent = null; 

            var openSet = new PriorityQueue<Node, float>(); // 탐색 후보
            var gScore = new Dictionary<Node, float>(); // 출발 노드에서 각 노드까지의 실제 비용

            gScore[startNode] = 0;
            float startFScore = Vector3.Distance(startNode.Center, targetNode.Center);
            openSet.Push(startNode, startFScore);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.Pop(); // 현재 가장 비용이 낮은 노드
                float currentGScore = gScore.ContainsKey(currentNode) ? gScore[currentNode] : float.MaxValue;

                // 목표 지점과 같은가?
                if (currentNode.Equals(targetNode))
                {
                    List<Vector3> rawPath = SmoothPath(RetracePath(startNode, currentNode), start, end);
                    float epsilon = 20.0f; // Agent Radius 0.5에 비해 적절한 값으로 테스트 시작
                    List<Vector3> finalPath = PathSimplifier.DouglasPeuckerSimplify(rawPath, epsilon);
                    // 부드러운 이동
                    return finalPath;
                }

                // 현재 노드와 인접한 노드 탐색
                foreach (var neighbor in currentNode.Neighbors)
                {
                    // 새로운 예상 비용 계산 : current Node + 현재 탐색 중인 Neighbor node 비용
                    float tentativeGScore = currentGScore + Vector3.Distance(currentNode.Center, neighbor.Center);

                    // 이웃노드에 더 낮은 비용을 가지고 있다면 continue
                    //: 휴리스틱까지 구해서 계산하는 거라서 최적 거리가 보장되어 있음
                    if (gScore.TryGetValue(neighbor, out float neighborGScore) && tentativeGScore >= neighborGScore)
                        continue;

                    // 새로운 낮은 비용 갱신
                    neighbor.Parent = currentNode;
                    gScore[neighbor] = tentativeGScore;

                    // 이웃 노드에서 목표노드까지의 휴리스틱 비용 계산 (직선 거리 비용 계산)
                    float hScore = Vector3.Distance(neighbor.Center, targetNode.Center);
                    float newFScore = tentativeGScore + hScore;
                    openSet.Push(neighbor, newFScore);
                }
            }
            return null;
        }

        // 역추적
        private static List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != null && currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            path.Add(startNode);
            path.Reverse();

            return path;
        }

        // 경로 후처리 : 퍼널 알고리즘
        private static List<Vector3> SmoothPath(List<Node> nodePath, Vector3 startPos, Vector3 endPos)
        {
            List<Vector3> smoothedPath = new List<Vector3>();

            // 경로에 삼각형이 2개 미만일 경우 처리
            if (nodePath.Count <= 1)
            {
                smoothedPath.Add(startPos);
                smoothedPath.Add(endPos);
                return smoothedPath;
            }

            // 꼭짓점(apex)은 시작 위치
            Vector3 apex = startPos;
            smoothedPath.Add(apex);

            // 왼쪽/오른쪽 경계 변
            Vector3 portalLeft = apex;
            Vector3 portalRight = apex;

            int leftIndex = 0;
            int rightIndex = 0;

            for (int i = 1; i < nodePath.Count; i++)
            {
                // 이전 노드와 현재 노드의 공유된 변을 가져옴
                Tuple<Vector3, Vector3> portal = GetSharedEdge(nodePath[i - 1], nodePath[i]);
                if (portal == null)
                {
                    // 변 없으면 경로 끊어졌으니 재시작
                    smoothedPath.Add(nodePath[i].Center);
                    apex = nodePath[i].Center;
                    portalLeft = apex;
                    portalRight = apex;
                    continue;
                }

                Vector3 p1 = portal.Item1;
                Vector3 p2 = portal.Item2;
                Vector3 currentLeft, currentRight;

                // 변의 왼쪽으로 이동하는 지 오른쪽으로 이동하는 지 구분한다.
                if (Cross(new Vector2(p1.X - apex.X, p1.Z - apex.Z), new Vector2(p2.X - apex.X, p2.Z - apex.Z)) > 0)
                {
                    // 양수면 왼쪽
                    currentLeft = p1;
                    currentRight = p2;
                }
                else
                {
                    // 음수면 오른쪽
                    currentLeft = p2;
                    currentRight = p1;
                }

                // 1. 새로운 변이 왼쪽 정점이 현재 깔때기(funnel)의 왼쪽 경계를 좁히는 경우
                if (Cross(new Vector2(portalLeft.X - apex.X, portalLeft.Z - apex.Z), new Vector2(currentLeft.X - apex.X, currentLeft.Z - apex.Z)) <= 0)
                {
                    // 현재 꼭짓점에서 왼쪽 경계가 교차하는지 확인
                    if (Cross(new Vector2(currentLeft.X - apex.X, currentLeft.Z - apex.Z), new Vector2(portalRight.X - apex.X, portalRight.Z - apex.Z)) > 0)
                    {
                        // 오른쪽 경계를 뚫고 지나가므로, 오른쪽 꼭짓점을 경로에 추가
                        smoothedPath.Add(portalRight);
                        apex = portalRight;
                        portalLeft = apex;
                        portalRight = apex;
                        i = rightIndex; // 오른쪽 꼭짓점부터 다시 시작
                        continue;
                    }
                    portalLeft = currentLeft;
                    leftIndex = i;
                }

                // 2. 새로운 변이 오른쪽 정점이 현재 깔때기의 오른쪽 경계를 좁히는 경우
                if (Cross(new Vector2(portalRight.X - apex.X, portalRight.Z - apex.Z), new Vector2(currentRight.X - apex.X, currentRight.Z - apex.Z)) >= 0)
                {
                    if (Cross(new Vector2(currentRight.X - apex.X, currentRight.Z - apex.Z), new Vector2(portalLeft.X - apex.X, portalLeft.Z - apex.Z)) < 0)
                    {
                        smoothedPath.Add(portalLeft);
                        apex = portalLeft;
                        portalLeft = apex;
                        portalRight = apex;
                        i = leftIndex;
                        continue;
                    }
                    portalRight = currentRight;
                    rightIndex = i;
                }
            }

            // 마지막 목표 지점을 최종 경로에 추가
            smoothedPath.Add(endPos);
            return smoothedPath;
        }

        // 두 삼각형 간의 공유된 변 찾기.
        private static Tuple<Vector3, Vector3> GetSharedEdge(Node nodeA, Node nodeB)
        {
            List<int> triA_indices = new List<int> { _navMeshData.triangles[nodeA.TriangleIndex * 3], _navMeshData.triangles[nodeA.TriangleIndex * 3 + 1], _navMeshData.triangles[nodeA.TriangleIndex * 3 + 2] };
            List<int> triB_indices = new List<int> { _navMeshData.triangles[nodeB.TriangleIndex * 3], _navMeshData.triangles[nodeB.TriangleIndex * 3 + 1], _navMeshData.triangles[nodeB.TriangleIndex * 3 + 2] };

            List<int> commonVertexIndices = new List<int>();
            foreach (int vA_idx in triA_indices)
            {
                foreach (int vB_idx in triB_indices)
                {
                    if (vA_idx == vB_idx)
                    {
                        commonVertexIndices.Add(vA_idx);
                    }
                }
            }

            if (commonVertexIndices.Count == 2)
            {
                // 공유된 두 정점을 찾고 반환
                Vector3 v1Data = _navMeshData.vertices[commonVertexIndices[0]];
                Vector3 v1 = new Vector3(v1Data.X, v1Data.Y, v1Data.Z);

                Vector3 v2Data = _navMeshData.vertices[commonVertexIndices[1]];
                Vector3 v2 = new Vector3(v2Data.X, v2Data.Y, v2Data.Z);

                return Tuple.Create(v1, v2);
            }
            return null; 
        }

        

#region Helper Functions
        // 2D 벡터의 외적 계산
        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private static bool LineSegmentIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector2 a = new Vector2(p1.X, p1.Z);
            Vector2 b = new Vector2(p2.X, p2.Z);
            Vector2 c = new Vector2(p3.X, p3.Z);
            Vector2 d = new Vector2(p4.X, p4.Z);

            float denominator = (a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X);

            if (denominator == 0)
                return false;

            float t = ((a.X - c.X) * (c.Y - d.Y) - (a.Y - c.Y) * (c.X - d.X)) / denominator;
            float u = -((a.X - b.X) * (a.Y - c.Y) - (a.Y - b.Y) * (a.X - c.X)) / denominator;

            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }
    }
#endregion
}

#region 나는 모르는 함수
public static class PathSimplifier
{
    public static List<Vector3> DouglasPeuckerSimplify(List<Vector3> points, float epsilon)
    {
        if (points == null || points.Count < 3)
        {
            return points; // 2개 이하의 점은 단순화할 수 없습니다.
        }

        // 재귀 함수 호출로 단순화 시작
        return Simplify(points, 0, points.Count - 1, epsilon);
    }

    private static List<Vector3> Simplify(List<Vector3> points, int firstIndex, int lastIndex, float epsilon)
    {
        float maxDistance = 0;
        int index = -1;

        for (int i = firstIndex + 1; i < lastIndex; i++)
        {
            // 수직 거리 계산 (XZ 평면 기준)
            float distance = PerpendicularDistance(points[i], points[firstIndex], points[lastIndex]);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }

        List<Vector3> result = new List<Vector3>();

        if (maxDistance > epsilon)
        {
            List<Vector3> left = Simplify(points, firstIndex, index, epsilon);
            List<Vector3> right = Simplify(points, index, lastIndex, epsilon);

            result.AddRange(left);
            result.AddRange(right.Skip(1));
        }
        else
        {
            result.Add(points[firstIndex]);
            result.Add(points[lastIndex]);
        }
        return result;
    }

    private static float PerpendicularDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        // Vector3의 X, Z 필드가 float 타입이라고 가정합니다.
        Vector2 p2d = new Vector2(p.X, p.Z);
        Vector2 a2d = new Vector2(a.X, a.Z);
        Vector2 b2d = new Vector2(b.X, b.Z);
    
        // Vector2.Distance는 XZ 평면에서의 거리를 계산한다고 가정합니다.
        float lengthSq = Vector2.Distance(a2d, b2d);
    
        if (lengthSq == 0)
        {
            return Vector2.Distance(p2d, a2d);
        }
    
        // t = (AP dot AB) / |AB|^2
        // Vector2.Dot은 Vector2 구조체에 정의되어 있다고 가정합니다.
        float t = Vector2.Dot(p2d - a2d, b2d - a2d) / lengthSq;
    
        // ⭐️ Math.Clamp01(t) 대체 ⭐️
        // t가 0보다 작으면 0을, 1보다 크면 1을 사용합니다.
        t = (float)Math.Max(0, Math.Min(1, t)); 
        // 또는 t = Math.Max(0f, Math.Min(1f, t)); (float 명시)
    
        Vector2 projection = a2d + t * (b2d - a2d);
    
        return Vector2.Distance(p2d, projection);
    }
}
#endregion