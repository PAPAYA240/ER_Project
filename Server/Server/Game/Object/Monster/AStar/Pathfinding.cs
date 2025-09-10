using Lucene.Net.Util;
using Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Xml.Linq;

namespace Server.Game.Object.Monster.AStar
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

        public static void Initialize()
        {
            string basePath = ConfigManager.Config.dataPaths["monster"];
            string navMeshFilePath = Path.Combine(basePath, "MonsterData/navmesh_data.json");
            string navMeshJsonText = File.ReadAllText(navMeshFilePath);

            _navMeshData = NavMeshExportData.LoadFromJson(navMeshFilePath);
            if (_navMeshData == null)
            {
                Console.WriteLine("Failed to load NavMesh data. Pathfinding will not work.");
                return;
            }

            BuildTriangleGraph();
            Console.WriteLine($"NavMesh graph built with {_triangleNodes.Count} triangles.");
        }

        private static void BuildTriangleGraph()
        {
            _triangleNodes = new List<Node>();

            // 1. 각 삼각형에 대한 Node 객체 생성 및 중심점 계산
            List<int> triangles = _navMeshData.triangles;
            for (int i = 0; i < triangles.Count / 3; i++)
            {
                SerializableVector3 v0Data = _navMeshData.vertices[triangles[i * 3]];
                Vector3 v0 = new Vector3(v0Data.x, v0Data.y, v0Data.z);

                SerializableVector3 v1Data = _navMeshData.vertices[triangles[i * 3 + 1]];
                Vector3 v1 = new Vector3(v1Data.x, v1Data.y, v1Data.z);

                SerializableVector3 v2Data = _navMeshData.vertices[triangles[i * 3 + 2]];
                Vector3 v2 = new Vector3(v2Data.x, v2Data.y, v2Data.z);

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

                // 정점 위치를 미리 가져옵니다.
                Vector3[] triVertices = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    SerializableVector3 sv = _navMeshData.vertices[triIndices[k]];
                    triVertices[k] = new Vector3(sv.x, sv.y, sv.z);
                }

                for (int j = 0; j < 3; j++)
                {
                    Vector3 v1 = triVertices[j];
                    Vector3 v2 = triVertices[(j + 1) % 3];

                    // TODO : 정밀도 너무 높아서 잘라냄 - 이거 없애는 방법 생각해보기
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


        // 두 삼각형이 공통된 변을 공유하는지 확인
        private static bool ShareEdge(int[] triA_indices, int[] triB_indices)
        {
            int commonVertices = 0;
            float tolerance = 0.0001f;

            // 1. 두 삼각형의 정점 가져오기
            List<SerializableVector3> triA_vertices = new List<SerializableVector3>();
            foreach (int idx in triA_indices)
            {
                triA_vertices.Add(_navMeshData.vertices[idx]);
            }

            List<SerializableVector3> triB_vertices = new List<SerializableVector3>();
            foreach (int idx in triB_indices)
            {
                triB_vertices.Add(_navMeshData.vertices[idx]);
            }

            // 2. 두 정점 집합 비교
            foreach (var vA in triA_vertices)
            {
                foreach (var vB in triB_vertices)
                {
                    float distSquared = (vA.x - vB.x) * (vA.x - vB.x) + (vA.y - vB.y) * (vA.y - vB.y) +(vA.z - vB.z) * (vA.z - vB.z);

                    if (distSquared < tolerance * tolerance)
                    {
                        commonVertices++;
                        break; // 이미 찾았으니 다음 vA로 이동
                    }
                }
            }
            return commonVertices >= 2;
        }

        // 주어진 점이 어떤 삼각형 내부에 있는지 찾기
        private static Node FindTriangleContainingPoint(Vector3 point)
        {
            // TODO : 이거 최적화 필요,  (아마도 : 공간 분할 구조 사용)
            List<int> triangles = _navMeshData.triangles; 
            //ㅁㄴㅇ림;ㅣㄴ아러;미나얼
            for (int i = 0; i < _triangleNodes.Count; i++)
            {
                int[] tri_indices = new int[] { triangles[i * 3], triangles[i * 3 + 1], triangles[i * 3 + 2] };
                SerializableVector3 v0Data = _navMeshData.vertices[tri_indices[0]];
                Vector3 v0 = new Vector3(v0Data.x, v0Data.y, v0Data.z);

                SerializableVector3 v1Data = _navMeshData.vertices[tri_indices[1]];
                Vector3 v1 = new Vector3(v1Data.x, v1Data.y, v1Data.z);

                SerializableVector3 v2Data = _navMeshData.vertices[tri_indices[2]];
                Vector3 v2 = new Vector3(v2Data.x, v2Data.y, v2Data.z);

                if (IsPointInTriangle(point, v0, v1, v2))
                    return _triangleNodes[i];
            }
            return null; 
        }

        // 3D 공간에서 점이 삼각형 내부에 있는지 확인 
        private static bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // XZ 평면으로 투영
            Vector2 p2 = new Vector2(p.X, p.Z);
            Vector2 a2 = new Vector2(a.X, a.Z);
            Vector2 b2 = new Vector2(b.X, b.Z);
            Vector2 c2 = new Vector2(c.X, c.Z);

            float s = a2.Y * c2.X - a2.X * c2.Y + (c2.Y - a2.Y) * p2.X + (a2.X - c2.X) * p2.Y;
            float t = a2.X * b2.Y - a2.Y * b2.X + (a2.Y - b2.Y) * p2.X + (b2.X - a2.X) * p2.Y;

            if ((s < 0) != (t < 0) && s != 0 && t != 0)
                return false;

            float A = -b2.Y * c2.X + a2.Y * (c2.X - b2.X) + a2.X * (b2.Y - c2.Y) + b2.X * c2.Y;
            if (A < 0) // 삼각형의 방향에 따라 A가 음수일 수 있음
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s >= 0 && t >= 0 && (s + t) <= A;
        }


        public static List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            var openSet = new List<Node>(); // 탐색할 노드 목록 (우선순위 큐 역할)
            var cameFrom = new Dictionary<Node, Node>(); // 경로를 추적하기 위한 딕셔너리
            var gScore = new Dictionary<Node, float>(); // 시작점에서 현재 노드까지의 실제 비용
            var fScore = new Dictionary<Node, float>(); // 총 예상 비용 시 부럴ㅇㅈ닞ㄹㅇㄹ묃러;미ㅏㄴ어리;ㅏ젇리ㅏㅓ미;ㅏ얼;ㅣㅏㅁ젇ㄹ

            Node startNode = FindNearestNode(start);
            Node targetNode = FindNearestNode(end);

            // 시작 노드를 openSet에 추가하고 초기 비용b 설정
            openSet.Add(startNode);
            gScore[startNode] = 0;

            // 휴리스틱 비용 계산 (목적지까지의 유클리드 거리)
            fScore[startNode] = Vector3.Distance(startNode.Center, targetNode.Center);

            while (openSet.Count > 0)
            {
                // openSet에서 fScore가 가장 낮은 노드를 찾습니다.
                // 이것이 A* 알고리즘의 핵심입니다.
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore[openSet[i]] < fScore[currentNode])
                    {
                        currentNode = openSet[i];
                    }
                }

                // 목적지에 도달했으면 경로 재구성 후 반환
                if (currentNode.Equals(targetNode))
                    return SmoothPath(RetracePath(startNode, currentNode), start, end);

                openSet.Remove(currentNode);

                // 현재 노드의 이웃 노드들을 탐색
                foreach (var neighbor in currentNode.Neighbors)
                {
                    float tentativeGScore = gScore.ContainsKey(currentNode) ? gScore[currentNode] + Vector3.Distance(currentNode.Center, neighbor.Center) : float.MaxValue;

                    // 이웃 노드를 이미 방문했거나, 더 나은 경로가 아니거나
                    if (gScore.ContainsKey(neighbor) && tentativeGScore >= gScore[neighbor])
                    {
                        continue;
                    }

                    // 더 나은 경로를 찾았으므로 업데이트
                    cameFrom[neighbor] = currentNode;
                    gScore[neighbor] = tentativeGScore;

                    // 목적지까지의 휴리스틱 비용 계산
                    float hScore = Vector3.Distance(neighbor.Center, targetNode.Center);
                    fScore[neighbor] = tentativeGScore + hScore;

                    // openSet에 이웃 노드가 없으면 추가
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }

            // 경로를 찾지 못함
            return null;
        }

        // 최종 경로 역추적
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
                SerializableVector3 v1Data = _navMeshData.vertices[commonVertexIndices[0]];
                Vector3 v1 = new Vector3(v1Data.x, v1Data.y, v1Data.z);

                SerializableVector3 v2Data = _navMeshData.vertices[commonVertexIndices[1]];
                Vector3 v2 = new Vector3(v2Data.x, v2Data.y, v2Data.z);

                return Tuple.Create(v1, v2);
            }
            return null; 
        }

        // 스무쓰 경로를 위한 함수
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

            // A*가 찾은 삼각형 노드 경로를 순회
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

                // 변의 레프트라이트 구분
                if (Cross(new Vector2(p1.X - apex.X, p1.Z - apex.Z), new Vector2(p2.X - apex.X, p2.Z - apex.Z)) > 0)
                {
                    currentLeft = p1;
                    currentRight = p2;
                }
                else
                {
                    currentLeft = p2;
                    currentRight = p1;
                }

                // 1. 새로운 변이 왼쪽 정점이 현재 깔때기(funnel)의 왼쪽 경계를 좁히는 경우
                if (Cross(new Vector2(portalLeft.X - apex.X, portalLeft.Z - apex.Z), new Vector2(currentLeft.X - apex.X, currentLeft.Z - apex.Z)) < 0)
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
                if (Cross(new Vector2(portalRight.X - apex.X, portalRight.Z - apex.Z), new Vector2(currentRight.X - apex.X, currentRight.Z - apex.Z)) > 0)
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