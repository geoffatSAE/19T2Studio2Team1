using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TO5.Wires
{
    [Serializable, PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Wires/Grayscale")]
    public sealed class Grayscale : PostProcessEffectSettings
    {
        [Range(0, 1)] public FloatParameter m_Blend = new FloatParameter { value = 0.8f };      // Intensity of greyscale
        public FloatParameter m_PulseSpeed = new FloatParameter { value = 1f };                 // Speed of color pulse

        // PostProcessEffectSettings Interface
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value && m_Blend > 0f;
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

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
