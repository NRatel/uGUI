#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    ///   EditorOnly class for tracking all Graphics.
    /// Used when a source asset is reimported into the editor to ensure that Graphics are updated as intended.
    /// </summary>
    public static class GraphicRebuildTracker
    {
        static IndexedSet<Graphic> m_Tracked = new IndexedSet<Graphic>();
        static bool s_Initialized;

        /// <summary>
        /// Add a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to track</param>
        public static void TrackGraphic(Graphic g)
        {
            if (!s_Initialized)
            {
                //文档：https://docs.unity3d.com/ScriptReference/CanvasRenderer-onRequestRebuild.html
                //每当CanvasRenderer中的数据失效并需要重建时触发的事件(仅编辑器)。
                //例如，每当一个纹理被重新导入时，这个事件就会被触发。
                CanvasRenderer.onRequestRebuild += OnRebuildRequested;
                s_Initialized = true;
            }

            m_Tracked.AddUnique(g);
        }

        /// <summary>
        /// Remove a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to remove from tracking.</param>
        public static void UnTrackGraphic(Graphic g)
        {
            m_Tracked.Remove(g);
        }

        static void OnRebuildRequested()
        {
            StencilMaterial.ClearAll();
            for (int i = 0; i < m_Tracked.Count; i++)
            {
                m_Tracked[i].OnRebuildRequested();
            }
        }
    }
}
#endif // if UNITY_EDITOR
