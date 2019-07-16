using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TO5.Wires
{
    [Serializable, PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Wires/Grayscale")]
    public sealed class Grayscale : PostProcessEffectSettings
    {
        [Range(0, 1)] public FloatParameter m_Blend = new FloatParameter { value = 0.8f };          // Intensity of greyscale
        public FloatParameter m_PulseSpeed = new FloatParameter { value = 1f };                     // Speed of color pulse

        [NonSerialized] public FloatParameter m_PulseTime = new FloatParameter { value = -1f };     // Time of pulse effect

        // PostProcessEffectSettings Interface
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
                return m_Blend > 0f && m_PulseTime > 0f;

            return false;
        }
    }

    public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
    {
        // PostProcessEffectRenderer Interface
        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(Shader.Find("Wires/PostProcess/Grayscale"));
            sheet.properties.SetFloat("_Blend", settings.m_Blend);
            sheet.properties.SetFloat("_PulseSpeed", settings.m_PulseSpeed);
            sheet.properties.SetFloat("_PulseTime", settings.m_PulseTime);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
