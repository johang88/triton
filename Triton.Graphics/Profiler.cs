using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Triton.Utility;

namespace Triton.Graphics
{
    public class Profiler : IDisposable
    {
        public static readonly HashedString Total = "total";
        public static readonly HashedString GBuffer = "gbuffer";
        public static readonly HashedString Lighting = "lighting";
        public static readonly HashedString ShadowsGeneration = "shadow generation (csm)";
        public static readonly HashedString ShadowsRender = "shadow render (csm)";
        public static readonly HashedString ShadowRenderPointSpot = "shadow render (point / spot)";
        public static readonly HashedString TiledLights = "tiled lights";
        public static readonly HashedString DirectionaLight = "directional lights";
        public static readonly HashedString SSAO = "ssao";
        public static readonly HashedString Post = "post";
        public static readonly HashedString AntiAliasing = "anti aliasing";
        public static readonly HashedString LuminanceAdaptation = "luminance adaptation";
        public static readonly HashedString LensFlares = "lensflares";
        public static readonly HashedString Bloom = "bloom";
        public static readonly HashedString Tonemap = "tonemap";
        public static readonly HashedString AtmosphericScattering = "atmospheric scattering";

        private const int MaxSections = 32;
        private readonly ProfilerSection[] Sections = new ProfilerSection[MaxSections];
        private int _sectionCount = 0;
        private uint _lastHandle = 0;
        private int _depth = 0;

        public Profiler()
        {
            for (var i = 0; i < Sections.Length; i++)
            {
                GL.GenQueries(1, out Sections[i].StartHandle);
                GL.GenQueries(1, out Sections[i].StopHandle);
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < Sections.Length; i++)
            {
                GL.DeleteQuery(Sections[i].StartHandle);
                GL.DeleteQuery(Sections[i].StopHandle);
            }
        }

        public void Reset()
        {
            _sectionCount = 0;
        }

        public void Begin(int name)
        {
            Sections[_sectionCount].Name = name;
            Sections[_sectionCount].Depth = _depth++;
            GL.QueryCounter(Sections[_sectionCount].StartHandle, QueryCounterTarget.Timestamp);

            _sectionCount++;
        }

        public void End(int name)
        {
            _depth--;

            for (var i = 0; i < _sectionCount; i++)
            {
                if (Sections[i].Name == name)
                {
                    GL.QueryCounter(Sections[i].StopHandle, QueryCounterTarget.Timestamp);
                    _lastHandle = Sections[i].StopHandle;
                    break;
                }
            }
        }

        public void Collect()
        {
            if (_sectionCount == 0)
                return;

            //int stopTimerAvailable = 0;
            //while (stopTimerAvailable == 0)
            //{
            //	GL.GetQueryObject(_lastHandle, GetQueryObjectParam.QueryResultAvailable, out stopTimerAvailable);
            //}

            for (var i = 0; i < _sectionCount; i++)
            {
                GL.GetQueryObject(Sections[i].StartHandle, GetQueryObjectParam.QueryResult, out Sections[i].Start);
                GL.GetQueryObject(Sections[i].StopHandle, GetQueryObjectParam.QueryResult, out Sections[i].End);

                Sections[i].ElapsedMs = (Sections[i].End - Sections[i].Start) / 1000000.0;
            }
        }

        public void GetSections(out ProfilerSection[] sections, out int count)
        {
            sections = Sections;
            count = _sectionCount;
        }

        public struct ProfilerSection
        {
            public int Name;
            public int Depth;
            public uint StartHandle;
            public uint StopHandle;
            public long Start;
            public long End;
            public double ElapsedMs;
        }
    }
}
