﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post
{
    public class HDRSettings
    {
        public float KeyValue;
        public bool AutoKey;
        public float AdaptationRate;
        public float BlurSigma;
        public float BloomThreshold;
        public float BloomStrength;

        public bool EnableBloom;

        public TonemapOperator TonemapOperator;
    }

    public enum TonemapOperator
    {
        Reinhard,
        Uncharted,
        UnchartedVar1,
        UnchartedVar2,
        FilmicALU,
        ASEC
    }
}
