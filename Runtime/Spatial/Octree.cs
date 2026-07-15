using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Spatial
{
    [Serializable]
    public class Octree<T> where T : Component
    {
        // Not serializable: Unity can't serialize self-referencing node types
        // (depth limit errors) and deserializes null references as empty
        // instances, breaking rootNode == null checks. Rebuild via BuildTree().
        public class OctreeNode
        {
            public Bounds bounds;
            public List<T> components;
            public OctreeNode[] children;
            public bool isLeaf;

            public OctreeNode(Bounds bounds)
            {
                this.bounds = bounds;
                this.components = new List<T>();
                this.children = new OctreeNode[8];
                this.isLeaf = true;
            }
        }

        [SerializeField] private List<T> allComponents;
        [SerializeField] private int maxDepth;
        [SerializeField] private int maxComponentsPerNode;

        private OctreeNode rootNode;

        public List<T> AllComponents => allComponents;
        public OctreeNode Root => rootNode;

        public Octree(int maxDepth = 5, int maxComponentsPerNode = 10)
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
            if (!TryCalculateBounds(out Bounds bounds))
            {
                rootNode = null;
                return;
            }

            rootNode = new OctreeNode(bounds);

            foreach (var component in allComponents)
            {
                if (component != null && component.gameObject.activeInHierarchy)
                {
                    Vector3 position = component.transform.position;
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

        public List<T> GetComponentsInRadius(Vector3 position, float radius)
        {
            List<T> result = new List<T>();
            GetComponentsInRadius(position, radius, result);
            return result;
        }

        /// <summary>Non-allocating overload. Clears and fills the provided list.</summary>
        public void GetComponentsInRadius(Vector3 position, float radius, List<T> result)
        {
            result.Clear();
            if (rootNode == null) return;

            Bounds queryBounds = new Bounds(position, Vector3.one * radius * 2);
            QueryRange(rootNode, queryBounds, position, radius * radius, result);
        }

        public List<T> GetComponentsInBounds(Bounds bounds)
        {
            List<T> result = new List<T>();
            GetComponentsInBounds(bounds, result);
            return result;
        }

        /// <summary>Non-allocating overload. Clears and fills the provided list.</summary>
        public void GetComponentsInBounds(Bounds bounds, List<T> result)
        {
            result.Clear();
            if (rootNode == null) return;

            QueryRange(rootNode, bounds, Vector3.zero, 0, result, false);
        }

        private void Insert(OctreeNode node, T component, Vector3 position, int depth)
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
                        Vector3 compPos = comp.transform.position;

                        for (int j = 0; j < 8; j++)
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
                // Bounds.Contains is inclusive on all faces, so a point on a
                // split plane is contained by several children — insert into
                // the first one only to avoid duplicates.
                for (int i = 0; i < 8; i++)
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

        private void Subdivide(OctreeNode node)
        {
            Vector3 size = node.bounds.size / 2f;
            Vector3 center = node.bounds.center;
            Vector3 quarter = size / 2f;

            // Create 8 octants
            node.children[0] = new OctreeNode(new Bounds(center + new Vector3(-quarter.x, quarter.y, quarter.z), size));   // Front Top Left
            node.children[1] = new OctreeNode(new Bounds(center + new Vector3(quarter.x, quarter.y, quarter.z), size));    // Front Top Right
            node.children[2] = new OctreeNode(new Bounds(center + new Vector3(-quarter.x, -quarter.y, quarter.z), size));  // Front Bottom Left
            node.children[3] = new OctreeNode(new Bounds(center + new Vector3(quarter.x, -quarter.y, quarter.z), size));   // Front Bottom Right
            node.children[4] = new OctreeNode(new Bounds(center + new Vector3(-quarter.x, quarter.y, -quarter.z), size));  // Back Top Left
            node.children[5] = new OctreeNode(new Bounds(center + new Vector3(quarter.x, quarter.y, -quarter.z), size));   // Back Top Right
            node.children[6] = new OctreeNode(new Bounds(center + new Vector3(-quarter.x, -quarter.y, -quarter.z), size)); // Back Bottom Left
            node.children[7] = new OctreeNode(new Bounds(center + new Vector3(quarter.x, -quarter.y, -quarter.z), size));  // Back Bottom Right

            node.isLeaf = false;
        }

        private void QueryRange(OctreeNode node, Bounds range, Vector3 center, float sqrRadius, List<T> result, bool useRadius = true)
        {
            if (node == null || !node.bounds.Intersects(range))
                return;

            foreach (var component in node.components)
            {
                if (component == null) continue;

                Vector3 componentPos = component.transform.position;

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
                for (int i = 0; i < 8; i++)
                {
                    QueryRange(node.children[i], range, center, sqrRadius, result, useRadius);
                }
            }
        }

        private bool TryCalculateBounds(out Bounds bounds)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool hasAny = false;

            foreach (var component in allComponents)
            {
                if (component != null && component.gameObject.activeInHierarchy)
                {
                    Vector3 pos = component.transform.position;
                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                    hasAny = true;
                }
            }

            if (!hasAny)
            {
                bounds = default;
                return false;
            }

            // Add padding to ensure all components are inside bounds
            Vector3 padding = (max - min) * 0.1f + Vector3.one * 10f;
            min -= padding;
            max += padding;

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            bounds = new Bounds(center, size);
            return true;
        }

        public void DrawGizmos()
        {
            if (rootNode != null)
            {
                DrawNodeGizmos(rootNode);
            }
        }

        private void DrawNodeGizmos(OctreeNode node)
        {
            if (node == null) return;

            // Draw node bounds
            Gizmos.color = node.isLeaf ? Color.green : Color.blue;
            Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

            // Draw components in this node
            Gizmos.color = Color.yellow;
            foreach (var component in node.components)
            {
                if (component != null)
                {
                    Gizmos.DrawWireSphere(component.transform.position, 0.5f);
                }
            }

            // Recursively draw children
            if (!node.isLeaf)
            {
                for (int i = 0; i < 8; i++)
                {
                    DrawNodeGizmos(node.children[i]);
                }
            }
        }
    }
}
