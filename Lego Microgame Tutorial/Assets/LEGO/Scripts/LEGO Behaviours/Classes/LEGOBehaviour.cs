using LEGOModelImporter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public abstract class LEGOBehaviour : MonoBehaviour
    {
        public const float LEGOHorizontalModule = 0.8f;
        public const float LEGOVerticalModule = 0.96f;
        public const float LEGOModuleVolume = LEGOHorizontalModule * LEGOHorizontalModule * LEGOVerticalModule;

        protected enum Scope
        {
            Brick,
            ConnectedBricks
        }
      
        [SerializeField, Tooltip("Apply to connected bricks.\nor\nApply to this brick only.")]
        protected Scope m_Scope = Scope.ConnectedBricks;

        [SerializeField]
        protected string m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Default.png";

        [SerializeField]
        protected Color m_FlashColour = Color.white;

        [SerializeField]
        protected Brick m_Brick;
        [SerializeField]
        protected Vector3 m_BrickPivotOffset;

        protected HashSet<Brick> m_ScopedBricks = new HashSet<Brick>();
        protected List<MeshRenderer> m_scopedPartRenderers = new List<MeshRenderer>();
        protected Vector3 m_ScopedPivotOffset;
        protected Bounds m_ScopedBounds;

        protected List<Material> m_DecorationSurfaceMaterials = new List<Material>();

        protected static readonly int s_EmissionColorID = Shader.PropertyToID("_EmissionColor");

        const float k_FlashLength = 0.5f;
      
        public bool IsPlacedOnBrick()
        {
            return m_Brick;
        }

        public bool IsVisible()
        {
            foreach(var partRenderer in m_scopedPartRenderers)
            {
                if (partRenderer.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        public Bounds GetGizmoBounds()
        {
            var position = transform.position;

            if (IsPlacedOnBrick())
            {
                position += transform.TransformVector(m_BrickPivotOffset);

            }

            var legoBehaviours = GetComponentsInChildren<LEGOBehaviour>();
            for (var i = 0; i < legoBehaviours.Length; i++)
            {
                if (legoBehaviours[i] == this)
                {
                    var center = position + Vector3.up * 3 + Vector3.forward * i + Vector3.back * (legoBehaviours.Length - 1) * 0.5f;
                    return new Bounds(center, Vector3.one);
                }
            }

            return new Bounds();
        }

        public Bounds GetBrickBounds()
        {
            var center = Vector3.zero;
            var extents = Vector3.one * 0.5f;

            if (IsPlacedOnBrick())
            {
                center = m_Brick.totalBounds.center;
                extents = m_Brick.totalBounds.extents;
            }

            var worldPoints = new List<Vector3>
            {
                transform.TransformPoint(center + Vector3.Scale(new Vector3(-1, -1, -1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(-1, -1, 1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(-1, 1, -1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(-1, 1, 1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(1, -1, -1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(1, -1, 1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(1, 1, -1), extents)),
                transform.TransformPoint(center + Vector3.Scale(new Vector3(1, 1, 1), extents))
            };

            var bounds = new Bounds(worldPoints[0], Vector3.zero);
            foreach (var worldPoint in worldPoints)
            {
                bounds.Encapsulate(worldPoint);
            }

            return bounds;
        }

        public Vector3 GetBrickCenter()
        {
            var position = transform.position;

            if (IsPlacedOnBrick())
            {
                position = transform.position + transform.TransformVector(m_BrickPivotOffset);
            }

            return position;
        }

        public Quaternion GetBrickRotation()
        {
            return transform.rotation;
        }

        public HashSet<Brick> GetScopedBricks()
        {
            var result = new HashSet<Brick>();

            if (IsPlacedOnBrick())
            {
                switch (m_Scope)
                {
                    case Scope.Brick:
                        {
                            result.Add(m_Brick);
                            break;
                        }
                    case Scope.ConnectedBricks:
                        {
                            result = m_Brick.GetConnectedBricks();
                            result.Add(m_Brick);
                            break;
                        }
                }
            }

            return result;
        }

        public Bounds GetScopedBounds(HashSet<Brick> scopedBricks, out List<MeshRenderer> scopedPartRenderers, out Vector3 scopedPivotOffset)
        {
            var result = new Bounds();

            scopedPartRenderers = new List<MeshRenderer>();

            // Find the bounds and part renderers of the scope.
            var firstPartBounds = true;
            foreach (var brick in scopedBricks)
            {
                foreach (var part in brick.parts)
                {
                    var partRenderers = part.GetComponentsInChildren<MeshRenderer>(true);

                    foreach (var partRenderer in partRenderers)
                    {
                        if (firstPartBounds)
                        {
                            result = partRenderer.bounds;
                            firstPartBounds = false;
                        }
                        else
                        {
                            result.Encapsulate(partRenderer.bounds);
                        }
                    }

                    scopedPartRenderers.AddRange(partRenderers);
                }
            }

            if (IsPlacedOnBrick())
            {
                // Compute pivot offset for entire scope.
                scopedPivotOffset = transform.InverseTransformVector(result.center - transform.position);
            }
            else
            {
                scopedPivotOffset = Vector3.zero;
            }

            return result;
        }

        protected virtual void Reset()
        {
            m_Brick = GetComponent<Brick>();

            if (m_Brick)
            {
                var brickBounds = new Bounds();

                // Get the brick bounds.
                var firstPartBounds = true;
                foreach (var part in m_Brick.parts)
                {
                    var partRenderers = part.GetComponentsInChildren<MeshRenderer>(true);

                    foreach (var partRenderer in partRenderers)
                    {
                        if (firstPartBounds)
                        {
                            brickBounds = partRenderer.bounds;
                            firstPartBounds = false;
                        }
                        else
                        {
                            brickBounds.Encapsulate(partRenderer.bounds);
                        }
                    }
                }

                // Compute pivot offset for the brick.
                m_BrickPivotOffset = transform.InverseTransformVector(brickBounds.center - transform.position);
            }
        }

        protected virtual void Awake()
        {
            m_ScopedBricks = GetScopedBricks();
            m_ScopedBounds = GetScopedBounds(m_ScopedBricks, out m_scopedPartRenderers, out m_ScopedPivotOffset);

            if (IsPlacedOnBrick())
            {
                // Get the decoration surfaces.
                foreach(var part in m_Brick.parts)
                {
                    var partRenderers = part.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var partRenderer in partRenderers)
                    {
                        if (partRenderer.transform.parent && partRenderer.transform.parent.name == "DecorationSurfaces")
                        {
                            m_DecorationSurfaceMaterials.Add(partRenderer.material);
                        }
                    }
                }
            }
        }

        protected void Flash()
        {
            LEGOBehaviourCoroutineManager.StartCoroutine(this, DoFlash(), true);
        }

        protected virtual void OnDrawGizmos()
        {
            var gizmoBounds = GetGizmoBounds();

            Gizmos.DrawCube(gizmoBounds.center, gizmoBounds.size);
            Gizmos.DrawIcon(gizmoBounds.center, m_IconPath);

            if (!IsPlacedOnBrick())
            {
                Gizmos.DrawIcon(gizmoBounds.center + Vector3.up * 2, "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Warning.png");
            }
        }

        IEnumerator DoFlash()
        {
            var flashTime = 0.0f;

            while (flashTime <= k_FlashLength)
            {
                var flashStrength = 1.0f - flashTime / k_FlashLength;

                foreach (var material in m_DecorationSurfaceMaterials)
                {
                    material.SetColor(s_EmissionColorID, flashStrength * m_FlashColour);
                }

                yield return new WaitForEndOfFrame();

                flashTime += Time.deltaTime;
            }

            foreach (var material in m_DecorationSurfaceMaterials)
            {
                material.SetColor(s_EmissionColorID, Color.black);
            }
        }
    }
}
