using LEGOModelImporter;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;
using System.Collections.Generic;

namespace Unity.LEGO.Behaviours
{
    public class BrickColliderCombiner : MonoBehaviour
    {
        static Dictionary<Part, List<Collider>> s_OriginalColliders = new Dictionary<Part, List<Collider>>();

        void Awake()
        {
            var bricks = FindObjectsOfType<Brick>();

            var behaviours = FindObjectsOfType<LEGOBehaviour>();
            var behaviourScopes = new Dictionary<LEGOBehaviour, HashSet<Brick>>();

            foreach (var brick in bricks)
            {
                var combinableBehaviourOnBrick = false;

                foreach (var behaviour in behaviours)
                {
                    var behaviourType = behaviour.GetType();
                    if (behaviourType == typeof(HazardAction) || behaviourType == typeof(PickupAction) || behaviourType == typeof(TouchTrigger) || behaviour is MovementAction)
                    {
                        if (!behaviourScopes.ContainsKey(behaviour))
                        {
                            behaviourScopes.Add(behaviour, behaviour.GetScopedBricks());
                        }

                        if (behaviourScopes[behaviour].Contains(brick))
                        {
                            combinableBehaviourOnBrick = true;
                            break;
                        }
                    }
                }

                if (combinableBehaviourOnBrick)
                {
                    // Combine all colliders in part into one box collider.
                    foreach (var part in brick.parts)
                    {
                        if (part.colliders.Count > 0)
                        {
                            var collidersParent = part.transform.Find("Colliders");

                            var min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                            var max = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
                            foreach (var collider in part.colliders)
                            {
                                var colliderType = collider.GetType();
                                if (colliderType == typeof(BoxCollider))
                                {
                                    var boxCollider = (BoxCollider)collider;
                                    var c0 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(-0.5f, -0.5f, -0.5f), boxCollider.size)));
                                    var c1 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(-0.5f, -0.5f, 0.5f), boxCollider.size)));
                                    var c2 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(-0.5f, 0.5f, -0.5f), boxCollider.size)));
                                    var c3 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(-0.5f, 0.5f, 0.5f), boxCollider.size)));
                                    var c4 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(0.5f, -0.5f, -0.5f), boxCollider.size)));
                                    var c5 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(0.5f, -0.5f, 0.5f), boxCollider.size)));
                                    var c6 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(0.5f, 0.5f, -0.5f), boxCollider.size)));
                                    var c7 = part.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center + Vector3.Scale(new Vector3(0.5f, 0.5f, 0.5f), boxCollider.size)));

                                    min = Vector3.Min(min, c0);
                                    min = Vector3.Min(min, c1);
                                    min = Vector3.Min(min, c2);
                                    min = Vector3.Min(min, c3);
                                    min = Vector3.Min(min, c4);
                                    min = Vector3.Min(min, c5);
                                    min = Vector3.Min(min, c6);
                                    min = Vector3.Min(min, c7);

                                    max = Vector3.Max(max, c0);
                                    max = Vector3.Max(max, c1);
                                    max = Vector3.Max(max, c2);
                                    max = Vector3.Max(max, c3);
                                    max = Vector3.Max(max, c4);
                                    max = Vector3.Max(max, c5);
                                    max = Vector3.Max(max, c6);
                                    max = Vector3.Max(max, c7);
                                }
                                else if (colliderType == typeof(SphereCollider))
                                {
                                    var sphereCollider = (SphereCollider)collider;
                                    var c = part.transform.InverseTransformPoint(sphereCollider.transform.TransformPoint(sphereCollider.center));
                                    min = Vector3.Min(min, c - Vector3.one * sphereCollider.radius);
                                    max = Vector3.Min(max, c + Vector3.one * sphereCollider.radius);
                                }

                                collider.gameObject.SetActive(false);
                            }

                            var combinedCollisionBox = new GameObject("Collision Box");
                            combinedCollisionBox.transform.parent = collidersParent;
                            combinedCollisionBox.transform.localPosition = Vector3.zero;
                            combinedCollisionBox.transform.localRotation = Quaternion.identity;

                            var combinedCollider = combinedCollisionBox.gameObject.AddComponent<BoxCollider>();
                            combinedCollider.center = (min + max) * 0.5f;
                            combinedCollider.size = max - min;

                            s_OriginalColliders[part] = part.colliders;

                            part.colliders = new List<Collider>();
                            part.colliders.Add(combinedCollider);
                        }
                    }
                }
            }
        }

        public static void RestoreOriginalColliders(Part part)
        {
            if (s_OriginalColliders.ContainsKey(part))
            {
                foreach (var collider in part.colliders)
                {
                    Destroy(collider.gameObject);
                }

                part.colliders = s_OriginalColliders[part];

                foreach (var collider in part.colliders)
                {
                    collider.gameObject.SetActive(true);
                }
            }
        }
    }
}