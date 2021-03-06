﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFileSystem.FileSystems;
using SharpFileSystem;
using ImGuiNET;
using Triton.Graphics.Deferred;
using Triton.Graphics.Post;

namespace Triton.Samples
{
    public abstract class BaseGame : Triton.Game.Game
    {
        protected Random RNG = new Random();

        public BaseGame(string name)
            : base(name)
        {
            RequestedWidth = 1280;
            RequestedHeight = 720;
            ResolutionScale = 1.0f;
        }

        protected KeyValuePair<FileSystemPath, IFileSystem> Mount(string mountPoint, IFileSystem fileSystem)
        {
            return new KeyValuePair<FileSystemPath, IFileSystem>(FileSystemPath.Parse(mountPoint), fileSystem);
        }

        protected override SharpFileSystem.IFileSystem MountFileSystem()
        {
            return new FileSystemMounter(
                Mount("/tmp/", new PhysicalFileSystem("./tmp")),
                Mount("/", new MergedFileSystem(
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../../Data/core_data/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../../Data/samples_data/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../../Data/no_dist/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../../Data/generated/"))
                    ))
                );
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 190));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, RequestedHeight - 310));
            ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            int selectedValue = (int)DeferredRenderer.Settings.ShadowQuality;
            var values = Enum.GetNames(typeof(ShadowQuality));
            if (ImGui.Combo("Shadow quality", ref selectedValue, values, values.Length))
            {
                DeferredRenderer.Settings.ShadowQuality = (ShadowQuality)selectedValue;
            }

            ImGui.Checkbox("Shadows", ref DeferredRenderer.Settings.EnableShadows);

            selectedValue = (int)PostEffectManager.AntiAliasing;
            values = Enum.GetNames(typeof(AntiAliasing));
            if (ImGui.Combo("Anti aliasing", ref selectedValue, values, values.Length))
            {
                PostEffectManager.AntiAliasing = (AntiAliasing)selectedValue;
            }

            selectedValue = (int)PostEffectManager.AntiAliasingQuality;
            values = Enum.GetNames(typeof(AntiAliasingQuality));
            if (ImGui.Combo("AA Quality", ref selectedValue, values, values.Length))
            {
                PostEffectManager.AntiAliasingQuality = (AntiAliasingQuality)selectedValue;
            }

            ImGui.Checkbox("Bloom", ref PostEffectManager.HDRSettings.EnableBloom);
            ImGui.SliderFloat("Bloom Threshold", ref PostEffectManager.HDRSettings.BloomThreshold, 0.5f, 20.0f, PostEffectManager.HDRSettings.BloomThreshold.ToString(), 1.0f);
            ImGui.SliderFloat("Bloom Strength", ref PostEffectManager.HDRSettings.BloomStrength, 1f, 20.0f, PostEffectManager.HDRSettings.BloomStrength.ToString(), 1.0f);
            ImGui.SliderFloat("Eye adaption rate", ref PostEffectManager.HDRSettings.AdaptationRate, 0.1f, 10.0f, PostEffectManager.HDRSettings.AdaptationRate.ToString(), 1.0f);

            selectedValue = (int)PostEffectManager.HDRSettings.TonemapOperator;
            values = Enum.GetNames(typeof(TonemapOperator));
            if (ImGui.Combo("Tonemap operator", ref selectedValue, values, values.Length))
            {
                PostEffectManager.HDRSettings.TonemapOperator = (TonemapOperator)selectedValue;
            }

            ImGui.Checkbox("Auto Key", ref PostEffectManager.HDRSettings.AutoKey);
            ImGui.SliderFloat("Key Value", ref PostEffectManager.HDRSettings.KeyValue, 0.001f, 1.0f, PostEffectManager.HDRSettings.KeyValue.ToString(), 1.0f);

            ImGui.End();

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 100));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, RequestedHeight - 110));
            ImGui.Begin("Debug Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            bool isChecked = (DebugFlags & Game.DebugFlags.Physics) == Game.DebugFlags.Physics;
            if (ImGui.Checkbox("Physics visualizer", ref isChecked))
            {
                DebugFlags ^= Game.DebugFlags.Physics;
            }

            selectedValue = (int)PostEffectManager.VisualizationMode;
            values = Enum.GetNames(typeof(VisualizationMode));
            if (ImGui.Combo("Visualize", ref selectedValue, values, values.Length))
            {
                PostEffectManager.VisualizationMode = (VisualizationMode)selectedValue;
            }

            ImGui.End();
        }
    }
}
