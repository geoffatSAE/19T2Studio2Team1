using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TO5.Wires
{
    [Serializable, PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Wires/Grayscale")]
    public sealed class Grayscale : PostProcessEffectSettings
    {
        [Range(0, 1)] public FloatParameter blend = new FloatParameter { value = 0.8f };            // Intensity of greyscale

        // PostProcessEffectSettings Interface
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
                return blend > 0f;

            return false;
        }
    }

    public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
    {
        private Shader m_Shader;            // Cached grayscale shader

        public GrayscaleRenderer() : base()
        {
            m_Shader = Shader.Find("Wires/PostProcess/Grayscale");
        }

        // PostProcessEffectRenderer Interface
        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(m_Shader);
            sheet.properties.SetFloat("_Blend", settings.blend);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
