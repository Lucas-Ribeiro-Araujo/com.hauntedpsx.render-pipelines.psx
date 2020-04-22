using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    public abstract class BaseShaderGUI : ShaderGUI
    {
        #region EnumsAndClasses

        public enum LightingMode
        {
            LitBaked = 0,
            LitVertexColor,
            LitBakedAndVertexColor,
            Unlit
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            AlphaPostmultiply, // Old school alpha-blending mode.
            AlphaPremultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        protected class Styles
        {
            // Categories
            public static readonly GUIContent SurfaceOptions =
                new GUIContent("Surface Options", "Controls how HPSXRP renders the Material on a screen.");

            public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent LightingMode =
                new GUIContent("Lighting Mode", "Controls how lighting is applied to the material.\nLitBaked: Lighting is sampled from lightmaps and probes.\nLitVertexColor: Lighting is sampled from vertex color data.\nLitBakedAndVertexColor: Lighting from LitBaked mode and LitVertexColor mode is added together.\nUnlit: No lighting is applied.");

            public static readonly GUIContent surfaceType = new GUIContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            public static readonly GUIContent blendingMode = new GUIContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            public static readonly GUIContent cullingText = new GUIContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");

            public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent alphaClippingDitherText = new GUIContent("Alpha Clipping Dither",
                "Makes your Material Cutout use the dither pattern specified in PSXRenderPipelineResources. Use this to create a transparent effect with approximate smooth transparency. The results are noiser than true transparency, but does not suffer from sorting problems.");


            // public static readonly GUIContent alphaClipThresholdText = new GUIContent("Threshold",
            //     "Sets where the Alpha Clipping starts. The higher the value is, the brighter the  effect is when clipping starts.");

            // public static readonly GUIContent receiveShadowText = new GUIContent("Receive Shadows",
            //     "When enabled, other GameObjects can cast shadows onto this GameObject.");

            public static readonly GUIContent mainTex = new GUIContent("Main Tex",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            public static readonly GUIContent emissionTex = new GUIContent("Emission Map",
                "Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.");
        }

        #endregion

        #region Variables

        protected MaterialEditor materialEditor { get; set; }

        protected MaterialProperty lightingModeProp { get; set; }

        protected MaterialProperty surfaceTypeProp { get; set; }

        protected MaterialProperty blendModeProp { get; set; }

        protected MaterialProperty cullingProp { get; set; }

        protected MaterialProperty alphaClipProp { get; set; }

        protected MaterialProperty alphaClippingDitherIsEnabledProp { get; set; }

        // protected MaterialProperty alphaCutoffProp { get; set; }

        // protected MaterialProperty receiveShadowsProp { get; set; }

        // Common Surface Input properties

        protected MaterialProperty mainTexProp { get; set; }

        protected MaterialProperty mainColorProp { get; set; }

        protected MaterialProperty emissionTextureProp { get; set; }

        protected MaterialProperty emissionColorProp { get; set; }

        public bool m_FirstTimeApply = true;

        // Header foldout states

        bool m_SurfaceOptionsFoldout;

        bool m_SurfaceInputsFoldout;

        bool m_AdvancedFoldout;

        #endregion

        ////////////////////////////////////
        // General Functions              //
        ////////////////////////////////////
        #region GeneralFunctions

        public abstract void MaterialChanged(Material material);

        public virtual void FindProperties(MaterialProperty[] properties)
        {
            lightingModeProp = FindProperty("_LightingMode", properties);
            surfaceTypeProp = FindProperty("_Surface", properties);
            blendModeProp = FindProperty("_Blend", properties);
            cullingProp = FindProperty("_Cull", properties);
            alphaClipProp = FindProperty("_AlphaClip", properties);
            alphaClippingDitherIsEnabledProp = FindProperty("_AlphaClippingDitherIsEnabled", properties);
            // alphaCutoffProp = FindProperty("_Cutoff", properties);
            // receiveShadowsProp = FindProperty("_ReceiveShadows", properties, false);
            mainTexProp = FindProperty("_MainTex", properties, false);
            mainColorProp = FindProperty("_MainColor", properties, false);
            emissionTextureProp = FindProperty("_EmissionTexture", properties, false);
            emissionColorProp = FindProperty("_EmissionColor", properties, false);
        }

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a hpsx shader.
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditorIn);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            // Foldout states
            m_SurfaceOptionsFoldout = true;
            m_SurfaceInputsFoldout = true;
            m_AdvancedFoldout = false;

            foreach (var obj in  materialEditor.targets)
                MaterialChanged((Material)obj);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            EditorGUI.BeginChangeCheck();

            m_SurfaceOptionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceOptionsFoldout, Styles.SurfaceOptions);
            if(m_SurfaceOptionsFoldout){
                DrawSurfaceOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            m_SurfaceInputsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceInputsFoldout, Styles.SurfaceInputs);
            if (m_SurfaceInputsFoldout)
            {
                DrawSurfaceInputs(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            m_AdvancedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedFoldout, Styles.AdvancedLabel);
            if (m_AdvancedFoldout)
            {
                DrawAdvancedOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DrawAdditionalFoldouts(material);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in  materialEditor.targets)
                    MaterialChanged((Material)obj);
            }
        }

        #endregion
        ////////////////////////////////////
        // Drawing Functions              //
        ////////////////////////////////////
        #region DrawingFunctions

        public virtual void DrawSurfaceOptions(Material material)
        {
            DoPopup(Styles.LightingMode, lightingModeProp, Enum.GetNames(typeof(LightingMode)));

            DoPopup(Styles.surfaceType, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)));
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
                DoPopup(Styles.blendingMode, blendModeProp, Enum.GetNames(typeof(BlendMode)));

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = cullingProp.hasMixedValue;
            var culling = (RenderFace)cullingProp.floatValue;
            culling = (RenderFace)EditorGUILayout.EnumPopup(Styles.cullingText, culling);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.cullingText.text);
                cullingProp.floatValue = (float)culling;
                material.doubleSidedGI = (RenderFace)cullingProp.floatValue != RenderFace.Front;
            }

            EditorGUI.showMixedValue = false;

            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
            {
                alphaClipProp.floatValue = 0;
                alphaClippingDitherIsEnabledProp.floatValue = 0;
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
                var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
                if (EditorGUI.EndChangeCheck())
                    alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
                EditorGUI.showMixedValue = false;

                if (alphaClipProp.floatValue > 0.5f)
                {
                    EditorGUI.BeginChangeCheck();
                    bool alphaClippingDitherIsEnabled = EditorGUILayout.Toggle(Styles.alphaClippingDitherText, alphaClippingDitherIsEnabledProp.floatValue == 1);
                    if (EditorGUI.EndChangeCheck())
                        alphaClippingDitherIsEnabledProp.floatValue = alphaClippingDitherIsEnabled ? 1 : 0;
                }
            }
            

            // if (receiveShadowsProp != null)
            // {
            //     EditorGUI.BeginChangeCheck();
            //     EditorGUI.showMixedValue = receiveShadowsProp.hasMixedValue;
            //     var receiveShadows =
            //         EditorGUILayout.Toggle(Styles.receiveShadowText, receiveShadowsProp.floatValue == 1.0f);
            //     if (EditorGUI.EndChangeCheck())
            //         receiveShadowsProp.floatValue = receiveShadows ? 1.0f : 0.0f;
            //     EditorGUI.showMixedValue = false;
            // }
        }

        public virtual void DrawSurfaceInputs(Material material)
        {
            DrawMainProperties(material);
        }

        public virtual void DrawAdvancedOptions(Material material)
        {
            materialEditor.EnableInstancingField();
        }

        public virtual void DrawAdditionalFoldouts(Material material){}

        public virtual void DrawMainProperties(Material material)
        {
            if (mainTexProp != null && mainColorProp != null) // Draw the mainTex, most shader will have at least a mainTex
            {
                materialEditor.TexturePropertySingleLine(Styles.mainTex, mainTexProp, mainColorProp);
                // TODO Temporary fix for lightmapping, to be replaced with attribute tag.
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", mainTexProp.textureValue);
                    var mainTexTiling = mainTexProp.textureScaleAndOffset;
                    material.SetTextureScale("_MainTex", new Vector2(mainTexTiling.x, mainTexTiling.y));
                    material.SetTextureOffset("_MainTex", new Vector2(mainTexTiling.z, mainTexTiling.w));
                }
            }
        }

        protected virtual void DrawEmissionProperties(Material material, bool keyword)
        {
            var emissive = true;
            var hadEmissionTexture = emissionTextureProp.textureValue != null;

            if (!keyword)
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionTex, emissionTextureProp, emissionColorProp,
                    false);
            }
            else
            {
                // Emission for GI?
                emissive = materialEditor.EmissionEnabledProperty();

                EditorGUI.BeginDisabledGroup(!emissive);
                {
                    // Texture and HDR color controls
                    materialEditor.TexturePropertyWithHDRColor(Styles.emissionTex, emissionTextureProp,
                        emissionColorProp,
                        false);
                }
                EditorGUI.EndDisabledGroup();
            }

            // If texture was assigned and color was black set color to white
            var brightness = emissionColorProp.colorValue.maxColorComponent;
            if (emissionTextureProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColorProp.colorValue = Color.white;

            // HPSXRP does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
            if (emissive)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        protected static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty textureProp)
        {
            materialEditor.TextureScaleOffsetProperty(textureProp);
        }

        #endregion
        ////////////////////////////////////
        // Material Data Functions        //
        ////////////////////////////////////
        #region MaterialDataFunctions

        public static void SetMaterialKeywords(Material material, Action<Material> shadingModelFunc = null, Action<Material> shaderFunc = null)
        {
            // Clear all keywords for fresh start
            material.shaderKeywords = null;

            SetupMaterialLightingMode(material);
            // Setup blending
            SetupMaterialBlendMode(material);
            // Receive Shadows
            // if(material.HasProperty("_ReceiveShadows"))
            //     CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
            // Emission
            if (material.HasProperty("_EmissiveColor"))
                MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled =
                (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            if (material.HasProperty("_EmissionEnabled") && !shouldEmissionBeEnabled)
                shouldEmissionBeEnabled = material.GetFloat("_EmissionEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
            // Shader specific keyword functions
            shadingModelFunc?.Invoke(material);
            shaderFunc?.Invoke(material);
        }

        public static void SetupMaterialLightingMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
                
                LightingMode lightingMode = (LightingMode)material.GetFloat("_LightingMode");

                switch (lightingMode)
                {
                    case LightingMode.LitBaked:
                        material.EnableKeyword("_LIGHTING_BAKED_ON");
                        material.DisableKeyword("_LIGHTING_VERTEX_COLOR_ON");
                        break;
                    case LightingMode.LitVertexColor:
                        material.DisableKeyword("_LIGHTING_BAKED_ON");
                        material.EnableKeyword("_LIGHTING_VERTEX_COLOR_ON");
                        break;
                    case LightingMode.LitBakedAndVertexColor:
                        material.EnableKeyword("_LIGHTING_BAKED_ON");
                        material.EnableKeyword("_LIGHTING_VERTEX_COLOR_ON");
                        break;
                    case LightingMode.Unlit:
                        material.DisableKeyword("_LIGHTING_BAKED_ON");
                        material.DisableKeyword("_LIGHTING_VERTEX_COLOR_ON");
                        break;
                }   
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = material.GetFloat("_AlphaClip") == 1;
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                }
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                // material.SetShaderPassEnabled("ShadowCaster", true);
            }
            else // SurfaceType == SurfaceType.Transparent
            {
                BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
                
                // Specific Transparent Mode Settings
                switch (blendMode)
                {
                    case BlendMode.AlphaPostmultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.AlphaPremultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Additive:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                }
                // General Transparent Material Settings
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                // material.SetShaderPassEnabled("ShadowCaster", false);
            }
        }

        #endregion
        ////////////////////////////////////
        // Helper Functions               //
        ////////////////////////////////////
        #region HelperFunctions

        public static void TwoFloatSingleLine(GUIContent title, MaterialProperty prop1, GUIContent prop1Label,
            MaterialProperty prop2, GUIContent prop2Label, MaterialEditor materialEditor, float labelWidth = 30f)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop1.hasMixedValue || prop2.hasMixedValue;
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.PrefixLabel(rect, title);
            var indent = EditorGUI.indentLevel;
            var preLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = labelWidth;
            Rect propRect1 = new Rect(rect.x + preLabelWidth, rect.y,
                (rect.width - preLabelWidth) * 0.5f, EditorGUIUtility.singleLineHeight);
            var prop1val = EditorGUI.FloatField(propRect1, prop1Label, prop1.floatValue);

            Rect propRect2 = new Rect(propRect1.x + propRect1.width, rect.y,
                propRect1.width, EditorGUIUtility.singleLineHeight);
            var prop2val = EditorGUI.FloatField(propRect2, prop2Label, prop2.floatValue);

            EditorGUI.indentLevel = indent;
            EditorGUIUtility.labelWidth = preLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(title.text);
                prop1.floatValue = prop1val;
                prop2.floatValue = prop2val;
            }

            EditorGUI.showMixedValue = false;
        }

        public void DoPopup(GUIContent label, MaterialProperty property, string[] options)
        {
            DoPopup(label, property, options, materialEditor);
        }

        public static void DoPopup(GUIContent label, MaterialProperty property, string[] options, MaterialEditor materialEditor)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            EditorGUI.showMixedValue = property.hasMixedValue;

            var mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(label, (int)mode, options);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = mode;
            }

            EditorGUI.showMixedValue = false;
        }

        // Helper to show texture and color properties
        public static Rect TextureColorProps(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty colorProp, bool hdr = false)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            materialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;

            if (colorProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = colorProp.hasMixedValue;
                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect rectAfterLabel = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                    EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
                var col = EditorGUI.ColorField(rectAfterLabel, GUIContent.none, colorProp.colorValue, true,
                    false, hdr);
                EditorGUI.indentLevel = indentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(colorProp.displayName);
                    colorProp.colorValue = col;
                }
                EditorGUI.showMixedValue = false;
            }

            return rect;
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others

        public new static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            return FindProperty(propertyName, properties, true);
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others

        public new static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + (object) properties.Length);
            return null;
        }

        #endregion
    }
}