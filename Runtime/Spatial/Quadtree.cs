using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeepr.Spatial
{
    [Serializable]
    public class Quadtree<T> where T : Component
    {
        [Serializable]
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
        [SerializeField] private QuadtreeNode rootNode;
        [SerializeField] private int maxDepth;
        [SerializeField] private int maxComponentsPerNode;
        
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
            Rect bounds = CalculateBounds();
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
        }
        
        public List<T> GetComponentsInRadius(Vector2 position, float radius)
        {
            List<T> result = new List<T>();
            if (rootNode == null) return result;
            
            Rect queryRect = new Rect(position.x - radius, position.y - radius, radius * 2, radius * 2);
            QueryRange(rootNode, queryRect, position, radius, result);
            return result;
        }
        
        public List<T> GetComponentsInBounds(Rect bounds)
        {
            List<T> result = new List<T>();
            if (rootNode == null) return result;
            
            QueryRange(rootNode, bounds, Vector2.zero, 0, result, false);
            return result;
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
                    Insert(node.children[i], component, position, depth + 1);
                }
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
        
        private void QueryRange(QuadtreeNode node, Rect range, Vector2 center, float radius, List<T> result, bool useRadius = true)
        {
            if (node == null || !node.bounds.Overlaps(range))
                return;
                
            foreach (var component in node.components)
            {
                if (component == null) continue;
                
                Vector2 componentPos = component.transform.position;
                
                if (useRadius)
                {
                    if (Vector2.Distance(center, componentPos) <= radius)
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
                    QueryRange(node.children[i], range, center, radius, result, useRadius);
                }
            }
        }
        
        private Rect CalculateBounds()
        {
            if (allComponents.Count == 0)
                return new Rect(0, 0, 100, 100);
                
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            
            foreach (var component in allComponents)
            {
                if (component != null)
                {
                    Vector2 pos = component.transform.position;
                    min = Vector2.Min(min, pos);
                    max = Vector2.Max(max, pos);
                }
            }
            
            // Add padding to ensure all components are inside bounds
            Vector2 padding = (max - min) * 0.1f + Vector2.one * 10f;
            min -= padding;
            max += padding;
            
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
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
            if (node == null || node.children == null || node.children.Length != 8) return;
            
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