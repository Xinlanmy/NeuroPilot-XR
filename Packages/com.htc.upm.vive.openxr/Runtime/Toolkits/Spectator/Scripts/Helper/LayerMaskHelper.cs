// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public static class LayerMaskHelper
    {
        public static bool HasLayer(this LayerMask layerMask, int layer)
        {
            if (layerMask == (layerMask | (1 << layer)))
            {
                return true;
            }

            return false;
        }

        public static bool[] HasLayers(this LayerMask layerMask)
        {
            var hasLayers = new bool[32];

            for (int i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    Debug.Log($"LayerMask.LayerToName = {LayerMask.LayerToName(i)}");
                    hasLayers[i] = true;
                }
            }

            return hasLayers;
        }

#if UNITY_EDITOR
        public static class LayerMaskDrawer
        {
            public static int LayerMaskField(string label, int layermask)
            {
                return FieldToLayerMask(EditorGUILayout.MaskField(label, LayerMaskToField(layermask),
                    InternalEditorUtility.layers));
            }

            public static int LayerMaskField(Rect position, string label, int layermask)
            {
                return FieldToLayerMask(EditorGUI.MaskField(position, label, LayerMaskToField(layermask),
                    InternalEditorUtility.layers));
            }

            /// <summary>
            /// Converts field LayerMask values to in game LayerMask values
            /// </summary>
            /// <param name="field"></param>
            /// <returns></returns>
            private static int FieldToLayerMask(int field)
            {
                if (field == -1) return -1;
                int mask = 0;
                var layers = InternalEditorUtility.layers;
                for (int c = 0; c < layers.Length; c++)
                {
                    if ((field & (1 << c)) != 0)
                    {
                        mask |= 1 << LayerMask.NameToLayer(layers[c]);
                    }
                    else
                    {
                        mask &= ~(1 << LayerMask.NameToLayer(layers[c]));
                    }
                }

                return mask;
            }

            /// <summary>
            /// Converts in game LayerMask values to field LayerMask values
            /// </summary>
            /// <param name="mask"></param>
            /// <returns></returns>
            private static int LayerMaskToField(int mask)
            {
                if (mask == -1) return -1;
                int field = 0;
                var layers = InternalEditorUtility.layers;
                for (int c = 0; c < layers.Length; c++)
                {
                    if ((mask & (1 << LayerMask.NameToLayer(layers[c]))) != 0)
                    {
                        field |= 1 << c;
                    }
                }

                return field;
            }
        }
#endif
    }
}