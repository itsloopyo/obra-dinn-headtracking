// Unity stub for CI builds - UnityEngine.dll slice.
//
// Real Unity 2017 UnityEngine.dll is a thin shell whose only job is to
// hold TypeForwardedTo entries pointing to the actual module DLLs
// (CoreModule, IMGUIModule, ...). BepInEx 5's BaseUnityPlugin and
// downstream consumers reference types like MonoBehaviour as
// `[UnityEngine]MonoBehaviour`. When the plugin csproj compiles
// against BepInEx + our stubs, the C# compiler resolves those refs by
// looking inside UnityEngine.dll - if the type isn't there directly
// AND there's no forwarder, it errors with CS7069. Mirror Unity 2017's
// forwarder layout here.
//
// Generic types (UnityEvent<T0>, UnityAction<T0>) cannot be forwarded
// in C# (typeof on open generics in attributes is rejected); consumers
// of those generic types must reference the module DLL directly.

using System.Runtime.CompilerServices;

// CoreModule - UnityEngine namespace
[assembly: TypeForwardedTo(typeof(UnityEngine.Object))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Component))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Behaviour))]
[assembly: TypeForwardedTo(typeof(UnityEngine.MonoBehaviour))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Transform))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GameObject))]
[assembly: TypeForwardedTo(typeof(UnityEngine.HideFlags))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Camera))]
[assembly: TypeForwardedTo(typeof(UnityEngine.CameraClearFlags))]
[assembly: TypeForwardedTo(typeof(UnityEngine.CameraType))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RenderTexture))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Material))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Shader))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Texture))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Texture2D))]
[assembly: TypeForwardedTo(typeof(UnityEngine.TextureFormat))]
[assembly: TypeForwardedTo(typeof(UnityEngine.FilterMode))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Sprite))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Matrix4x4))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Vector2))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Vector3))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Vector4))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Quaternion))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Ray))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Rect))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Color))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Color32))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Resolution))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RuntimePlatform))]
[assembly: TypeForwardedTo(typeof(UnityEngine.ScaleMode))]
[assembly: TypeForwardedTo(typeof(UnityEngine.LayerMask))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Time))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Mathf))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Debug))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GL))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Screen))]
[assembly: TypeForwardedTo(typeof(UnityEngine.QualitySettings))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Application))]
[assembly: TypeForwardedTo(typeof(UnityEngine.PlayerPrefs))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Resources))]
[assembly: TypeForwardedTo(typeof(UnityEngine.SerializeField))]
[assembly: TypeForwardedTo(typeof(UnityEngine.HideInInspector))]
[assembly: TypeForwardedTo(typeof(UnityEngine.HeaderAttribute))]
[assembly: TypeForwardedTo(typeof(UnityEngine.TooltipAttribute))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RangeAttribute))]

// CoreModule - UnityEngine.Rendering
[assembly: TypeForwardedTo(typeof(UnityEngine.Rendering.RenderPipelineAsset))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Rendering.GraphicsSettings))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Rendering.RenderPipeline))]

// CoreModule - UnityEngine.SceneManagement
[assembly: TypeForwardedTo(typeof(UnityEngine.SceneManagement.Scene))]
[assembly: TypeForwardedTo(typeof(UnityEngine.SceneManagement.LoadSceneMode))]
[assembly: TypeForwardedTo(typeof(UnityEngine.SceneManagement.SceneManager))]

// CoreModule - UnityEngine.Events
[assembly: TypeForwardedTo(typeof(UnityEngine.Events.UnityEvent))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Events.UnityAction))]

// IMGUIModule
[assembly: TypeForwardedTo(typeof(UnityEngine.GUIUtility))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GUIStyle))]
[assembly: TypeForwardedTo(typeof(UnityEngine.FontStyle))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GUIStyleState))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GUI))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GUIContent))]
[assembly: TypeForwardedTo(typeof(UnityEngine.GUISkin))]

// InputLegacyModule
[assembly: TypeForwardedTo(typeof(UnityEngine.Input))]
[assembly: TypeForwardedTo(typeof(UnityEngine.KeyCode))]

// PhysicsModule
[assembly: TypeForwardedTo(typeof(UnityEngine.Rigidbody))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Collider))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RaycastHit))]
[assembly: TypeForwardedTo(typeof(UnityEngine.Physics))]

// TextRenderingModule
[assembly: TypeForwardedTo(typeof(UnityEngine.Font))]
[assembly: TypeForwardedTo(typeof(UnityEngine.TextAnchor))]

// UIModule
[assembly: TypeForwardedTo(typeof(UnityEngine.Canvas))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RenderMode))]
[assembly: TypeForwardedTo(typeof(UnityEngine.RectTransform))]
[assembly: TypeForwardedTo(typeof(UnityEngine.CanvasGroup))]
