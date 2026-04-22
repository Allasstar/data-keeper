using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DataKeeper.Debugger
{
    /// <summary>
    /// Lightweight runtime shape drawing system using GL immediate mode.
    /// Works in builds (not editor-only like Debug.Draw / Gizmos).
    ///
    /// Usage:
    ///   DebugDraw.Line(a, b, Color.red);                                    // DepthCorrect by default
    ///   DebugDraw.Sphere(pos, 0.5f, Color.green, 2f);                      // visible for 2 seconds
    ///   DebugDraw.WireCube(center, size, Color.yellow, 0f, default,
    ///                 DebugDraw.Mode.XRay);                                 // visible through walls, dimmed
    ///   DebugDraw.Arrow(from, to, Color.white, 0.25f, 0f,
    ///              DebugDraw.Mode.AlwaysOnTop);                             // fully on top, ignores depth
    ///
    /// No setup required — bootstraps itself on first call.
    /// Add DEBUG_DRAW to Scripting Define Symbols to run in release.
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_DRAW
    public static class DebugDraw
    {
        // ─── Depth Mode ──────────────────────────────────────────────────────────

        /// <summary>Controls how a drawn shape interacts with scene depth.</summary>
        public enum Mode
        {
            /// <summary>
            /// Occluded by geometry — correct depth sorting, best spatial perception. Default.
            /// </summary>
            DepthCorrect,

            /// <summary>
            /// Always renders on top of everything, ignoring scene depth.
            /// Useful for UI-adjacent debug info that must never be hidden.
            /// </summary>
            AlwaysOnTop,

            /// <summary>
            /// Two-pass: full opacity where visible, dimmed (15% alpha) where occluded.
            /// Best of both worlds — spatially grounded yet always readable.
            /// </summary>
            XRay,
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        public static void Line(Vector3 start, Vector3 end, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Renderer.Instance.Enqueue(new DrawCommand(CommandType.Line, duration, color, mode, start, end));

        public static void Ray(Vector3 origin, Vector3 direction, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Line(origin, origin + direction, color, duration, mode);

        public static void Cross(Vector3 center, float size, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
        {
            float h = size * 0.5f;
            var r = Renderer.Instance;
            r.Enqueue(new DrawCommand(CommandType.Line, duration, color, mode,
                center - Vector3.right * h, center + Vector3.right * h));
            r.Enqueue(new DrawCommand(CommandType.Line, duration, color, mode,
                center - Vector3.up * h, center + Vector3.up * h));
            r.Enqueue(new DrawCommand(CommandType.Line, duration, color, mode,
                center - Vector3.forward * h, center + Vector3.forward * h));
        }

        public static void Circle(Vector3 center, float radius, Vector3 normal, Color color,
            float duration = 0f, int segments = 32, Mode mode = Mode.DepthCorrect)
            => Renderer.Instance.Enqueue(new DrawCommand(CommandType.Circle, duration, color, mode,
                center, normal) { Radius = radius, Segments = segments });

        public static void Sphere(Vector3 center, float radius, Color color,
            float duration = 0f, int segments = 16, Mode mode = Mode.DepthCorrect)
        {
            Circle(center, radius, Vector3.up, color, duration, segments, mode);
            Circle(center, radius, Vector3.right, color, duration, segments, mode);
            Circle(center, radius, Vector3.forward, color, duration, segments, mode);
        }

        public static void WireCube(Vector3 center, Vector3 size, Color color,
            float duration = 0f, Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Renderer.Instance.Enqueue(new DrawCommand(CommandType.WireBox, duration, color, mode,
                center, Vector3.zero) { Size = size, Rotation = rotation });
        }

        public static void WireCube(Vector3 center, float size, Color color,
            float duration = 0f, Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
            => WireCube(center, Vector3.one * size, color, duration, rotation, mode);

        public static void WireCapsule(Vector3 start, Vector3 end, float radius, Color color,
            float duration = 0f, int segments = 16, Mode mode = Mode.DepthCorrect)
            => Renderer.Instance.Enqueue(new DrawCommand(CommandType.WireCapsule, duration, color, mode,
                start, end) { Radius = radius, Segments = segments });

        public static void WireCapsule(Vector3 center, float height, float radius, Color color,
            float duration = 0f, int segments = 16, Mode mode = Mode.DepthCorrect)
        {
            float half = Mathf.Max(0f, height * 0.5f - radius);
            WireCapsule(center + Vector3.up * half, center - Vector3.up * half,
                radius, color, duration, segments, mode);
        }

        public static void Arrow(Vector3 from, Vector3 to, Color color,
            float headSize = 0.25f, float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Renderer.Instance.Enqueue(new DrawCommand(CommandType.Arrow, duration, color, mode,
                from, to) { Radius = headSize });

        public static void Bounds(Bounds bounds, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => WireCube(bounds.center, bounds.size, color, duration, default, mode);

        public static void Point(Vector3 position, float size, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Cross(position, size, color, duration, mode);

        // ─── Internals ───────────────────────────────────────────────────────────

        enum CommandType
        {
            Line,
            Circle,
            WireBox,
            WireCapsule,
            Arrow
        }

        struct DrawCommand
        {
            public CommandType Type;
            public float ExpiresAt;
            public Color Color;
            public Mode DepthMode;
            public Vector3 A, B;
            public Vector3 Size;
            public Quaternion Rotation;
            public float Radius;
            public int Segments;

            public DrawCommand(CommandType type, float duration, Color color, Mode depthMode,
                Vector3 a, Vector3 b)
            {
                Type = type;
                ExpiresAt = Time.time + duration;
                Color = color;
                DepthMode = depthMode;
                A = a;
                B = b;
                Size = Vector3.one;
                Rotation = Quaternion.identity;
                Radius = 0f;
                Segments = 16;
            }
        }

        // ─── Renderer MonoBehaviour ───────────────────────────────────────────────

        class Renderer : MonoBehaviour
        {
            static Renderer _instance;

            public static Renderer Instance
            {
                get
                {
                    if (_instance != null) return _instance;
                    var go = new GameObject("[RuntimeDraw]") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Renderer>();
                    return _instance;
                }
            }

            // How dim XRay shapes appear through occluding geometry
            const float XRayOccludedAlpha = 0.15f;

            Material _mat;
            List<DrawCommand> _persistent = new(64);
            List<DrawCommand> _oneFrame = new(256);
            List<DrawCommand> _swap = new(256);

            // Buckets filled each OnRenderObject — avoids mid-frame ZTest thrashing
            List<DrawCommand> _bucketDepth = new(128);
            List<DrawCommand> _bucketOnTop = new(64);
            List<DrawCommand> _bucketXRay = new(64);

            void Awake()
            {
                _mat = new Material(Shader.Find("Hidden/Internal-Colored"))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _mat.SetInt("_Cull", (int)CullMode.Off);
                _mat.SetInt("_ZWrite", 0);
                // ZTest is set per-batch below
            }

            public void Enqueue(DrawCommand cmd)
            {
                if (cmd.ExpiresAt <= Time.time)
                    _oneFrame.Add(cmd);
                else
                    _persistent.Add(cmd);
            }

            void LateUpdate()
            {
                float now = Time.time;
                _swap.Clear();
                foreach (var cmd in _persistent)
                    if (cmd.ExpiresAt > now)
                        _swap.Add(cmd);
                (_persistent, _swap) = (_swap, _persistent);
            }

            void OnRenderObject()
            {
                if (_mat == null) return;

                // ── Sort into mode buckets ────────────────────────────────────────
                _bucketDepth.Clear();
                _bucketOnTop.Clear();
                _bucketXRay.Clear();
                Bucket(_persistent);
                Bucket(_oneFrame);

                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.identity);

                // Pass 1 — DepthCorrect (LessEqual, full alpha)
                // Rendered first so AlwaysOnTop/XRay correctly overdraw it.
                if (_bucketDepth.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketDepth)
                        Render(cmd, 1f);
                }

                // Pass 2 — AlwaysOnTop (Always, full alpha)
                if (_bucketOnTop.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.Always);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketOnTop)
                        Render(cmd, 1f);
                }

                // Pass 3 — XRay: two sub-passes
                //   3a. Greater (behind geometry) — dimmed
                //   3b. LessEqual (in front)      — full alpha
                if (_bucketXRay.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.Greater);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketXRay)
                        Render(cmd, XRayOccludedAlpha);

                    _mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketXRay)
                        Render(cmd, 1f);
                }

                GL.PopMatrix();
                _oneFrame.Clear();
            }

            void Bucket(List<DrawCommand> source)
            {
                foreach (var cmd in source)
                {
                    switch (cmd.DepthMode)
                    {
                        case Mode.DepthCorrect: _bucketDepth.Add(cmd); break;
                        case Mode.AlwaysOnTop: _bucketOnTop.Add(cmd); break;
                        case Mode.XRay: _bucketXRay.Add(cmd); break;
                    }
                }
            }

            void Render(in DrawCommand cmd, float alphaScale)
            {
                Color c = cmd.Color;
                c.a *= alphaScale;
                switch (cmd.Type)
                {
                    case CommandType.Line: RenderLine(cmd.A, cmd.B, c); break;
                    case CommandType.Circle: RenderCircle(cmd.A, cmd.B, cmd.Radius, c, cmd.Segments); break;
                    case CommandType.WireBox: RenderWireBox(cmd.A, cmd.Size, cmd.Rotation, c); break;
                    case CommandType.WireCapsule: RenderWireCapsule(cmd.A, cmd.B, cmd.Radius, c, cmd.Segments); break;
                    case CommandType.Arrow: RenderArrow(cmd.A, cmd.B, c, cmd.Radius); break;
                }
            }

            // ── Primitive Renderers ───────────────────────────────────────────────

            static void RenderLine(Vector3 a, Vector3 b, Color color)
            {
                GL.Begin(GL.LINES);
                GL.Color(color);
                GL.Vertex(a);
                GL.Vertex(b);
                GL.End();
            }

            static void RenderCircle(Vector3 center, Vector3 normal, float radius,
                Color color, int segments)
            {
                Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f
                    ? Vector3.up
                    : Vector3.right;
                Vector3 tangA = Vector3.Cross(normal, up).normalized;
                Vector3 tangB = Vector3.Cross(normal, tangA);

                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                float step = Mathf.PI * 2f / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float a = step * i;
                    GL.Vertex(center + (tangA * Mathf.Cos(a) + tangB * Mathf.Sin(a)) * radius);
                }

                GL.End();
            }

            static void RenderWireBox(Vector3 center, Vector3 size, Quaternion rot, Color color)
            {
                Vector3 h = size * 0.5f;
                Vector3 c0 = center + rot * new Vector3(-h.x, -h.y, -h.z);
                Vector3 c1 = center + rot * new Vector3(h.x, -h.y, -h.z);
                Vector3 c2 = center + rot * new Vector3(h.x, -h.y, h.z);
                Vector3 c3 = center + rot * new Vector3(-h.x, -h.y, h.z);
                Vector3 c4 = center + rot * new Vector3(-h.x, h.y, -h.z);
                Vector3 c5 = center + rot * new Vector3(h.x, h.y, -h.z);
                Vector3 c6 = center + rot * new Vector3(h.x, h.y, h.z);
                Vector3 c7 = center + rot * new Vector3(-h.x, h.y, h.z);

                GL.Begin(GL.LINES);
                GL.Color(color);
                Edge(c0, c1);
                Edge(c1, c2);
                Edge(c2, c3);
                Edge(c3, c0); // bottom
                Edge(c4, c5);
                Edge(c5, c6);
                Edge(c6, c7);
                Edge(c7, c4); // top
                Edge(c0, c4);
                Edge(c1, c5);
                Edge(c2, c6);
                Edge(c3, c7); // verticals
                GL.End();
            }

            static void RenderWireCapsule(Vector3 top, Vector3 bottom, float radius,
                Color color, int segments)
            {
                Vector3 axis = (top - bottom).normalized;
                if (axis == Vector3.zero) axis = Vector3.up;

                Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.99f
                    ? Vector3.up
                    : Vector3.right;
                Vector3 tangA = Vector3.Cross(axis, up).normalized;
                Vector3 tangB = Vector3.Cross(axis, tangA);

                RenderCircle(top, axis, radius, color, segments);
                RenderCircle(bottom, axis, radius, color, segments);

                GL.Begin(GL.LINES);
                GL.Color(color);
                for (int i = 0; i < 4; i++)
                {
                    float a = Mathf.PI * 0.5f * i;
                    Vector3 off = (tangA * Mathf.Cos(a) + tangB * Mathf.Sin(a)) * radius;
                    GL.Vertex(top + off);
                    GL.Vertex(bottom + off);
                }

                GL.End();

                RenderHemisphere(top, axis, radius, tangA, tangB, color, segments);
                RenderHemisphere(bottom, -axis, radius, tangA, tangB, color, segments);
            }

            static void RenderHemisphere(Vector3 center, Vector3 axis, float radius,
                Vector3 tangA, Vector3 tangB, Color color, int segments)
            {
                int half = segments / 2;
                for (int arc = 0; arc < 2; arc++)
                {
                    Vector3 planar = arc == 0 ? tangA : tangB;
                    GL.Begin(GL.LINE_STRIP);
                    GL.Color(color);
                    for (int i = 0; i <= half; i++)
                    {
                        float t = Mathf.PI * i / half;
                        GL.Vertex(center + (planar * Mathf.Sin(t) + axis * Mathf.Cos(t)) * radius);
                    }

                    GL.End();
                }
            }

            static void RenderArrow(Vector3 from, Vector3 to, Color color, float headSize)
            {
                RenderLine(from, to, color);

                Vector3 dir = (to - from).normalized;
                if (dir == Vector3.zero) return;

                Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.99f
                    ? Vector3.up
                    : Vector3.right;
                Vector3 right = Vector3.Cross(dir, up).normalized;
                Vector3 fwd = Vector3.Cross(right, dir);

                Vector3 tip = to;
                Vector3 base0 = to - dir * headSize + right * headSize * 0.4f;
                Vector3 base1 = to - dir * headSize - right * headSize * 0.4f;
                Vector3 base2 = to - dir * headSize + fwd * headSize * 0.4f;
                Vector3 base3 = to - dir * headSize - fwd * headSize * 0.4f;

                GL.Begin(GL.LINES);
                GL.Color(color);
                GL.Vertex(tip);
                GL.Vertex(base0);
                GL.Vertex(tip);
                GL.Vertex(base1);
                GL.Vertex(tip);
                GL.Vertex(base2);
                GL.Vertex(tip);
                GL.Vertex(base3);
                GL.End();
            }

            static void Edge(Vector3 a, Vector3 b)
            {
                GL.Vertex(a);
                GL.Vertex(b);
            }
        }
    }
#else
    // Fully stripped in release — all calls compile to nothing
    public static class DebugDraw
    {
        public enum Mode { DepthCorrect, AlwaysOnTop, XRay }

        public static void Line(UnityEngine.Vector3 a, UnityEngine.Vector3 b,
            UnityEngine.Color c, float d = 0, Mode m = Mode.DepthCorrect) {}
        public static void Ray(UnityEngine.Vector3 o, UnityEngine.Vector3 dir,
            UnityEngine.Color c, float d = 0, Mode m = Mode.DepthCorrect) {}
        public static void Cross(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0, Mode m = Mode.DepthCorrect) {}
        public static void Circle(UnityEngine.Vector3 p, float r, UnityEngine.Vector3 n,
            UnityEngine.Color c, float d = 0, int seg = 32, Mode m = Mode.DepthCorrect) {}
        public static void Sphere(UnityEngine.Vector3 p, float r,
            UnityEngine.Color c, float d = 0, int seg = 16, Mode m = Mode.DepthCorrect) {}
        public static void WireCube(UnityEngine.Vector3 p, UnityEngine.Vector3 s,
            UnityEngine.Color c, float d = 0, UnityEngine.Quaternion rot = default,
            Mode m = Mode.DepthCorrect) {}
        public static void WireCube(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0, UnityEngine.Quaternion rot = default,
            Mode m = Mode.DepthCorrect) {}
        public static void WireCapsule(UnityEngine.Vector3 a, UnityEngine.Vector3 b,
            float r, UnityEngine.Color c, float d = 0, int seg = 16,
            Mode m = Mode.DepthCorrect) {}
        public static void WireCapsule(UnityEngine.Vector3 p, float h, float r,
            UnityEngine.Color c, float d = 0, int seg = 16, Mode m = Mode.DepthCorrect) {}
        public static void Arrow(UnityEngine.Vector3 f, UnityEngine.Vector3 t,
            UnityEngine.Color c, float hs = 0.25f, float d = 0, Mode m = Mode.DepthCorrect) {}
        public static void Bounds(UnityEngine.Bounds b,
            UnityEngine.Color c, float d = 0, Mode m = Mode.DepthCorrect) {}
        public static void Point(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0, Mode m = Mode.DepthCorrect) {}
    }
#endif
}