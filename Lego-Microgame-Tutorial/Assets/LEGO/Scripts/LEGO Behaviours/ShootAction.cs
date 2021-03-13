using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class ShootAction : RepeatableAction
    {
        [SerializeField, Tooltip("The projectile to launch.")]
        GameObject m_Projectile = null;

        [SerializeField, Range(1, 100), Tooltip("The velocity of the projectiles.")]
        float m_Velocity = 25f;

        [SerializeField, Range(0, 100), Tooltip("The accuracy in percent.")]
        int m_Accuracy = 90;

        [SerializeField, Tooltip("The time in seconds before projectiles disappears.")]
        float m_Lifetime = 2f;

        [SerializeField, Tooltip("Projectiles are affected by gravity.")]
        bool m_UseGravity = true;

        float m_Time;
        bool m_HasFired;

        protected override void Reset()
        {
            base.Reset();

            m_Scope = Scope.Brick;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Shoot Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Lifetime = Mathf.Max(1.0f, m_Lifetime);
            m_Pause = Mathf.Max(0.25f, m_Pause);
        }

        protected void Update()
        {
            if (m_Active)
            {
                m_Time += Time.deltaTime;

                if (!m_HasFired)
                {
                    Fire();
                    m_HasFired = true;
                }

                if (m_Time >= m_Pause)
                {
                    m_Time -= m_Pause;
                    m_HasFired = false;
                    m_Active = m_Repeat;
                }
            }
        }

        void Fire()
        {
            if (m_Projectile)
            {
                var go = Instantiate(m_Projectile);

                go.transform.position = transform.TransformPoint(m_ScopedPivotOffset);

                var accuracyToDegrees = 90.0f - 90.0f * m_Accuracy / 100.0f;
                var randomSpread = Random.insideUnitCircle * Mathf.Tan(accuracyToDegrees * Mathf.Deg2Rad * 0.5f);
                go.transform.rotation = transform.rotation * Quaternion.LookRotation(Vector3.forward + Vector3.right * randomSpread.x + Vector3.up * randomSpread.y);

                var projectile = go.GetComponent<Projectile>();
                if (projectile)
                {
                    projectile.Init(m_ScopedBricks, m_Velocity, m_UseGravity, m_Lifetime);
                }

                PlayAudio();
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!m_Projectile)
            {
                var gizmoBounds = GetGizmoBounds();

                Gizmos.DrawIcon(gizmoBounds.center + Vector3.up, "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Warning.png");
            }
        }
    }
}
