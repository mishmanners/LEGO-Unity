using LEGOModelImporter;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Unity.LEGO.Behaviours
{

    public class BrickMeshCombiner : MonoBehaviour
    {
        void Start()
        {
            var bricks = FindObjectsOfType<Brick>();

            var behaviours = FindObjectsOfType<LEGOBehaviour>();
            var behaviourScopes = new Dictionary<LEGOBehaviour, HashSet<Brick>>();

            var optimizationCandidates = new HashSet<GameObject>();
            var optimizationBounds = new Bounds();
            var firstOptimizationCandidate = true;

            foreach (var brick in bricks)
            {
                var behaviourOnBrick = false;

                foreach (var behaviour in behaviours)
                {
                    if (!behaviourScopes.ContainsKey(behaviour))
                    {
                        behaviourScopes.Add(behaviour, behaviour.GetScopedBricks());
                    }

                    if (behaviourScopes[behaviour].Contains(brick))
                    {
                        behaviourOnBrick = true;
                        break;
                    }
                }

                // Get rid of picking mesh - except for bricks with behaviours directly on them.
                if (!brick.GetComponent<LEGOBehaviour>() && !brick.gameObject.isStatic)
                {
                    Destroy(brick.GetComponent<MeshRenderer>());
                    Destroy(brick.GetComponent<MeshFilter>());
                }

                if (!behaviourOnBrick && !brick.gameObject.isStatic)
                {
                    foreach (var part in brick.parts)
                    {
                        optimizationCandidates.Add(part.gameObject);
                        if (firstOptimizationCandidate)
                        {
                            optimizationBounds = new Bounds(part.transform.position, Vector3.zero);
                            firstOptimizationCandidate = false;
                        }
                        else
                        {
                            optimizationBounds.Encapsulate(part.transform.position);
                        }
                    }
                }
            }

            var meshCombiner = gameObject.AddComponent<MeshCombiner>();
            meshCombiner.CombineParents = optimizationCandidates.ToList();
            meshCombiner.UseGrid = true;
            meshCombiner.GridCenter = optimizationBounds.center;
            meshCombiner.GridExtents = optimizationBounds.size;
            meshCombiner.GridResolution = new Vector3Int(4, 1, 4);
        }
    }
}