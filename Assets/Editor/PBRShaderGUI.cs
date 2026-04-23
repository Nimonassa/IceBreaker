using UnityEditor;
using UnityEngine;

public class PBRShaderGUI : ShaderGUI
{
    private int selectedTab = 0;

    // Foldout states for all tabs
    private bool foldoutAlbedo = true;
    private bool foldoutAO = true;
    private bool foldoutNormal = true;
    private bool foldoutHeight = true;
    private bool foldoutRoughness = true;
    private bool foldoutSpecular = true;
    private bool foldoutFresnel = true;
    private bool foldoutTransparency = true;
    private bool foldoutSettings = true;

    private readonly string[] tabs = {
        "Color / Albedo", "Ambient Occlusion", "Normal", "Height",
        "Reflectance", "Transparency", "Settings"
    };

    // Properties
    MaterialProperty albedoTex, albedoColor;
    MaterialProperty aoTex, aoStrength;
    MaterialProperty normalTex, normalStrength;
    MaterialProperty heightScale, heightTex, heightStrength, heightAmount;
    MaterialProperty roughnessTex, roughnessStrength;
    MaterialProperty specularTex, specularStrength;
    MaterialProperty transUseColorTex, transTex, transMix;
    MaterialProperty fresnelStrength, fresnelColorInf, fresnelIOR;
    MaterialProperty useCubicMapping, cubicTiling; 
    MaterialProperty tiling, offset;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);

        GUILayout.BeginHorizontal();

        // --- LEFT SIDEBAR (Categories) ---
        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(140));
        GUILayout.Space(5);

        GUIStyle tabStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Normal,
            fixedHeight = 22,
            padding = new RectOffset(5, 0, 0, 0)
        };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = selectedTab == i;
            string prefix = isActive ? "◉ " : "○ ";
            GUI.color = isActive ? new Color(1f, 0.6f, 0f) : Color.white;

            if (GUILayout.Button(prefix + tabs[i], tabStyle))
            {
                selectedTab = i;
                GUI.FocusControl(null);
            }
        }
        GUI.color = Color.white;
        GUILayout.Space(5);
        GUILayout.EndVertical();

        // --- RIGHT CONTENT AREA (Properties) ---
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(5);
        DrawTabContent(materialEditor);
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Technical footers
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }

    private void FindProperties(MaterialProperty[] props)
    {
        albedoTex = FindProperty("_Albedo_Texture", props, false);
        albedoColor = FindProperty("_Albedo_MixColor", props, false);
        aoTex = FindProperty("_AO_Texture", props, false);
        aoStrength = FindProperty("_AO_Strength", props, false);
        normalTex = FindProperty("_Normal_Texture", props, false);
        normalStrength = FindProperty("_Normal_Strength", props, false);
        heightScale = FindProperty("_Height_UseObjectScale", props, false);
        heightTex = FindProperty("_Height_Texture", props, false);
        heightStrength = FindProperty("_Height_Strength", props, false);
        heightAmount = FindProperty("_Height_AmountCm", props, false);
        roughnessTex = FindProperty("_Roughness_Texture", props, false);
        roughnessStrength = FindProperty("_Roughness_Strength", props, false);
        specularTex = FindProperty("_Specular_Texture", props, false);
        specularStrength = FindProperty("_Specular_Strength", props, false);
        transUseColorTex = FindProperty("_Transparency_UseTextureFromColor", props, false);
        transTex = FindProperty("_Transparency_Texture", props, false);
        transMix = FindProperty("_Transparency_MixStrength", props, false);
        fresnelStrength = FindProperty("_Fresnel_Strength", props, false);
        fresnelColorInf = FindProperty("_Fresnel_ColorInfluenceAmount", props, false);
        fresnelIOR = FindProperty("_Fresnel_IOR", props, false);
        useCubicMapping = FindProperty("_UseCubicMapping", props, false); // <-- ADDED
        cubicTiling = FindProperty("_CubicTiling", props, false);
        tiling = FindProperty("_Tiling_Amount", props, false);
        offset = FindProperty("_Tiling_Offset", props, false);

    }

    private void DrawTabContent(MaterialEditor editor)
    {
        EditorGUIUtility.labelWidth = 150f;

        switch (selectedTab)
        {
            case 0: // Albedo
                foldoutAlbedo = DrawHeaderFoldout("Albedo Map", foldoutAlbedo);
                if (foldoutAlbedo)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, albedoColor, "Mix Color");
                    DrawProp(editor, albedoTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                break;

            case 1: // AO
                foldoutAO = DrawHeaderFoldout("Ambient Occlusion", foldoutAO);
                if (foldoutAO)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, aoStrength, "Strength");
                    DrawProp(editor, aoTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                break;

            case 2: // Normal
                foldoutNormal = DrawHeaderFoldout("Normal Map", foldoutNormal);
                if (foldoutNormal)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, normalStrength, "Strength");
                    DrawProp(editor, normalTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                break;

            case 3: // Height
                foldoutHeight = DrawHeaderFoldout("Height / Displacement", foldoutHeight);
                if (foldoutHeight)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, heightScale, "Use Object Scale");
                    DrawProp(editor, heightAmount, "Height (cm)");
                    DrawProp(editor, heightStrength, "Strength");
                    DrawProp(editor, heightTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                break;

            case 4: // Reflectance
                foldoutRoughness = DrawHeaderFoldout("Roughness", foldoutRoughness);
                if (foldoutRoughness)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, roughnessStrength, "Strength");
                    DrawProp(editor, roughnessTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(2);

                foldoutSpecular = DrawHeaderFoldout("Specular", foldoutSpecular);
                if (foldoutSpecular)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, specularStrength, "Strength");
                    DrawProp(editor, specularTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(2);

                foldoutFresnel = DrawHeaderFoldout("Fresnel", foldoutFresnel);
                if (foldoutFresnel)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, fresnelIOR, "IOR");
                    DrawProp(editor, fresnelStrength, "Strength");
                    DrawProp(editor, fresnelColorInf, "Color Influence");
                    EditorGUI.indentLevel--;
                }
                break;

            case 5: // Transparency
                foldoutTransparency = DrawHeaderFoldout("Transparency Settings", foldoutTransparency);
                if (foldoutTransparency)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(editor, transUseColorTex, "Use Color Texture");
                    DrawProp(editor, transMix, "Mix Strength");
                    DrawProp(editor, transTex, "Texture");
                    EditorGUI.indentLevel--;
                }
                break;

            case 6: // Settings
                foldoutSettings = DrawHeaderFoldout("Tiling & Rendering", foldoutSettings);
                if (foldoutSettings)
                {
                    EditorGUI.indentLevel++;

                    DrawProp(editor, useCubicMapping, "Use Cubic Mapping");

                    // Swap between Cubic and UV tiling based on the toggle state
                    if (useCubicMapping.floatValue > 0.5f)
                    {
                        DrawProp(editor, cubicTiling, "Cubic Tiling");
                    }
                    else
                    {
                        DrawProp(editor, tiling, "UV Tiling");
                    }

                    DrawProp(editor, offset, "Offset");

                    EditorGUI.indentLevel--;
                }
                break;

        }
    }

    private bool DrawHeaderFoldout(string title, bool state)
    {
        Rect rect = GUILayoutUtility.GetRect(16f, 22f, EditorStyles.foldoutHeader);
        return GUI.Toggle(rect, state, title, EditorStyles.foldoutHeader);
    }

    private void DrawProp(MaterialEditor editor, MaterialProperty prop, string customLabel = null)
    {
        if (prop != null)
        {
            string label = string.IsNullOrEmpty(customLabel) ? prop.displayName : customLabel;
            editor.ShaderProperty(prop, new GUIContent(label));
        }
    }
}
