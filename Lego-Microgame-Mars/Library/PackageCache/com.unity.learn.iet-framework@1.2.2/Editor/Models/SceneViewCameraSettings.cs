using System;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// The Scene View camera mode
    /// </summary>
    public enum SceneViewCameraMode { SceneView2D, SceneView3D };
    /// <summary>
    /// Determines how the camera position is applied when loaded
    /// </summary>
    public enum SceneViewFocusMode { Manual, FrameObject };

    /// <summary>
    /// Used to store and apply scene view camera settings
    /// </summary>
    [Serializable]
    public class SceneViewCameraSettings
    {
        /// <summary>
        /// The Scene View camera mode
        /// </summary>
        public SceneViewCameraMode cameraMode { get { return m_CameraMode; } }
        [SerializeField]
        SceneViewCameraMode m_CameraMode = SceneViewCameraMode.SceneView2D;

        /// <summary>
        /// Determines how the camera position is applied when loaded
        /// </summary>
        public SceneViewFocusMode focusMode { get { return m_FocusMode; } }
        [SerializeField]
        SceneViewFocusMode m_FocusMode = SceneViewFocusMode.Manual;

        /// <summary>
        /// Is the camera ortographic?
        /// </summary>
        public bool orthographic { get { return m_Orthographic; } }
        [SerializeField]
        bool m_Orthographic = false;

        /// <summary>
        /// Ortographic size of the camera
        /// </summary>
        public float size { get { return m_Size; } }
        [SerializeField]
        float m_Size = default;

        /// <summary>
        /// The point the camera will look at
        /// </summary>
        public Vector3 pivot { get { return m_Pivot; } }
        [SerializeField]
        Vector3 m_Pivot = default;

        /// <summary>
        /// The rotation of the camera
        /// </summary>
        public Quaternion rotation { get { return m_Rotation; } }
        [SerializeField]
        Quaternion m_Rotation = default;

        /// <summary>
        /// The object that can be framed by the camera
        /// </summary>
        public SceneObjectReference frameObject { get { return m_FrameObject; } }
        [SerializeField]
        SceneObjectReference m_FrameObject = null;

        /// <summary>
        /// Are these camera settings going to be used?
        /// </summary>
        public bool enabled { get { return m_Enabled; } }
        [SerializeField]
        bool m_Enabled = false;

        /// <summary>
        /// Applies the saved camera settings to the current scene camera
        /// </summary>
        public void Apply()
        {
            var sceneView = EditorWindow.GetWindow<SceneView>(null, false);
            sceneView.in2DMode = (cameraMode == SceneViewCameraMode.SceneView2D);
            switch (focusMode)
            {
                case SceneViewFocusMode.FrameObject:
                    GameObject go = frameObject.ReferencedObjectAsGameObject;
                    if (go == null)
                        throw new InvalidOperationException("Error looking up frame object");
                    sceneView.Frame(GameObjectProxy.CalculateBounds(go), true);
                    break;
                case SceneViewFocusMode.Manual:
                    sceneView.LookAt(pivot, rotation, size, orthographic, false);
                    break;
                default:
                    throw new NotImplementedException(string.Format("Focus mode {0} not supported", focusMode));
            }
        }
    }
}
