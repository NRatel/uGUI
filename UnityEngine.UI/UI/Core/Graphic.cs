using System;
#if UNITY_EDITOR
using System.Reflection;
#endif
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI.CoroutineTween;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    /// <summary>
    ///   Base class for all visual UI Component.
    ///   When creating visual UI components you should inherit from this class.
    /// </summary>
    /// <example>
    /// Below is a simple example that draws a colored quad inside the Rect Transform area.
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// [ExecuteInEditMode]
    /// public class SimpleImage : Graphic
    /// {
    ///     protected override void OnPopulateMesh(VertexHelper vh)
    ///     {
    ///         Vector2 corner1 = Vector2.zero;
    ///         Vector2 corner2 = Vector2.zero;
    ///
    ///         corner1.x = 0f;
    ///         corner1.y = 0f;
    ///         corner2.x = 1f;
    ///         corner2.y = 1f;
    ///
    ///         corner1.x -= rectTransform.pivot.x;
    ///         corner1.y -= rectTransform.pivot.y;
    ///         corner2.x -= rectTransform.pivot.x;
    ///         corner2.y -= rectTransform.pivot.y;
    ///
    ///         corner1.x *= rectTransform.rect.width;
    ///         corner1.y *= rectTransform.rect.height;
    ///         corner2.x *= rectTransform.rect.width;
    ///         corner2.y *= rectTransform.rect.height;
    ///
    ///         vh.Clear();
    ///
    ///         UIVertex vert = UIVertex.simpleVert;
    ///
    ///         vert.position = new Vector2(corner1.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner1.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vh.AddTriangle(0, 1, 2);
    ///         vh.AddTriangle(2, 3, 0);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Graphic
        : UIBehaviour,
          ICanvasElement
    {
        static protected Material s_DefaultUI = null;       //默认UI材质，Canvas.GetDefaultCanvasMaterial()
        static protected Texture2D s_WhiteTexture = null;   //默认空白贴图，Texture2D.whiteTexture

        /// <summary>
        /// Default material used to draw UI elements if no explicit material was specified.
        /// 如果没有明确指定材质，则默认材质用于绘制UI元素
        /// </summary>

        static public Material defaultGraphicMaterial
        {
            get
            {
                if (s_DefaultUI == null)
                    s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
                return s_DefaultUI;
            }
        }

        // Cached and saved values
        [FormerlySerializedAs("m_Mat")]
        [SerializeField] protected Material m_Material;         //当前材质

        [SerializeField] private Color m_Color = Color.white;   //当前颜色

        [NonSerialized] protected bool m_SkipLayoutUpdate;      //是否跳过Layout更新，置为true后本帧内有效，执行跳过后立刻置回false。
        [NonSerialized] protected bool m_SkipMaterialUpdate;    //是否跳过材质更新，置为true后本帧内有效，执行跳过后立刻置回false。

        /// <summary>
        /// Base color of the Graphic.
        /// </summary>
        /// <remarks>
        /// The builtin UI Components use this as their vertex color. Use this to fetch or change the Color of visual UI elements, such as an Image.
        /// </remarks>
        /// <example>
        /// <code>
        /// //Place this script on a GameObject with a Graphic component attached e.g. a visual UI element (Image).
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Graphic m_Graphic;
        ///     Color m_MyColor;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the Graphic from the GameObject
        ///         m_Graphic = GetComponent<Graphic>();
        ///         //Create a new Color that starts as red
        ///         m_MyColor = Color.red;
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        ///
        ///     // Update is called once per frame
        ///     void Update()
        ///     {
        ///         //When the mouse button is clicked, change the Graphic Color
        ///         if (Input.GetKey(KeyCode.Mouse0))
        ///         {
        ///             //Change the Color over time between blue and red while the mouse button is pressed
        ///             m_MyColor = Color.Lerp(Color.red, Color.blue, Mathf.PingPong(Time.time, 1));
        ///         }
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual Color color { get { return m_Color; } set { if (SetPropertyUtility.SetColor(ref m_Color, value)) SetVerticesDirty(); } }

        [SerializeField] private bool m_RaycastTarget = true;   //是否作为射线检测目标

        /// <summary>
        /// Should this graphic be considered a target for raycasting?
        /// </summary>
        public virtual bool raycastTarget { get { return m_RaycastTarget; } set { m_RaycastTarget = value; } }

        [NonSerialized] private RectTransform m_RectTransform;      //与自身同级的、依赖的RectTransform
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;    //与自身同级的、依赖的CanvasRenderer
        [NonSerialized] private Canvas m_Canvas;                    //自身所属的、第一个active和enabled均为true的Canvas，可为null

        [NonSerialized] private bool m_VertsDirty;       //顶点脏标记，默认false, SetVerticesDirty()置为true, Rebuild()中UpdateGeometry()执行后置回false
        [NonSerialized] private bool m_MaterialDirty;    //材质脏标记，默认false, SetVerticesDirty()置为true, Rebuild()中UpdateMaterial()执行后置回false

        [NonSerialized] protected UnityAction m_OnDirtyLayoutCallback;      //SetLayoutDirty() 被调用时触发该回调
        [NonSerialized] protected UnityAction m_OnDirtyVertsCallback;       //SetVerticesDirty() 被调用时触发该回调
        [NonSerialized] protected UnityAction m_OnDirtyMaterialCallback;    //SetMaterialDirty() 被调用时触发该回调

        [NonSerialized] protected static Mesh s_Mesh;       //默认创建的、所有UI元素共享的 Mesh，HideFlags.HideAndDontSave。（新Scene中保留，Hierarchy上隐藏）
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        [NonSerialized] protected Mesh m_CachedMesh;        //疑问??? 没找到任何引用
        [NonSerialized] protected Vector2[] m_CachedUvs;    //疑问??? 没找到任何引用
        // Tween controls for the Graphic
        [NonSerialized] private readonly TweenRunner<ColorTween> m_ColorTweenRunner;    //颜色渐变动画运行器，用于执行颜色渐变/透明度渐变。

        protected bool useLegacyMeshGeneration { get; set; }        //是否使用旧的Mesh创建方式，默认为true。

        // Called by Unity prior to deserialization,
        // should not be called by users
        // 疑问??? 继承自Monobehaviour的类 构造函数?
        // 创建 m_ColorTweenRunner，默认 useLegacyMeshGeneration为true。
        protected Graphic()
        {
            if (m_ColorTweenRunner == null)
                m_ColorTweenRunner = new TweenRunner<ColorTween>();
            m_ColorTweenRunner.Init(this);
            useLegacyMeshGeneration = true;
        }

        /// <summary>
        /// Set all properties of the Graphic dirty and needing rebuilt.
        /// Dirties Layout, Vertices, and Materials.
        /// </summary>
        public virtual void SetAllDirty()
        {
            // Optimization: Graphic layout doesn't need recalculation if
            // the underlying Sprite is the same size with the same texture.
            // (e.g. Sprite sheet texture animation)
            //优化:如果基础精灵具有相同的大小和纹理，那么 Graphic layout 便不需要重新计算。

            //能跳过的过程尽量跳过
            if (m_SkipLayoutUpdate)
            {
                m_SkipLayoutUpdate = false;
            }
            else
            {
                SetLayoutDirty();
            }

            if (m_SkipMaterialUpdate)
            {
                m_SkipMaterialUpdate = false;
            }
            else
            {
                SetMaterialDirty();
            }

            SetVerticesDirty();
        }

        /// <summary>
        /// Mark the layout as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyLayoutCallback notification if any elements are registered. See RegisterDirtyLayoutCallback
        /// </remarks>
        public virtual void SetLayoutDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }

        /// <summary>
        /// Mark the vertices as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyVertsCallback notification if any elements are registered. See RegisterDirtyVerticesCallback
        /// </remarks>
        public virtual void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }

        /// <summary>
        /// Mark the material as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyMaterialCallback notification if any elements are registered. See RegisterDirtyMaterialCallback
        /// </remarks>
        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }

        //重写 UIBehaviour 的方法
        //RectTransform大小发生变化时（具体看UIBehaviour里的注释），
        //1、标记顶点脏（需要重新创建自身Mesh）。
        //2、标记布局脏（会导致布局改变）。
        protected override void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                // prevent double dirtying...
                if (CanvasUpdateRegistry.IsRebuildingLayout())
                    SetVerticesDirty();
                else
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        //重写 UIBehaviour 的方法
        //父物体改变前（具体看UIBehaviour里的注释），
        //1、清除GraphicRegistry中的注册（使原位置射线检测失效）。
        //2、标记重新布局（会导致布局改变）（不会触发布局为脏回调）。
        protected override void OnBeforeTransformParentChanged()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        //重写 UIBehaviour 的方法
        //父物体改变后（具体看UIBehaviour里的注释），
        //1、清除缓存的 m_Canvas 并重新查找和缓存（因为位置变了，所属Canvas可能变化）。
        //2、在GraphicRegistry中注册。（使新位置射线检测生效）。
        //3、标记为全脏。
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_Canvas = null;

            if (!IsActive())
                return;

            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            SetAllDirty();
        }

        /// <summary>
        /// Absolute depth of the graphic, used by rendering and events -- lowest to highest.
        /// </summary>
        /// <example>
        /// The depth is relative to the first root canvas.
        ///
        /// Canvas
        ///  Graphic - 1
        ///  Graphic - 2
        ///  Nested Canvas
        ///     Graphic - 3
        ///     Graphic - 4
        ///  Graphic - 5
        ///
        /// This value is used to determine draw and event ordering.
        /// </example>
        public int depth { get { return canvasRenderer.absoluteDepth; } }

        /// <summary>
        /// The RectTransform component used by the Graphic. Cached for speed.
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_RectTransform, null))
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        /// <summary>
        /// A reference to the Canvas this Graphic is rendering to.
        /// </summary>
        /// <remarks>
        /// In the situation where the Graphic is used in a hierarchy with multiple Canvases, the Canvas closest to the root will be used.
        /// </remarks>
        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        private void CacheCanvas()
        {
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                // Find the first active and enabled canvas.
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        m_Canvas = list[i];
                        break;
                    }
                }
            }
            else
            {
                m_Canvas = null;
            }

            ListPool<Canvas>.Release(list);
        }

        /// <summary>
        /// A reference to the CanvasRenderer populated by this Graphic.
        /// </summary>
        public CanvasRenderer canvasRenderer
        {
            get
            {
                // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_CanvasRenderer, null))
                {
                    m_CanvasRenderer = GetComponent<CanvasRenderer>();
                }
                return m_CanvasRenderer;
            }
        }

        /// <summary>
        /// Returns the default material for the graphic.
        /// 本Graphic默认采用的材质，默认为defaultGraphicMaterial（可重写）
        /// </summary>
        public virtual Material defaultMaterial
        {
            get { return defaultGraphicMaterial; }
        }

        /// <summary>
        /// The Material set by the user
        /// 当前材质set/get。set时触发SetMaterialDirty(); get时若为空则取defaultMaterial（可重写）
        /// </summary>
        public virtual Material material
        {
            get
            {
                return (m_Material != null) ? m_Material : defaultMaterial;
            }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// The material that will be sent for Rendering (Read only).
        /// </summary>
        /// <remarks>
        /// This is the material that actually gets sent to the CanvasRenderer. By default it's the same as [[Graphic.material]]. When extending Graphic you can override this to send a different material to the CanvasRenderer than the one set by Graphic.material. This is useful if you want to modify the user set material in a non destructive manner.
        /// 实际被发送到CanvasRenderer的、被 IMaterialModifier 修改后的材质。（可重写）
        /// </remarks>
        public virtual Material materialForRendering
        {
            get
            {
                var components = ListPool<Component>.Get();
                GetComponents(typeof(IMaterialModifier), components);

                var currentMat = material;
                for (var i = 0; i < components.Count; i++)
                    currentMat = (components[i] as IMaterialModifier).GetModifiedMaterial(currentMat);
                ListPool<Component>.Release(components);
                return currentMat;
            }
        }

        /// <summary>
        /// The graphic's texture. (Read Only).
        /// </summary>
        /// <remarks>
        /// This is the Texture that gets passed to the CanvasRenderer, Material and then Shader _MainTex.
        ///
        /// When implementing your own Graphic you can override this to control which texture goes through the UI Rendering pipeline.
        ///
        /// Bear in mind that Unity tries to batch UI elements together to improve performance, so its ideal to work with atlas to reduce the number of draw calls.
        /// </remarks>
        public virtual Texture mainTexture
        {
            get
            {
                return s_WhiteTexture;
            }
        }

        // Mark the Graphic and the canvas as having been changed.
        // 标记 Graphic 和 Canvas 已改变。
        //1、查找和缓存 m_Canvas。
        //2、在 GraphicRegistry 中注册。（使射线检测生效）
        //3、在 GraphicRebuildTracker 追踪。（仅编辑器下）
        //4、为 s_WhiteTexture 设置初始值 Texture2D.whiteTexture。
        //5、标记为全脏。
        protected override void OnEnable()
        {
            base.OnEnable(); //疑问??? 父方法是空的为何要调？
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);

#if UNITY_EDITOR
            GraphicRebuildTracker.TrackGraphic(this);
#endif
            if (s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;

            SetAllDirty();
        }

        // Clear references.
        // 清除引用
        //1、清除 GraphicRebuildTracker 追踪。（仅编辑器下）
        //2、清除 GraphicRegistry 中的注册。
        //3、清除 CanvasUpdateRegistry 中的注册。
        //4、清理 canvasRenderer。
        //5、标记重新布局（会导致布局改变）（不会触发布局为脏回调）。
        protected override void OnDisable()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (canvasRenderer != null)
                canvasRenderer.Clear();

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            base.OnDisable();
        }

        //1、销毁缓存的 Mesh???
        protected override void OnDestroy()
        {
            if (m_CachedMesh)
                Destroy(m_CachedMesh);
            m_CachedMesh = null;

            base.OnDestroy();
        }

        //重写 UIBehaviour 的方法
        //当关联的 Canvas 在 Hierarchy 上变化时（具体看UIBehaviour里的注释），
        //1、清除缓存的 m_Canvas 并重新查找和缓存。
        //2、若新的 Canvas 与原来的不同，更新GraphicRegistry中注册（更新射线的检测）。
        protected override void OnCanvasHierarchyChanged()
        {
            // Use m_Cavas so we dont auto call CacheCanvas
            Canvas currentCanvas = m_Canvas;

            // Clear the cached canvas. Will be fetched below if active.
            m_Canvas = null;

            if (!IsActive())
                return;  

            CacheCanvas();

            if (currentCanvas != m_Canvas)
            {
                GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);

                // Only register if we are active and enabled as OnCanvasHierarchyChanged can get called
                // during object destruction and we dont want to register ourself and then become null.
                if (IsActive())
                    GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            }
        }

        /// <summary>
        /// This method must be called when <c>CanvasRenderer.cull</c> is modified.
        /// </summary>
        /// <remarks>
        /// This can be used to perform operations that were previously skipped because the <c>Graphic</c> was culled.
        /// </remarks>
        public virtual void OnCullingChanged()
        {
            if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
            {
                /// When we were culled, we potentially skipped calls to <c>Rebuild</c>.
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
        }

        //实现 ICanvasElement 的接口
        // Rebuilds the graphic geometry and its material on the PreRender cycle.
        // 在 CanvasUpdate 的 PreRender 阶段重建图形几何 和 材质
        //1、顶点脏则更新图形几何
        //2、材质脏则更新材质
        public virtual void Rebuild(CanvasUpdate update)
        {
            if (canvasRenderer == null || canvasRenderer.cull)
                return;

            switch (update)
            {
                case CanvasUpdate.PreRender:
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }
                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }
                    break;
            }
        }

        //实现 ICanvasElement 的接口
        public virtual void LayoutComplete()
        {}

        //实现 ICanvasElement 的接口
        public virtual void GraphicUpdateComplete()
        {}

        // Call to update the Material of the graphic onto the CanvasRenderer.
        // 将 Graphic 的 Material 更新至 CanvasRenderer 上。
        //1、设置 canvasRenderer 的材质数量为1。
        //2、设置 canvasRenderer 的材质为 materialForRendering （经过修改的最终材质）。
        //3、设置 canvasRenderer 的 Texture 为 Graphic 的 mainTexture。
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        // Call to update the geometry of the Graphic onto the CanvasRenderer.
        // 将 Graphic 的 Mesh 更新至 CanvasRenderer 上。
        protected virtual void UpdateGeometry()
        {
            if (useLegacyMeshGeneration)
            {
                DoLegacyMeshGeneration();
            }
            else
            {
                DoMeshGeneration();
            }
        }

        private void DoMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
                OnPopulateMesh(s_VertexHelper);
            else
                s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);

            ListPool<Component>.Release(components);

            s_VertexHelper.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
        }

        //旧的 Mesh 创建方法
        //1、rectTransform 存在 且宽高为正时才创建，否则调用 Mesh 的清理方法。
        //2、调用 OnPopulateMesh 执行创建。
        private void DoLegacyMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
#pragma warning disable 618         //疑问??? 618是什么警告？
                OnPopulateMesh(workerMesh);
#pragma warning restore 618
            }
            else
            {
                workerMesh.Clear();
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
#pragma warning disable 618
                ((IMeshModifier)components[i]).ModifyMesh(workerMesh);
#pragma warning restore 618
            }

            ListPool<Component>.Release(components);
            canvasRenderer.SetMesh(workerMesh);
        }

        protected static Mesh workerMesh
        {
            get
            {
                if (s_Mesh == null)
                {
                    s_Mesh = new Mesh();
                    s_Mesh.name = "Shared UI Mesh";
                    s_Mesh.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Mesh;
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use OnPopulateMesh instead.", true)]
        protected virtual void OnFillVBO(System.Collections.Generic.List<UIVertex> vbo) {}

        [Obsolete("Use OnPopulateMesh(VertexHelper vh) instead.", false)]
        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <param name="m">Mesh to populate with UI data.</param>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(Mesh m)
        {
            OnPopulateMesh(s_VertexHelper);
            s_VertexHelper.FillMesh(m);
        }

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <param name="vh">VertexHelper utility.</param>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            Color32 color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only callback that is issued by Unity if a rebuild of the Graphic is required.
        /// Currently sent when an asset is reimported.
        /// </summary>
        public virtual void OnRebuildRequested()
        {
            // when rebuild is requested we need to rebuild all the graphics /
            // and associated components... The correct way to do this is by
            // calling OnValidate... Because MB's don't have a common base class
            // we do this via reflection. It's nasty and ugly... Editor only.
            //当重建被请求时，我们需要重建所有的graphics和相关组件…
            //做这件事的正确方法是调用OnValidate……
            //但因为MonoBehaviour没有公共基类,所以通过反射来实现。又脏又丑……仅编辑器下。
            var mbs = gameObject.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb == null)
                    continue;
                var methodInfo = mb.GetType().GetMethod("OnValidate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                    methodInfo.Invoke(mb, null);
            }
        }

        protected override void Reset()
        {
            SetAllDirty();
        }

#endif

        //重写 UIBehaviour 的方法
        //当动画属性变化时，方法被调用。
        //1、标记为全脏。
        protected override void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }


        // Make the Graphic have the native size of its content.
        // 使 Graphic 具有其内容本身的大小。
        // 为子类设定的模板方法
        public virtual void SetNativeSize() {}


        // When a GraphicRaycaster is raycasting into the scene it does two things. First it filters the elements using their RectTransform rect. Then it uses this Raycast function to determine the elements hit by the raycast.
        // 当GraphicRaycaster向场景进行光线投射时，它会做两件事。它使用 RectTransform 的 rect 来过滤元素。使用光线投射函数来确定光线投射的元素。
        // 参数 "sp": Screen point being tested。被射线检测的屏幕坐标
        // 参数 "eventCamera": Camera that is being used for the testing. 射线检测的事件相机
        // 方法的目的：确定 本 Graphic 是否没射线检测到!!!
        // 1、若未激活/启动，则直接返回false。
        // 2、从当前 Graphic 开始向其父物体递归遍历检测，直到parent为null或被提前打断。
        // 3、若当前物体存在一个“使用自己独立 SortOrder” 的 Canvas 组件，则使本次执行完后结束递归遍历（提前打断）。（NRatel原因/结论：检测以相同 SortOrder 为基准，对嵌套的 Canvas 分割断层。
        // 4、若当前物体不存在一个“实现了接口 ICanvasRaycastFilter” 的组件，则跳过当前，继续检测其父物体。（NRatel原因/结论：不实现该接口的组件无法被射线检测到。
        // 5、若当前物体存在一个“ignoreParentGroups == true” 的 CanvasGroup 组件，则在递归遍历过程中只检测当前 CanvasGroup 这一层。
        public virtual bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return false;

            var t = transform;
            var components = ListPool<Component>.Get();

            bool ignoreParentGroups = false;        //是否忽略了父CanvasGroup（这是一个保证仅一次的标志）
            bool continueTraversal = true;          //是否继续遍历，若置为false，本次执行完后结束遍历

            while (t != null)
            {
                t.GetComponents(components);         //取所有组件
                for (var i = 0; i < components.Count; i++)
                {
                    var canvas = components[i] as Canvas;  
                    if (canvas != null && canvas.overrideSorting)
                        continueTraversal = false;  //若当前物体存在一个“使用自己独立 SortOrder” 的 Canvas 组件，则使本次执行完后结束递归遍历（提前打断）。

                    var filter = components[i] as ICanvasRaycastFilter;
                    if (filter == null)
                        continue;                   //下面的步骤均依赖 ICanvasRaycastFilter。

                    var raycastValid = true;        //射线检测是否有效？

                    var group = components[i] as CanvasGroup;
                    if (group != null)
                    {
                        //if (ignoreParentGroups == false && group.ignoreParentGroups)
                        //{
                        //    ignoreParentGroups = true;
                        //    raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                        //}
                        //else if (!ignoreParentGroups)
                        //    raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);

                        //将这块代码等价替换，易于理解（NRatel）
                        //若当前物体存在一个“ignoreParentGroups == true” 的 CanvasGroup 组件，则在递归遍历过程中只检测当前 CanvasGroup 这一层。
                        if (!ignoreParentGroups)
                        {
                            if (group.ignoreParentGroups)
                            {
                                ignoreParentGroups = true;
                            }
                            raycastValid = filter.IsRaycastLocationValid(sp, eventCamera); 
                        }
                    }
                    else
                    {
                        raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                    }

                    if (!raycastValid)
                    {
                        ListPool<Component>.Release(components);
                        return false;               //可被检测（有ICanvasRaycastFilter），但未被检测到
                    }
                }
                t = continueTraversal ? t.parent : null;
            }
            ListPool<Component>.Release(components);
            return true;
        }

#if UNITY_EDITOR
        //编辑器下，脚本被加载、或 Inspector 中的任何值被修改时，方法被调用
        //1、设为全脏
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }

#endif

        ///<summary>
        ///Adjusts the given pixel to be pixel perfect.
        ///</summary>
        ///<param name="point">Local space point.</param>
        ///<returns>Pixel perfect adjusted point.</returns>
        ///<remarks>
        ///Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        ///</remarks>
        public Vector2 PixelAdjustPoint(Vector2 point)
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return point;
            else
            {
                return RectTransformUtility.PixelAdjustPoint(point, transform, canvas);
            }
        }

        /// <summary>
        /// Returns a pixel perfect Rect closest to the Graphic RectTransform.
        /// </summary>
        /// <remarks>
        /// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        /// </remarks>
        /// <returns>A Pixel perfect Rect.</returns>
        public Rect GetPixelAdjustedRect()
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return rectTransform.rect;
            else
                return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        }

        ///<summary>
        ///Tweens the CanvasRenderer color associated with this Graphic.
        ///</summary>
        ///<param name="targetColor">Target color.</param>
        ///<param name="duration">Tween duration.</param>
        ///<param name="ignoreTimeScale">Should ignore Time.scale?</param>
        ///<param name="useAlpha">Should also Tween the alpha channel?</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, true);
        }

        ///<summary>
        ///Tweens the CanvasRenderer color associated with this Graphic.
        ///用内置m_ColorTweenRunner执行颜色渐变（最终调用canvasRenderer.SetColor）
        ///</summary>
        ///<param name="targetColor">Target color.</param>
        ///<param name="duration">Tween duration.</param>
        ///<param name="ignoreTimeScale">Should ignore Time.scale?</param>
        ///<param name="useAlpha">Should also Tween the alpha channel?</param>
        /// <param name="useRGB">Should the color or the alpha be used to tween</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
        {
            if (canvasRenderer == null || (!useRGB && !useAlpha))
                return;

            Color currentColor = canvasRenderer.GetColor();
            if (currentColor.Equals(targetColor))
            {
                m_ColorTweenRunner.StopTween();
                return;
            }

            ColorTween.ColorTweenMode mode = (useRGB && useAlpha ?
                ColorTween.ColorTweenMode.All :
                (useRGB ? ColorTween.ColorTweenMode.RGB : ColorTween.ColorTweenMode.Alpha));

            var colorTween = new ColorTween {duration = duration, startColor = canvasRenderer.GetColor(), targetColor = targetColor};
            colorTween.AddOnChangedCallback(canvasRenderer.SetColor);
            colorTween.ignoreTimeScale = ignoreTimeScale;
            colorTween.tweenMode = mode;
            m_ColorTweenRunner.StartTween(colorTween);
        }

        static private Color CreateColorFromAlpha(float alpha)
        {
            var alphaColor = Color.black;
            alphaColor.a = alpha;
            return alphaColor;
        }

        ///<summary>
        ///Tweens the alpha of the CanvasRenderer color associated with this Graphic.
        ///</summary>
        ///<param name="alpha">Target alpha.</param>
        ///<param name="duration">Duration of the tween in seconds.</param>
        ///<param name="ignoreTimeScale">Should ignore [[Time.scale]]?</param>
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            CrossFadeColor(CreateColorFromAlpha(alpha), duration, ignoreTimeScale, true, false);
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics layout is dirtied.
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics layout are dirtied
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback -= action;
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics vertices are dirtied.
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics vertices are dirtied
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback -= action;
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics material is dirtied.
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics material are dirtied
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback -= action;
        }
    }
}
