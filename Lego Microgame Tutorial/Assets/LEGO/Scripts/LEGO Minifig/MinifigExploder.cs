using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Minifig
{

    public static class MinifigExploder
    {
        private class EnableCollider : MonoBehaviour
        {
            private Collider theCollider;
            private int fixedUpdateDelay = 20;

            void Start()
            {
                theCollider = GetComponent<Collider>();
            }

            void FixedUpdate()
            {
                if (fixedUpdateDelay <= 0)
                {
                    theCollider.isTrigger = false;
                    Destroy(this);
                }

                fixedUpdateDelay--;
            }
        }

        private class BlinkAndDestroy : MonoBehaviour
        {
            public float timeLeft = 4.0f;

            private float blinkPeriod = 0.8f;
            private float blinkFrequency = 0.1f;

            private Renderer theRenderer;
            private float timeBlink; 

            void Start()
            {
                theRenderer = GetComponent<Renderer>();
                timeLeft += Random.Range(-0.3f, 0.3f);
            }

            void Update()
            {
                timeLeft -= Time.deltaTime;

                if (timeLeft <= blinkPeriod)
                {
                    if (timeBlink <= 0.0f)
                    {
                        timeBlink += blinkFrequency;
                        theRenderer.enabled = !theRenderer.enabled;
                    }

                    timeBlink -= Time.deltaTime;
                }

                if (timeLeft <= 0.0f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public static void Explode(Minifig minifig, Transform leftArmTip, Transform rightArmTip, Transform leftLegTip, Transform rightLegTip, Transform head, Vector3 speed, float angularSpeed)
        {
            const float spreadForce = 6.0f;
            const float removeDelay = 3.0f;

            // FIXME Add speed, angularSpeed and spreadForce to rigid body.

            // Use wrist, arm and armUp transforms.
            ExplodeLimb(minifig.transform, "Left arm", minifig.GetLeftArm(), leftArmTip.parent.parent.parent, leftArmTip.parent.parent, leftArmTip, 0.3f, speed, angularSpeed, spreadForce, removeDelay);
            ExplodeLimb(minifig.transform, "Right arm", minifig.GetRightArm(), rightArmTip.parent.parent.parent, rightArmTip.parent.parent, rightArmTip, 0.3f, speed, angularSpeed, spreadForce, removeDelay);

            var leftHandTransforms = new List<Transform> { leftArmTip, leftArmTip.parent };
            var rightHandTransforms = new List<Transform> { rightArmTip, rightArmTip.parent };
            var leftHandColliderCenters = new List<Vector3> { Vector3.forward * 0.2f, Vector3.zero };
            var rightHandColliderCenters = new List<Vector3> { Vector3.back * 0.2f, Vector3.zero };
            var handColliderSizes = new List<Vector3> { new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0.2f, 0.4f, 0.2f) };
            var handColliderInitialTriggers = new List<bool> { false, true };
            ExplodeWithTransforms(minifig.transform, "Left hand", new List<Transform> { minifig.GetLeftHand() }, leftHandTransforms, leftHandColliderCenters, handColliderSizes, handColliderInitialTriggers, speed, angularSpeed, spreadForce, removeDelay);
            ExplodeWithTransforms(minifig.transform, "Right hand", new List<Transform> { minifig.GetRightHand() }, rightHandTransforms, rightHandColliderCenters, handColliderSizes, handColliderInitialTriggers, speed, angularSpeed, spreadForce, removeDelay);

            // Use foot, leg and legUp transforms.
            ExplodeLimb(minifig.transform, "Left leg", minifig.GetLeftLeg(), leftLegTip.parent.parent, leftLegTip.parent, leftLegTip, 0.5f, speed, angularSpeed, spreadForce, removeDelay);
            ExplodeLimb(minifig.transform, "Right leg", minifig.GetRightLeg(), rightLegTip.parent.parent, rightLegTip.parent, rightLegTip, 0.5f, speed, angularSpeed, spreadForce, removeDelay);

            // Use hip transform.
            var hipTransforms = new List<Transform> { leftLegTip.parent.parent.parent, leftLegTip.parent.parent.parent, leftLegTip.parent.parent.parent, leftLegTip.parent.parent.parent };
            var hipColliderCenters = new List<Vector3> { Vector3.up * 0.4f, Vector3.zero, new Vector3(0.0f, 0.7f, 0.4f), new Vector3(0.0f, 0.7f, -0.4f) };
            var hipColliderSizes = new List<Vector3> { new Vector3(0.8f, 0.2f, 1.6f), new Vector3(0.7f, 0.7f, 0.2f), new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0.4f, 0.4f, 0.4f) };
            var hipColliderInitialTriggers = new List<bool> { false, false, true, true };
            ExplodeWithTransforms(minifig.transform, "Hip", minifig.GetHip(), hipTransforms, hipColliderCenters, hipColliderSizes, hipColliderInitialTriggers, speed, angularSpeed, spreadForce, removeDelay);

            // Use spine05 and spine01 transforms.
            var torsoTransforms = new List<Transform> { head.parent.parent.parent.parent, head.parent.parent.parent.parent.parent.parent.parent.parent };
            var torsoColliderCenters = new List<Vector3> { Vector3.up * 0.1f, Vector3.up * 0.3f };
            var torsoColliderSizes = new List<Vector3> { new Vector3(0.8f, 0.5f, 1.2f), new Vector3(0.8f, 0.5f, 1.4f) };
            var torsoColliderInitialTriggers = new List<bool> { false, false };
            ExplodeWithTransforms(minifig.transform, "Torso", minifig.GetTorso(), torsoTransforms, torsoColliderCenters, torsoColliderSizes, torsoColliderInitialTriggers, speed, angularSpeed, spreadForce, removeDelay);

            var headTransforms = new List<Transform> { head };
            var headColliderCenters = new List<Vector3> { Vector3.up * 0.48f };
            var headColliderSizes = new List<Vector3> { new Vector3(0.8f, 0.96f, 0.8f) };
            var headColliderInitialTriggers = new List<bool> { false };
            ExplodeWithTransforms(minifig.transform, "Head", minifig.GetHead(), headTransforms, headColliderCenters, headColliderSizes, headColliderInitialTriggers, speed, angularSpeed, spreadForce, removeDelay);

            var headAccessory = minifig.GetHeadAccessory();
            if (headAccessory)
            {
                var headAccessoryTransforms = new List<Transform> { head.GetChild(0) };
                // FIXME Get correct colliders for accessories.
                var headAccessoryColliderCenters = new List<Vector3> { Vector3.zero };
                var headAccessoryColliderSizes = new List<Vector3> { new Vector3(0.8f, 0.96f, 0.8f) };
                var headAccessoryColliderInitialTriggers = new List<bool> { true };
                ExplodeWithTransforms(minifig.transform, "Head accessory", new List<Transform> { headAccessory }, headAccessoryTransforms, headAccessoryColliderCenters, headAccessoryColliderSizes, headAccessoryColliderInitialTriggers, speed, angularSpeed, spreadForce + 1.5f, removeDelay, false);
            }

            // Hack for Santa's beard.
            var beardParent = head.GetChild(1);
            if (beardParent.childCount > 0)
            {
                var beardAccessory = beardParent.GetChild(0);
                if (beardAccessory)
                {
                    var beardAccessoryTransforms = new List<Transform> { beardAccessory };
                    var beardAccessoryColliderCenters = new List<Vector3> { Vector3.forward * 0.2f };
                    var beardAccessoryColliderSizes = new List<Vector3> { new Vector3(0.8f, 0.96f, 0.8f) };
                    var beardAccessoryColliderInitialTriggers = new List<bool> { true };
                    ExplodeWithTransforms(minifig.transform, "Beard accessory", new List<Transform> { beardAccessory }, beardAccessoryTransforms, beardAccessoryColliderCenters, beardAccessoryColliderSizes, beardAccessoryColliderInitialTriggers, speed, angularSpeed, spreadForce + 1.5f, removeDelay, false);
                }
            }
        }

        private static void ExplodeLimb(Transform parentTransform, string name, List<Transform> meshes, Transform root, Transform mid, Transform tip, float colliderWidth, Vector3 speed, float angularSpeed, float spreadForce, float removeDelay)
        {
            var resultGO = CreateMeshGO(name, meshes, removeDelay, true);

            AddOrientedColliderFromTwoPositions(resultGO, mid.position, root.position, colliderWidth);
            AddOrientedColliderFromTwoPositions(resultGO, tip.position, mid.position, colliderWidth);

            AddAndMoveRigidBody(resultGO, parentTransform, speed, angularSpeed, spreadForce);
        }

        private static void ExplodeWithTransforms(Transform parentTransform, string name, List<Transform> meshes, List<Transform> transforms, List<Vector3> colliderCenters, List<Vector3> colliderSizes, List<bool> colliderInitialTriggers, Vector3 speed, float angularSpeed, float spreadForce, float removeDelay, bool bakeMesh = true)
        {
            var resultGO = CreateMeshGO(name, meshes, removeDelay, bakeMesh);

            for (var i = 0; i < transforms.Count; ++i)
            {
                AddOrientedColliderFromTransform(resultGO, transforms[i], colliderCenters[i], colliderSizes[i], colliderInitialTriggers[i]);
            }

            AddAndMoveRigidBody(resultGO, parentTransform, speed, angularSpeed, spreadForce);
        }

        private static GameObject CreateMeshGO(string name, List<Transform> meshes, float removeDelay, bool bakeMesh)
        {
            var resultGO = new GameObject(name);
            resultGO.transform.localPosition = Vector3.zero;
            resultGO.transform.localRotation = Quaternion.identity;

            var resultMesh = new Mesh();

            var resultFilter = resultGO.AddComponent<MeshFilter>();
            resultFilter.sharedMesh = resultMesh;

            var resultRenderer = resultGO.AddComponent<MeshRenderer>();
            var materials = new List<Material>();

            var combineInstances = new List<CombineInstance>();
            for (var i = 0; i < meshes.Count; ++i)
            {
                var mesh = meshes[i];

                // Just parent the result with the parent of the first mesh.
                if (i == 0)
                {
                    resultGO.transform.SetParent(mesh.parent, false);
                }

                if (bakeMesh)
                {
                    var renderer = mesh.GetComponent<SkinnedMeshRenderer>();
                    materials.Add(renderer.sharedMaterial);

                    var bakedMesh = new Mesh();
                    renderer.BakeMesh(bakedMesh);

                    combineInstances.Add(new CombineInstance { mesh = bakedMesh });
                }
                else
                {
                    var renderers = mesh.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        materials.Add(renderer.sharedMaterial);
                    }

                    var filters = mesh.GetComponentsInChildren<MeshFilter>();
                    foreach (var filter in filters)
                    {
                        combineInstances.Add(new CombineInstance { mesh = filter.sharedMesh });
                    }
                }

                mesh.gameObject.SetActive(false);
            }

            resultRenderer.sharedMaterials = materials.ToArray();

            resultMesh.CombineMeshes(combineInstances.ToArray(), false, false);

            if (removeDelay > 0.0f)
            {
                var blinkAndDestroy = resultGO.AddComponent<BlinkAndDestroy>();
                blinkAndDestroy.timeLeft = removeDelay;
            }

            return resultGO;
        }

        private static void AddOrientedColliderFromTwoPositions(GameObject parent, Vector3 positionA, Vector3 positionB, float colliderWidth)
        {
            var position = (positionA + positionB) * 0.5f;
            var direction = positionA - positionB;

            var colliderGO = new GameObject("Collider");
            colliderGO.transform.parent = parent.transform;

            colliderGO.transform.position = position;
            colliderGO.transform.rotation = Quaternion.LookRotation(direction);

            var collider = colliderGO.AddComponent<BoxCollider>();
            collider.size = new Vector3(colliderWidth, colliderWidth, direction.magnitude);
        }

        private static void AddOrientedColliderFromTransform(GameObject parent, Transform transform, Vector3 colliderCenter, Vector3 colliderSize, bool colliderInitialTrigger)
        {
            var colliderGO = new GameObject("Collider");
            colliderGO.transform.parent = parent.transform;

            colliderGO.transform.position = transform.position;
            colliderGO.transform.rotation = transform.rotation;

            var collider = colliderGO.AddComponent<BoxCollider>();
            collider.center = colliderCenter;
            collider.size = colliderSize;
            collider.isTrigger = colliderInitialTrigger;

            if (colliderInitialTrigger)
            {
                colliderGO.AddComponent<EnableCollider>();
            }
        }

        private static void AddAndMoveRigidBody(GameObject go, Transform parentTransform, Vector3 speed, float angularSpeed, float spreadForce)
        {
            var rigidBody = go.AddComponent<Rigidbody>();
            rigidBody.AddForce(speed, ForceMode.VelocityChange);
            rigidBody.AddRelativeTorque(0.0f, angularSpeed * Mathf.Deg2Rad, 0.0f, ForceMode.VelocityChange);
            rigidBody.AddExplosionForce(spreadForce, parentTransform.position, 0.0f, 0.0f, ForceMode.VelocityChange);
        }
    }
}
