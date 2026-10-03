using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DataKeeper.Editor.MeshTools
{
    public sealed class MeshUVPreview
    {
        private static readonly string[] MainTextureProperties = { "_BaseMap", "_MainTex" };

        private readonly List<Vector2> _sourceUVs = new List<Vector2>();
        private readonly List<Vector2> _previewUVs = new List<Vector2>();
        private readonly List<int> _edges = new List<int>();
        private readonly List<int> _indices = new List<int>();
        private readonly HashSet<long> _edgeKeys = new HashSet<long>();

        private MeshFilter _filter;
        private Mesh _source;
        private Mesh _sceneMesh;
        private int _channel;
        private Rect _sourceBounds;

        public List<Vector2> SourceUVs => _sourceUVs;
        public List<Vector2> PreviewUVs => _previewUVs;
        public List<int> Edges => _edges;
        public Texture MainTexture { get; private set; }
        public Rect Bounds { get; private set; }

        public void Bind(MeshFilter filter, int channel)
        {
            EndScenePreview();

            _filter = filter;
            _channel = channel;
            _source = filter != null ? filter.sharedMesh : null;
            _sourceUVs.Clear();
            _previewUVs.Clear();
            _edges.Clear();
            MainTexture = null;

            if (_source == null || !_source.HasVertexAttribute(VertexAttribute.TexCoord0 + channel))
                return;

            _source.GetUVs(channel, _sourceUVs);
            _previewUVs.AddRange(_sourceUVs);
            _sourceBounds = CalculateBounds(_sourceUVs);
            Bounds = _sourceBounds;
            BuildEdges();
            MainTexture = FindMainTexture(filter.GetComponent<MeshRenderer>());
        }

        public void SetTransform(Vector2 scale, Vector2 offset)
        {
            for (int i = 0; i < _sourceUVs.Count; i++)
                _previewUVs[i] = Vector2.Scale(_sourceUVs[i], scale) + offset;

            if (_previewUVs.Count > 0)
            {
                var previewBounds = CalculateBounds(_previewUVs);
                Bounds = Rect.MinMaxRect(
                    Mathf.Min(_sourceBounds.xMin, previewBounds.xMin), Mathf.Min(_sourceBounds.yMin, previewBounds.yMin),
                    Mathf.Max(_sourceBounds.xMax, previewBounds.xMax), Mathf.Max(_sourceBounds.yMax, previewBounds.yMax));
            }

            if (_sceneMesh != null)
                _sceneMesh.SetUVs(_channel, _previewUVs);
        }

        // The swap is never recorded or marked dirty; the window restores the original before
        // anything serializes the scene (save, play mode, domain reload).
        public void BeginScenePreview()
        {
            if (_sceneMesh != null || _filter == null || _sourceUVs.Count == 0)
                return;

            _sceneMesh = Object.Instantiate(_source);
            _sceneMesh.name = _source.name;
            _sceneMesh.hideFlags = HideFlags.HideAndDontSave;
            _sceneMesh.SetUVs(_channel, _previewUVs);
            _filter.sharedMesh = _sceneMesh;
        }

        public void EndScenePreview()
        {
            if (_sceneMesh == null)
                return;

            // After an undo the filter may already hold a different mesh, which must not be overwritten.
            if (_filter != null && _filter.sharedMesh == _sceneMesh)
                _filter.sharedMesh = _source;

            Object.DestroyImmediate(_sceneMesh);
            _sceneMesh = null;
        }

        private void BuildEdges()
        {
            _edgeKeys.Clear();
            for (int subMesh = 0; subMesh < _source.subMeshCount; subMesh++)
            {
                if (_source.GetTopology(subMesh) != MeshTopology.Triangles)
                    continue;

                _source.GetIndices(_indices, subMesh);
                for (int i = 0; i < _indices.Count; i += 3)
                {
                    AddEdge(_indices[i], _indices[i + 1]);
                    AddEdge(_indices[i + 1], _indices[i + 2]);
                    AddEdge(_indices[i + 2], _indices[i]);
                }
            }
            _edgeKeys.Clear();
        }

        private void AddEdge(int a, int b)
        {
            var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (!_edgeKeys.Add(key))
                return;

            _edges.Add(a);
            _edges.Add(b);
        }

        private static Rect CalculateBounds(List<Vector2> uvs)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < uvs.Count; i++)
            {
                min = Vector2.Min(min, uvs[i]);
                max = Vector2.Max(max, uvs[i]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Texture FindMainTexture(MeshRenderer renderer)
        {
            var material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null)
                return null;

            foreach (var property in MainTextureProperties)
            {
                if (material.HasTexture(property))
                    return material.GetTexture(property);
            }
            return null;
        }
    }
}
