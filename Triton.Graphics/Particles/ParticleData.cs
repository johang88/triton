using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public class ParticleData
    {
        public Vector3[] Position;
        public Vector3[] Velocity;
        public Vector3[] Acceleration;
        public Vector4[] Color;
        public Vector4[] StartColor;
        public Vector4[] EndColor;
        public Vector4[] Time;
        public bool[] Alive;

        public int Count;
        public int AliveCount;

        public ParticleData(int maxCount)
        {
            Generate(maxCount);
        }

        public void Generate(int maxCount)
        {
            Count = maxCount;
            AliveCount = 0;

            Position = new Vector3[maxCount];
            Velocity = new Vector3[maxCount];
            Acceleration = new Vector3[maxCount];
            Color = new Vector4[maxCount];
            StartColor = new Vector4[maxCount];
            EndColor = new Vector4[maxCount];
            Time = new Vector4[maxCount];
            Alive = new bool[maxCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Kill(int id)
        {
            if (AliveCount > 0)
            {
                Alive[id] = false;
                SwapData(id, AliveCount - 1);
                AliveCount--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wake(int id)
        {
            if (AliveCount < Count)
            {
                Alive[id] = true;
                SwapData(id, AliveCount);
                AliveCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SwapData(int a, int b)
        {
            Swap(ref Velocity[a], ref Velocity[b]);
            Swap(ref Acceleration[a], ref Acceleration[b]);
            Swap(ref Position[a], ref Position[b]);
            Swap(ref Color[a], ref Color[b]);
            Swap(ref StartColor[a], ref StartColor[b]);
            Swap(ref EndColor[a], ref EndColor[b]);
            Swap(ref Time[a], ref Time[b]);
            Swap(ref Alive[a], ref Alive[b]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
