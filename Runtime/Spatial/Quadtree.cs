using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Spatial
{
    [Serializable]
    public class Quadtree<T> where T : Component
    {
        // Not serializable: Unity can't serialize self-referencing node types
        // (depth limit errors) and deserializes null references as empty
        // instances, breaking rootNode == null checks. Rebuild via BuildTree().
        public class QuadtreeNode
        {
            public Rect bounds;
            public List<T> components;
            public QuadtreeNode[] children;
            public bool isLeaf;

            public QuadtreeNode(Rect bounds)
            {
                this.bounds = bounds;
                this.components = new List<T>();
                this.children = new QuadtreeNode[4];
                this.isLeaf = true;
            }
        }

        [SerializeField] private List<T> allComponents;
        [SerializeField] private int maxDepth;
        [SerializeField] private int maxComponentsPerNode;

        private QuadtreeNode rootNode;

        public List<T> AllComponents => allComponents;
        public QuadtreeNode Root => rootNode;

        public Quadtree(int maxDepth = 5, int maxComponentsPerNode = 10)
        {
            this.allComponents = new List<T>();
            this.maxDepth = maxDepth;
            this.maxComponentsPerNode = maxComponentsPerNode;
            this.rootNode = null;
        }

        public void BuildTree()
        {
            if (allComponents.Count == 0)
            {
                rootNode = null;
                return;
            }

            // Calculate bounds from all components
            if (!TryCalculateBounds(out Rect bounds))
            {
                rootNode = null;
                return;
            }

            rootNode = new QuadtreeNode(bounds);

            foreach (var component in allComponents)
            {
                if (component != null && component.gameObject.activeInHierarchy)
                {
                    Vector2 position = component.transform.position;
                    Insert(rootNode, component, position, 0);
                }
            }
        }

        public void AddComponent(T component)
        {
            if (component != null && !allComponents.Contains(component))
            {
                allComponents.Add(component);
            }
        }

        public void AddComponents(IEnumerable<T> components)
        {
            foreach (var component in components)
            {
                AddComponent(component);
            }
        }

        public void RemoveComponent(T component)
        {
            allComponents.Remove(component);
        }

        public void Clear()
        {
            rootNode = null;
            allComponents.Clear();
        }

        public List<T> GetComponentsInRadius(Vector2 position, float radius)
        {
            List<T> result = new List<T>();
            GetComponentsInRadius(position, radius, result);
            return result;
        }

        /// <summary>Non-allocating overload. Clears and fills the provided list.</summary>
        public void GetComponentsInRadius(Vector2 position, float radius, List<T> result)
        {
            result.Clear();
            if (rootNode == null) return;

            Rect queryRect = new Rect(position.x - radius, position.y - radius, radius * 2, radius * 2);
            QueryRange(rootNode, queryRect, position, radius * radius, result);
        }

        public List<T> GetComponentsInBounds(Rect bounds)
        {
            List<T> result = new List<T>();
            GetComponentsInBounds(bounds, result);
            return result;
        }

        /// <summary>Non-allocating overload. Clears and fills the provided list.</summary>
        public void GetComponentsInBounds(Rect bounds, List<T> result)
        {
            result.Clear();
            if (rootNode == null) return;

            QueryRange(rootNode, bounds, Vector2.zero, 0, result, false);
        }

        private void Insert(QuadtreeNode node, T component, Vector2 position, int depth)
        {
            if (!node.bounds.Contains(position))
                return;

            if (node.isLeaf)
            {
                node.components.Add(component);

                if (node.components.Count > maxComponentsPerNode && depth < maxDepth)
                {
                    Subdivide(node);

                    for (int i = node.components.Count - 1; i >= 0; i--)
                    {
                        T comp = node.components[i];
                        Vector2 compPos = comp.transform.position;

                        for (int j = 0; j < 4; j++)
                        {
                            if (node.children[j].bounds.Contains(compPos))
                            {
                                Insert(node.children[j], comp, compPos, depth + 1);
                                node.components.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (node.children[i].bounds.Contains(position))
                    {
                        Insert(node.children[i], component, position, depth + 1);
                        return;
                    }
                }

                // Inside this node but no child accepts it (float sliver at
                // internal edges) — keep it here so it isn't lost.
                node.components.Add(component);
            }
        }

        private void Subdivide(QuadtreeNode node)
        {
            float halfWidth = node.bounds.width / 2f;
            float halfHeight = node.bounds.height / 2f;
            float x = node.bounds.x;
            float y = node.bounds.y;

            node.children[0] = new QuadtreeNode(new Rect(x, y + halfHeight, halfWidth, halfHeight)); // NW
            node.children[1] = new QuadtreeNode(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight)); // NE
            node.children[2] = new QuadtreeNode(new Rect(x, y, halfWidth, halfHeight)); // SW
            node.children[3] = new QuadtreeNode(new Rect(x + halfWidth, y, halfWidth, halfHeight)); // SE

            node.isLeaf = false;
        }

        private void QueryRange(QuadtreeNode node, Rect range, Vector2 center, float sqrRadius, List<T> result, bool useRadius = true)
        {
            if (node == null || !node.bounds.Overlaps(range))
                return;

            foreach (var component in node.components)
            {
                if (component == null) continue;

                Vector2 componentPos = component.transform.position;

                if (useRadius)
                {
                    if ((componentPos - center).sqrMagnitude <= sqrRadius)
                    {
                        result.Add(component);
                    }
                }
                else
                {
                    if (range.Contains(componentPos))
                    {
                        result.Add(component);
                    }
                }
            }

            if (!node.isLeaf)
            {
                for (int i = 0; i < 4; i++)
                {
                    QueryRange(node.children[i], range, center, sqrRadius, result, useRadius);
                }
            }
        }

        private bool TryCalculateBounds(out Rect bounds)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool hasAny = false;

            foreach (var component in allComponents)
            {
                if (component != null && component.gameObject.activeInHierarchy)
                {
                    Vector2 pos = component.transform.position;
                    min = Vector2.Min(min, pos);
                    max = Vector2.Max(max, pos);
                    hasAny = true;
                }
            }

            if (!hasAny)
            {
                bounds = default;
                return false;
            }

            // Add padding to ensure all components are inside bounds
            Vector2 padding = (max - min) * 0.1f + Vector2.one * 10f;
            min -= padding;
            max += padding;

            bounds = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            return true;
        }

        public void DrawGizmos()
        {
            if (rootNode != null)
            {
                DrawNodeGizmos(rootNode);
            }
        }

        private void DrawNodeGizmos(QuadtreeNode node)
        {
            if (node == null) return;

            // Draw node bounds
            Gizmos.color = node.isLeaf ? Color.green : Color.blue;
            Vector3 center = new Vector3(node.bounds.center.x, node.bounds.center.y, 0);
            Vector3 size = new Vector3(node.bounds.size.x, node.bounds.size.y, 0);
            Gizmos.DrawWireCube(center, size);

            // Draw components in this node
            Gizmos.color = Color.yellow;
            foreach (var component in node.components)
            {
                if (component != null)
                {
                    Vector3 pos = component.transform.position;
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
            }

            // Recursively draw children
            if (!node.isLeaf)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawNodeGizmos(node.children[i]);
                }
            }
        }
    }
}
