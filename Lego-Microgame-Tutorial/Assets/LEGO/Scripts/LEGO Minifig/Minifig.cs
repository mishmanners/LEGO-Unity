using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Minifig
{

    public class Minifig : MonoBehaviour
    {

#pragma warning disable 649
        [HideInInspector, SerializeField]
        Transform headAccessoryLocator;

        [HideInInspector, SerializeField]
        Transform torsoBackGeometry;

        [HideInInspector, SerializeField]
        Transform torsoFrontGeometry;

        [HideInInspector, SerializeField]
        Transform torsoGeometry;

        [HideInInspector, SerializeField]
        List<Transform> armsGeometry;

        [HideInInspector, SerializeField]
        List<Transform> handsGeometry;

        [HideInInspector, SerializeField]
        List<Transform> legsGeometry;

        [HideInInspector, SerializeField]
        Transform headGeometry;

        [HideInInspector, SerializeField]
        Transform faceGeometry;
#pragma warning restore 649

        public void SetHeadAccessory(GameObject accessory)
        {
            accessory.transform.SetParent(headAccessoryLocator, false);
        }

        public void SetTorsoMaterials(Dictionary<string, Material> materials)
        {
            SetMaterial(torsoGeometry, materials["Torso_Main"]);

            SetMaterial(torsoFrontGeometry, materials["Torso_Front"]);

            SetMaterial(torsoBackGeometry, materials["Torso_Back"]);
        }

        public void SetArmMaterial(Material material)
        {
            SetMaterial(armsGeometry, material);
        }

        public void SetHandMaterial(Material material)
        {
            SetMaterial(handsGeometry, material);
        }

        public void SetLegMaterials(Dictionary<string, Material> materials)
        {
            SetMaterials(legsGeometry, materials);
        }

        public void SetFaceMaterial(Material material)
        {
            SetMaterial(faceGeometry, material);
        }

        public List<Transform> GetTorso()
        {
            return new List<Transform> { torsoGeometry, torsoFrontGeometry, torsoBackGeometry };
        }

        public List<Transform> GetLeftArm()
        {
            return armsGeometry.GetRange(0, 2);
        }

        public List<Transform> GetRightArm()
        {
            return armsGeometry.GetRange(2, 2);
        }

        public List<Transform> GetHip()
        {
            return legsGeometry.GetRange(0, 3);
        }

        public List<Transform> GetLeftLeg()
        {
            return legsGeometry.GetRange(3, 4);
        }

        public List<Transform> GetRightLeg()
        {
            return legsGeometry.GetRange(7, 4);
        }

        public Transform GetLeftHand()
        {
            return handsGeometry[0];
        }

        public Transform GetRightHand()
        {
            return handsGeometry[1];
        }

        public List<Transform> GetHead()
        {
            return new List<Transform> { headGeometry, faceGeometry };
        }

        public Transform GetHeadAccessory()
        {
            if (headAccessoryLocator.childCount > 0)
            {
                return headAccessoryLocator.GetChild(0);
            } else
            {
                return null;
            }
        }

        public Transform GetFace()
        {
            return faceGeometry;
        }

        void SetMaterials(List<Transform> transforms, Dictionary<string, Material> materials)
        {
            foreach (var transform in transforms)
            {
                SetMaterial(transform, materials[transform.name]);
            }
        }

        void SetMaterial(List<Transform> transforms, Material material)
        {
            foreach (var transform in transforms)
            {
                SetMaterial(transform, material);
            }
        }

        void SetMaterial(Transform transform, Material material)
        {
            transform.GetComponent<Renderer>().material = material;
        }
    }

}