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
    ///   DebugDraw.Line(a, b, Color.red);
    ///   DebugDraw.Arrow(from, to, Color.white, 0.25f);
    ///   DebugDraw.Sphere(pos, 0.5f, Color.green, 2f);
    ///   DebugDraw.Capsule(bottom, top, 0.5f, Color.cyan);
    ///   DebugDraw.Square(center, 1f, Vector3.up, Color.yellow);
    ///   DebugDraw.Triangle(a, b, c, Color.red);
    ///   DebugDraw.Cube(center, size, Color.white);
    ///   DebugDraw.Bounds(bounds, Color.green);
    ///   DebugDraw.Circle(center, 0.5f, Vector3.up, Color.blue);
    ///
    /// No setup required — bootstraps itself on first call.
    /// Add DEBUG_DRAW to Scripting Define Symbols to run in release builds.
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_DRAW
    public static class DebugDraw
    {
        // ─── Depth Mode ───────────────────────────────────────────────────────────

        /// <summary>Controls how a drawn shape interacts with scene depth.</summary>
        public enum Mode
        {
            /// <summary>Occluded by geometry — correct depth sorting. Default.</summary>
            DepthCorrect,
            /// <summary>Always renders on top of everything, ignoring depth.</summary>
            AlwaysOnTop,
            /// <summary>Full opacity where visible, dimmed where occluded.</summary>
            XRay,
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        // ── Primitives ────────────────────────────────────────────────────────────

        public static void Line(Vector3 start, Vector3 end, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Enqueue(CommandType.Line, color, duration, mode, start, end);

        public static void Ray(Vector3 origin, Vector3 direction, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Line(origin, origin + direction, color, duration, mode);

        /// <param name="size">Arm length of each axis line.</param>
        public static void Cross(Vector3 center, float size, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
        {
            float h = size * 0.5f;
            Line(center - Vector3.right   * h, center + Vector3.right   * h, color, duration, mode);
            Line(center - Vector3.up      * h, center + Vector3.up      * h, color, duration, mode);
            Line(center - Vector3.forward * h, center + Vector3.forward * h, color, duration, mode);
        }

        public static void Point(Vector3 position, float size, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Cross(position, size, color, duration, mode);

        // ── Flat shapes ───────────────────────────────────────────────────────────

        /// <param name="normal">Axis the circle faces toward.</param>
        /// <param name="segments">Number of line segments (smoothness).</param>
        public static void Circle(Vector3 center, float radius, Vector3 normal, Color color,
            float duration = 0f, int segments = 32, Mode mode = Mode.DepthCorrect)
            => Enqueue(CommandType.Circle, color, duration, mode, center, normal,
                radius: radius, segments: segments);

        /// <param name="normal">Axis the square faces toward.</param>
        /// <param name="rotation">Roll rotation around the normal in degrees.</param>
        public static void Square(Vector3 center, float size, Vector3 normal, Color color,
            float duration = 0f, float rotation = 0f, Mode mode = Mode.DepthCorrect)
            => Enqueue(CommandType.Square, color, duration, mode, center, normal,
                radius: size * 0.5f, roll: rotation);

        /// <summary>Draws a wire triangle from three explicit world-space corners.</summary>
        public static void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
        {
            Line(a, b, color, duration, mode);
            Line(b, c, color, duration, mode);
            Line(c, a, color, duration, mode);
        }

        // ── 3-D shapes ────────────────────────────────────────────────────────────

        /// <param name="segments">Latitude resolution per circle.</param>
        /// <param name="meridians">Extra longitude arcs (0 = only 3 axis circles).</param>
        /// <param name="rotation">Orientation of the sphere's axis circles and meridians.</param>
        public static void Sphere(Vector3 center, float radius, Color color,
            float duration = 0f, int segments = 16, int meridians = 0,
            Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Enqueue(CommandType.Sphere, color, duration, mode, center, Vector3.up,
                radius: radius, segments: segments, meridians: meridians, rotation: rotation);
        }

        /// <param name="start">Center of the bottom sphere.</param>
        /// <param name="end">Center of the top sphere.</param>
        /// <param name="segments">Resolution of each sphere and connecting rings.</param>
        /// <param name="meridians">Extra meridian arcs on each end sphere.</param>
        public static void Capsule(Vector3 start, Vector3 end, float radius, Color color,
            float duration = 0f, int segments = 16, int meridians = 0,
            Mode mode = Mode.DepthCorrect)
        {
            // Derive rotation from the start→end axis so sphere circles align with the capsule body
            Vector3 axis = end - start;
            Quaternion rotation = axis != Vector3.zero
                ? Quaternion.FromToRotation(Vector3.up, axis.normalized)
                : Quaternion.identity;
            Enqueue(CommandType.Capsule, color, duration, mode, start, end,
                radius: radius, segments: segments, meridians: meridians, rotation: rotation);
        }

        /// <param name="center">World-space center.</param>
        /// <param name="height">Total height (tip to tip).</param>
        /// <param name="radius">Radius of the cylindrical body.</param>
        /// <param name="rotation">Orientation of the capsule — default is along world Y.</param>
        public static void Capsule(Vector3 center, float height, float radius, Color color,
            float duration = 0f, int segments = 16, int meridians = 0,
            Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
        {
            if (rotation == default) rotation = Quaternion.identity;
            float half = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 offset = rotation * Vector3.up * half;
            Capsule(center - offset, center + offset, radius, color, duration, segments, meridians, mode);
        }

        /// <param name="rotation">Orientation of the box.</param>
        public static void Cube(Vector3 center, Vector3 size, Color color,
            float duration = 0f, Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Enqueue(CommandType.Cube, color, duration, mode, center, Vector3.zero,
                size: size, rotation: rotation);
        }

        public static void Cube(Vector3 center, float size, Color color,
            float duration = 0f, Quaternion rotation = default, Mode mode = Mode.DepthCorrect)
            => Cube(center, Vector3.one * size, color, duration, rotation, mode);

        public static void Bounds(Bounds bounds, Color color,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Cube(bounds.center, bounds.size, color, duration, default, mode);

        /// <param name="headSize">Length of the arrowhead lines.</param>
        /// <param name="headAngle">Half-angle of the arrowhead cone in degrees.</param>
        public static void Arrow(Vector3 from, Vector3 to, Color color,
            float headSize = 0.25f, float headAngle = 20f,
            float duration = 0f, Mode mode = Mode.DepthCorrect)
            => Enqueue(CommandType.Arrow, color, duration, mode, from, to,
                radius: headSize, roll: headAngle);

        // ─── Internals ────────────────────────────────────────────────────────────

        enum CommandType { Line, Circle, Square, Sphere, Capsule, Cube, Arrow }

        struct DrawCommand
        {
            public CommandType Type;
            public float       ExpiresAt;
            public Color       Color;
            public Mode        DepthMode;
            public Vector3     A, B;        // start/end or center/normal
            public Vector3     Size;
            public Quaternion  Rotation;
            public float       Radius;
            public float       Roll;        // degrees — reused for arrow head angle too
            public int         Segments;
            public int         Meridians;
        }

        static void Enqueue(CommandType type, Color color, float duration, Mode mode,
            Vector3 a, Vector3 b,
            Vector3 size = default, Quaternion rotation = default,
            float radius = 0f, float roll = 0f,
            int segments = 16, int meridians = 0)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Renderer.Instance.Enqueue(new DrawCommand
            {
                Type      = type,
                ExpiresAt = Time.time + duration,
                Color     = color,
                DepthMode = mode,
                A         = a,
                B         = b,
                Size      = size == default ? Vector3.one : size,
                Rotation  = rotation,
                Radius    = radius,
                Roll      = roll,
                Segments  = segments,
                Meridians = meridians,
            });
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
                    var go = new GameObject("[DebugDraw]") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Renderer>();
                    return _instance;
                }
            }

            const float XRayOccludedAlpha = 0.15f;

            Material           _mat;
            List<DrawCommand>  _persistent  = new(64);
            List<DrawCommand>  _oneFrame    = new(256);
            List<DrawCommand>  _swap        = new(256);
            List<DrawCommand>  _bucketDepth = new(128);
            List<DrawCommand>  _bucketOnTop = new(64);
            List<DrawCommand>  _bucketXRay  = new(64);

            void Awake()
            {
                _mat = new Material(Shader.Find("Hidden/Internal-Colored"))
                    { hideFlags = HideFlags.HideAndDontSave };
                _mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _mat.SetInt("_Cull",     (int)CullMode.Off);
                _mat.SetInt("_ZWrite",   0);
            }

            public void Enqueue(DrawCommand cmd) =>
                (cmd.ExpiresAt <= Time.time ? _oneFrame : _persistent).Add(cmd);

            void LateUpdate()
            {
                float now = Time.time;
                _swap.Clear();
                foreach (var cmd in _persistent)
                    if (cmd.ExpiresAt > now) _swap.Add(cmd);
                (_persistent, _swap) = (_swap, _persistent);
            }

            void OnRenderObject()
            {
                if (_mat == null) return;

                _bucketDepth.Clear(); _bucketOnTop.Clear(); _bucketXRay.Clear();
                Bucket(_persistent);  Bucket(_oneFrame);

                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.identity);

                if (_bucketDepth.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketDepth) Render(cmd, 1f);
                }

                if (_bucketOnTop.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.Always);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketOnTop) Render(cmd, 1f);
                }

                if (_bucketXRay.Count > 0)
                {
                    _mat.SetInt("_ZTest", (int)CompareFunction.Greater);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketXRay) Render(cmd, XRayOccludedAlpha);

                    _mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    _mat.SetPass(0);
                    foreach (var cmd in _bucketXRay) Render(cmd, 1f);
                }

                GL.PopMatrix();
                _oneFrame.Clear();
            }

            void Bucket(List<DrawCommand> src)
            {
                foreach (var cmd in src)
                    switch (cmd.DepthMode)
                    {
                        case Mode.DepthCorrect: _bucketDepth.Add(cmd); break;
                        case Mode.AlwaysOnTop:  _bucketOnTop.Add(cmd); break;
                        case Mode.XRay:         _bucketXRay.Add(cmd);  break;
                    }
            }

            void Render(in DrawCommand cmd, float alpha)
            {
                Color c = cmd.Color; c.a *= alpha;
                switch (cmd.Type)
                {
                    case CommandType.Line:    DrawLine(cmd.A, cmd.B, c); break;
                    case CommandType.Circle:  DrawCircle(cmd.A, cmd.B, cmd.Radius, c, cmd.Segments); break;
                    case CommandType.Square:  DrawSquare(cmd.A, cmd.B, cmd.Radius, cmd.Roll, c); break;
                    case CommandType.Sphere:  DrawSphere(cmd.A, cmd.Radius, c, cmd.Segments, cmd.Meridians, cmd.Rotation); break;
                    case CommandType.Capsule: DrawCapsule(cmd.A, cmd.B, cmd.Radius, c, cmd.Segments, cmd.Meridians, cmd.Rotation); break;
                    case CommandType.Cube:    DrawCube(cmd.A, cmd.Size, cmd.Rotation, c); break;
                    case CommandType.Arrow:   DrawArrow(cmd.A, cmd.B, cmd.Radius, cmd.Roll, c); break;
                }
            }

            // ── Primitive helpers ─────────────────────────────────────────────────

            // Returns two orthogonal tangents for a given normal/axis vector
            static void Tangents(Vector3 normal, out Vector3 tangA, out Vector3 tangB)
            {
                Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f
                    ? Vector3.up : Vector3.right;
                tangA = Vector3.Cross(normal, up).normalized;
                tangB = Vector3.Cross(normal, tangA);
            }

            static void DrawLine(Vector3 a, Vector3 b, Color c)
            {
                GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(a); GL.Vertex(b);
                GL.End();
            }

            static void DrawCircle(Vector3 center, Vector3 normal, float radius,
                Color color, int segments)
            {
                Tangents(normal, out var tA, out var tB);
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                float step = Mathf.PI * 2f / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float a = step * i;
                    GL.Vertex(center + (tA * Mathf.Cos(a) + tB * Mathf.Sin(a)) * radius);
                }
                GL.End();
            }

            static void DrawSquare(Vector3 center, Vector3 normal, float halfSize,
                float rollDeg, Color color)
            {
                Tangents(normal, out var tA, out var tB);

                // Apply roll rotation around the normal
                float roll = rollDeg * Mathf.Deg2Rad;
                float cr = Mathf.Cos(roll), sr = Mathf.Sin(roll);
                Vector3 right = tA * cr + tB * sr;
                Vector3 fwd   = tA * -sr + tB * cr;

                Vector3 c0 = center + (-right + fwd)  * halfSize;
                Vector3 c1 = center + ( right + fwd)  * halfSize;
                Vector3 c2 = center + ( right - fwd)  * halfSize;
                Vector3 c3 = center + (-right - fwd)  * halfSize;

                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(c0); GL.Vertex(c1); GL.Vertex(c2);
                GL.Vertex(c3); GL.Vertex(c0);
                GL.End();
            }

            // rotation rotates all circle normals so the sphere's equator/poles align with any axis
            static void DrawSphere(Vector3 center, float radius, Color color,
                int segments, int meridians, Quaternion rotation)
            {
                DrawCircle(center, rotation * Vector3.up,      radius, color, segments);
                DrawCircle(center, rotation * Vector3.right,   radius, color, segments);
                DrawCircle(center, rotation * Vector3.forward, radius, color, segments);

                for (int i = 0; i < meridians; i++)
                {
                    float   angle  = Mathf.PI * i / meridians;
                    Vector3 normal = rotation * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    DrawCircle(center, normal, radius, color, segments);
                }
            }

            static void DrawCapsule(Vector3 bottom, Vector3 top, float radius, Color color,
                int segments, int meridians, Quaternion rotation)
            {
                // Two full spheres — circles aligned to the capsule axis via rotation
                DrawSphere(top,    radius, color, segments, meridians, rotation);
                DrawSphere(bottom, radius, color, segments, meridians, rotation);

                // Four connecting lines along the body
                Vector3 axis = (top - bottom).normalized;
                if (axis == Vector3.zero) axis = rotation * Vector3.up;
                Tangents(axis, out var tA, out var tB);

                GL.Begin(GL.LINES);
                GL.Color(color);
                for (int i = 0; i < 4; i++)
                {
                    float   a   = Mathf.PI * 0.5f * i;
                    Vector3 off = (tA * Mathf.Cos(a) + tB * Mathf.Sin(a)) * radius;
                    GL.Vertex(top    + off);
                    GL.Vertex(bottom + off);
                }
                GL.End();
            }

            static void DrawCube(Vector3 center, Vector3 size, Quaternion rot, Color color)
            {
                Vector3 h  = size * 0.5f;
                // Bottom ring (–Y), top ring (+Y)
                Vector3 c0 = center + rot * new Vector3(-h.x, -h.y, -h.z);
                Vector3 c1 = center + rot * new Vector3( h.x, -h.y, -h.z);
                Vector3 c2 = center + rot * new Vector3( h.x, -h.y,  h.z);
                Vector3 c3 = center + rot * new Vector3(-h.x, -h.y,  h.z);
                Vector3 c4 = center + rot * new Vector3(-h.x,  h.y, -h.z);
                Vector3 c5 = center + rot * new Vector3( h.x,  h.y, -h.z);
                Vector3 c6 = center + rot * new Vector3( h.x,  h.y,  h.z);
                Vector3 c7 = center + rot * new Vector3(-h.x,  h.y,  h.z);

                GL.Begin(GL.LINES);
                GL.Color(color);
                // Bottom face
                GL.Vertex(c0); GL.Vertex(c1);
                GL.Vertex(c1); GL.Vertex(c2);
                GL.Vertex(c2); GL.Vertex(c3);
                GL.Vertex(c3); GL.Vertex(c0);
                // Top face
                GL.Vertex(c4); GL.Vertex(c5);
                GL.Vertex(c5); GL.Vertex(c6);
                GL.Vertex(c6); GL.Vertex(c7);
                GL.Vertex(c7); GL.Vertex(c4);
                // Verticals
                GL.Vertex(c0); GL.Vertex(c4);
                GL.Vertex(c1); GL.Vertex(c5);
                GL.Vertex(c2); GL.Vertex(c6);
                GL.Vertex(c3); GL.Vertex(c7);
                GL.End();
            }

            static void DrawArrow(Vector3 from, Vector3 to, float headSize,
                float headAngleDeg, Color color)
            {
                DrawLine(from, to, color);

                Vector3 dir = (to - from).normalized;
                if (dir == Vector3.zero) return;

                Tangents(dir, out var tA, out var tB);

                // Build 4 head lines evenly distributed around the shaft,
                // each angled inward by headAngleDeg from the shaft direction.
                float   rad    = headSize * Mathf.Tan(headAngleDeg * Mathf.Deg2Rad);
                Vector3 tip    = to;
                Vector3 shaft  = tip - dir * headSize; // base center of the head cone

                GL.Begin(GL.LINES);
                GL.Color(color);
                for (int i = 0; i < 4; i++)
                {
                    float   a      = Mathf.PI * 0.5f * i;
                    Vector3 offset = (tA * Mathf.Cos(a) + tB * Mathf.Sin(a)) * rad;

                    Vector3 baseVert = shaft + offset;

                    // Shaft → base vertex (the 4 slanted lines)
                    GL.Vertex(tip);
                    GL.Vertex(baseVert);
                }

                // Wire square at the base of the head to close the arrowhead visually
                Vector3 b0 = shaft + ( tA + tB) * rad;
                Vector3 b1 = shaft + (-tA + tB) * rad;
                Vector3 b2 = shaft + (-tA - tB) * rad;
                Vector3 b3 = shaft + ( tA - tB) * rad;
                GL.Vertex(b0); GL.Vertex(b1);
                GL.Vertex(b1); GL.Vertex(b2);
                GL.Vertex(b2); GL.Vertex(b3);
                GL.Vertex(b3); GL.Vertex(b0);
                GL.End();
            }
        }
    }

#else
    // Fully stripped in release — all calls compile to nothing
    public static class DebugDraw
    {
        public enum Mode { DepthCorrect, AlwaysOnTop, XRay }

        public static void Line(UnityEngine.Vector3 a, UnityEngine.Vector3 b,
            UnityEngine.Color c, float d = 0f, Mode m = default) {}
        public static void Ray(UnityEngine.Vector3 o, UnityEngine.Vector3 dir,
            UnityEngine.Color c, float d = 0f, Mode m = default) {}
        public static void Cross(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0f, Mode m = default) {}
        public static void Point(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0f, Mode m = default) {}

        public static void Circle(UnityEngine.Vector3 p, float r, UnityEngine.Vector3 n,
            UnityEngine.Color c, float d = 0f, int seg = 32, Mode m = default) {}
        public static void Square(UnityEngine.Vector3 p, float s, UnityEngine.Vector3 n,
            UnityEngine.Color c, float d = 0f, float roll = 0f, Mode m = default) {}
        public static void Triangle(UnityEngine.Vector3 a, UnityEngine.Vector3 b,
            UnityEngine.Vector3 cv, UnityEngine.Color c, float d = 0f, Mode m = default) {}

        public static void Sphere(UnityEngine.Vector3 p, float r,
            UnityEngine.Color c, float d = 0f, int seg = 16, int mer = 0,
            UnityEngine.Quaternion rot = default, Mode m = default) {}
        public static void Capsule(UnityEngine.Vector3 a, UnityEngine.Vector3 b,
            float r, UnityEngine.Color c, float d = 0f, int seg = 16, int mer = 0,
            Mode m = default) {}
        public static void Capsule(UnityEngine.Vector3 p, float h, float r,
            UnityEngine.Color c, float d = 0f, int seg = 16, int mer = 0,
            UnityEngine.Quaternion rot = default, Mode m = default) {}
        public static void Cube(UnityEngine.Vector3 p, UnityEngine.Vector3 s,
            UnityEngine.Color c, float d = 0f, UnityEngine.Quaternion rot = default, Mode m = default) {}
        public static void Cube(UnityEngine.Vector3 p, float s,
            UnityEngine.Color c, float d = 0f, UnityEngine.Quaternion rot = default, Mode m = default) {}
        public static void Bounds(UnityEngine.Bounds b,
            UnityEngine.Color c, float d = 0f, Mode m = default) {}
        public static void Arrow(UnityEngine.Vector3 f, UnityEngine.Vector3 t,
            UnityEngine.Color c, float hs = 0.25f, float ha = 20f, float d = 0f, Mode m = default) {}
    }
#endif
}