using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class AliveAction : Action
    {
        public enum Type
        {
            Creature,
            Robot
        }

        [SerializeField, Tooltip("Move like a creature.\nor\nMove like a robot.")]
        protected Type m_Type;

        enum State
        {
            WaitingToMove,
            Moving
        }

        State m_State;
        float m_CurrentTime;
        AliveMovement m_CurrentMovement;
        float m_Pause;
        Vector3 m_Offset;
        Vector3 m_RotationOffset;
        Vector3 m_Pivot;
        List<AliveMovement> m_Movements = new List<AliveMovement> { new Breathe(), new Jump(), new Look(), new Shake(), new Wobble() };

        const float k_MaxPauseTime = 1.0f;

        List<Shader> m_OriginalShaders = new List<Shader>();
        List<Material> m_Materials = new List<Material>();

        static readonly int s_DeformMatrix1ID = Shader.PropertyToID("_DeformMatrix1");
        static readonly int s_DeformMatrix2ID = Shader.PropertyToID("_DeformMatrix2");
        static readonly int s_DeformMatrix3ID = Shader.PropertyToID("_DeformMatrix3");

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Alive Action.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Change the shader of all scoped part renderers.
                foreach(var partRenderer in m_scopedPartRenderers)
                {
                    m_OriginalShaders.Add(partRenderer.sharedMaterial.shader);

                    // The renderQueue value is reset when changing the shader, so transfer it.
                    var renderQueue = partRenderer.material.renderQueue;
                    partRenderer.material.shader = Shader.Find("Deformed");
                    partRenderer.material.renderQueue = renderQueue;

                    m_Materials.Add(partRenderer.material);
                }

                m_Pivot = transform.InverseTransformVector(new Vector3(m_ScopedBounds.center.x, m_ScopedBounds.min.y, m_ScopedBounds.center.z) - transform.position);

                m_Pause = Random.Range(0.0f, k_MaxPauseTime);
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                // Update time.
                m_CurrentTime += Time.deltaTime;

                // Move.
                if (m_State == State.Moving)
                {
                    m_CurrentMovement.UpdateMovement(m_CurrentTime);

                    // Move and rotate bricks.
                    var offsetDelta = m_CurrentMovement.GetOffset() - m_Offset;
                    var rotationDelta = m_CurrentMovement.GetRotation() - m_RotationOffset;
                    var worldPivot = transform.position + transform.TransformVector(m_Pivot);
                    foreach (var brick in m_ScopedBricks)
                    {
                        brick.transform.RotateAround(worldPivot, rotationDelta, rotationDelta.magnitude);
                        brick.transform.position += offsetDelta;
                    }

                    m_Offset += offsetDelta;
                    m_RotationOffset += rotationDelta;

                    var deformMatrix = Matrix4x4.Translate(worldPivot) * Matrix4x4.Scale(m_CurrentMovement.GetScale()) * Matrix4x4.Translate(-worldPivot);

                    foreach (var material in m_Materials)
                    {
                        material.SetVector(s_DeformMatrix1ID, deformMatrix.GetRow(0));
                        material.SetVector(s_DeformMatrix2ID, deformMatrix.GetRow(1));
                        material.SetVector(s_DeformMatrix3ID, deformMatrix.GetRow(2));
                    }

                    if (m_CurrentTime >= m_CurrentMovement.Time)
                    {
                        m_CurrentTime -= m_CurrentMovement.Time;
                        m_Pause = Random.Range(0.0f, k_MaxPauseTime);
                        m_State = State.WaitingToMove;
                    }
                }

                // Waiting to move.
                if (m_State == State.WaitingToMove)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_CurrentMovement = m_Movements[Random.Range(0, m_Movements.Count)];
                        m_CurrentMovement.Initialise(m_ScopedBounds, m_Type);

                        if (m_CurrentMovement.GetType() != typeof(Breathe) && m_CurrentMovement.GetType() != typeof(Shake))
                        {
                            PlayAudio();
                        }

                        m_State = State.Moving;
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Check if the original materials have been stored for all scoped part renderers.
            if (m_scopedPartRenderers.Count > m_OriginalShaders.Count)
            {
                return;
            }

            // Change the material back to original for all scoped part renderers.
            for (var i = 0; i < m_scopedPartRenderers.Count; ++i)
            {
                if (m_scopedPartRenderers[i])
                {
                    var partRenderer = m_scopedPartRenderers[i];
                    // The renderQueue value is reset when changing the shader, so transfer it.
                    var renderQueue = partRenderer.material.renderQueue;
                    partRenderer.material.shader = m_OriginalShaders[i];
                    partRenderer.material.renderQueue = renderQueue;
                }
            }
        }
    }
}
