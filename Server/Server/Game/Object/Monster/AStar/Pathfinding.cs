using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Game.Object.Monster.AStar
{
    public class Node : IComparable<Node>
    {
        public int X, Z; // 그리드 좌표
        public int G, H; // G 시작점- 나 사이 비용, H 목적지까지 예상 비용
        public int F => G + H;
        public Node Parent;
        public Node(int x, int z) { X = x; Z = z; }
        public int CompareTo(Node other)
        {
            if (F == other.F) return 0;
            return F < other.F ? -1 : 1;
        }
        public override bool Equals(object obj)
        {
            return obj is Node node && X == node.X && Z == node.Z;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }
    }
    public static class Pathfinding
    {
        public static List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            int startX = (int)start.X;
            int startZ = (int)start.Z;
            int endX = (int)end.X;
            int endZ = (int)end.Z;

            // 탐색 목록임 
            List<Node> openList = new List<Node>();
            // 이미 탐색한 목록임
            HashSet<Node> closedList = new HashSet<Node>();

            Node startNode = new Node(startX, startZ);
            Node targetNode = new Node(endX, endZ);
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                // F가 가장 낮은 노드 찾기
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].F < currentNode.F ||
                        (openList[i].F == currentNode.F &&
                        openList[i].H < currentNode.H))
                        currentNode = openList[i];
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // 경로 도착 시
                if (currentNode.Equals(targetNode))
                {
                    List<Vector3> vectorPath = RetracePath(startNode, currentNode); 
                    if(vectorPath.Count > 0)
                        return SmoothPath(vectorPath);
                    else
                        return vectorPath;
                }

                // 8방향 탐색
                for (int z = -1; z <= 1; z++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && z == 0) continue;

                        int neighborX = currentNode.X + x;
                        int neighborZ = currentNode.Z + z;

                        // 못가는 구간
                        if (!GridManager.Instance.IsWalkable(neighborX, neighborZ))
                            continue;

                        Node neighborNode = new Node(neighborX, neighborZ);
                        // 탐색한 구간
                        if (closedList.Contains(neighborNode))
                             continue;

                        // 비용 계산
                        int newMoveCostNeighbor = currentNode.G + GetDistance(currentNode, neighborNode);
                        Node existNeighbor = openList.Find(n => n.Equals(neighborNode));
                        if (existNeighbor == null)
                        {
                            neighborNode.G = newMoveCostNeighbor;
                            neighborNode.H = GetDistance(neighborNode, targetNode);
                            neighborNode.Parent = currentNode;
                            openList.Add(neighborNode);
                        }
                        // 2. openList에 이미 있는데, 지금 경로가 더 효율적인가?
                        else
                        {
                            if (newMoveCostNeighbor < existNeighbor.G)
                            {
                                // 더 효율적이면 정보 갱신
                                existNeighbor.G = newMoveCostNeighbor;
                                existNeighbor.Parent = currentNode;
                            }
                        }
                    }
                }
            }
            return new List<Vector3>(); // 경로 찾기 실패
        }

        private static List<Vector3> SmoothPath(List<Vector3> originalPath)
        {
            if (originalPath.Count < 2)
                return originalPath;

            List<Vector3> smoothedPath = new List<Vector3>();
            smoothedPath.Add(originalPath[0]); // 시작점은 무조건 추가

            int currentIdx = 0;
            while (currentIdx < originalPath.Count - 1)
            {
                int lastVisibleIdx = currentIdx + 1;
                for (int checkIdx = currentIdx + 2; checkIdx < originalPath.Count; checkIdx++)
                {
                    // currentIdx에서 checkIdx까지 직선으로 갈 수 있는지 확인
                    if (HasLineOfSight(originalPath[currentIdx], originalPath[checkIdx]))
                    {
                        lastVisibleIdx = checkIdx; // 갈 수 있다면, 더 먼 점을 목표로
                    }
                    else
                    {
                        break; // 중간에 장애물이 있으면 중단
                    }
                }
                smoothedPath.Add(originalPath[lastVisibleIdx]);
                currentIdx = lastVisibleIdx;
            }

            return smoothedPath;
        }
        private static bool HasLineOfSight(Vector3 start, Vector3 end)
        {
            return true;
        }

        // 최종 경로 역추적
        private static List<Vector3> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            path.Reverse();

            // 좌표 변환하기
            List<Vector3> vecPath = new List<Vector3>();
            foreach (Node node in path)
            {
                float y = GridManager.Instance.GetHeight(node.X, node.Z);
                vecPath.Add(new Vector3(node.X, y, node.Z));
            }
            return vecPath;
        }

        // 맨해튼 거리 계산
        private static int GetDistance(Node nodeA, Node nodeB)
        {
            int distX = Math.Abs(nodeA.X - nodeB.X);
            int distZ = Math.Abs(nodeA.Z - nodeB.Z);
            
            return (distX > distZ) ?
                14 * distZ + 10 * (distX - distZ) :
                14 * distX + 10 * (distZ - distX);
        }
    }
}
