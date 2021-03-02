using UnityEngine;

namespace Unity.LEGO.Utilities
{

    public class Rotate : MonoBehaviour
    {
        // A simple script that rotates the game object around world up axis.

        [SerializeField, Tooltip("The speed in degrees per second to rotate around the world up axis.")]
        float m_Speed = 5.0f;

        void Update()
        {
            transform.RotateAround(transform.position, Vector3.up, Time.deltaTime * m_Speed);
        }
    }
}