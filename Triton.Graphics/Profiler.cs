using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class Profiler
	{
		public static readonly Common.HashedString GBuffer = "gbuffer";
		public static readonly Common.HashedString Lighting = "lighting";
		public static readonly Common.HashedString AntiAliasing = "anti aliasing";
		public static readonly Common.HashedString LuminanceAdaptation = "luminance adaptation";
		public static readonly Common.HashedString LensFlares = "lensflares";
		public static readonly Common.HashedString Bloom = "bloom";
		public static readonly Common.HashedString Tonemap = "tonemap";

		private const int MaxSections = 32;
		private ProfilerSection[] Sections = new ProfilerSection[MaxSections];
		private int SectionCount = 0;
		private System.Diagnostics.Stopwatch Watch = new System.Diagnostics.Stopwatch();

		public Profiler()
		{
			Watch.Start();
		}

		public void Reset()
		{
			SectionCount = 0;
		}

		public void Begin(int name)
		{
			Sections[SectionCount].Name = name;
			Sections[SectionCount].Start = Watch.ElapsedMilliseconds;
			Sections[SectionCount].End = -1;
			SectionCount++;
		}

		public void End(int name)
		{
			for (var i = 0; i < SectionCount; i++)
			{ 
				if (Sections[i].Name == name)
				{
					Sections[i].End = Watch.ElapsedMilliseconds;
					break;
				}
			}
		}

		public void GetSections(out ProfilerSection[] sections, out int count)
		{
			sections = Sections;
			count = SectionCount;
		}

		public struct ProfilerSection
		{
			public int Name;
			public long Start;
			public long End;
		}
	}
}
