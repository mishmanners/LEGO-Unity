using UnityEngine;
using Unity.LEGO.Behaviours.Actions;
using System.Linq;
using Unity.LEGO.Behaviours.Triggers;
using System.Collections.Generic;
using Unity.InteractiveTutorials;
using LEGOModelImporter;
using UnityEditor;

namespace Unity.LEGO.Tutorials
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "DeletionCriteria", menuName = "Tutorials/LEGO/DeletionCriteria")]
    class DeletionCriteria : ScriptableObject
    {
        WinAction winAction;
        public TouchTrigger TouchTrigger { get; private set; }
        GameObject triggerCopy;

        [SerializeField]
        TutorialPage tutorialPage;

        public void RestoreTouchTriggerIfMissing()
        {
            if (TouchTrigger)
            {
                if (triggerCopy)
                {
                    DestroyImmediate(triggerCopy);
                }
                return;
            }
            List<Trigger> triggers = winAction.GetTargetingTriggers();
            if (triggers.Count > 1 && triggers[0] as PickupTrigger)
            {
                if (triggerCopy)
                {
                    DestroyImmediate(triggerCopy);
                }
                return;
            }

            if (!triggerCopy) { return; }
            triggerCopy.SetActive(true);
            triggerCopy.name = triggerCopy.name.Replace("(Clone)", "");

            // Re-restablish connectivity
            var brick = triggerCopy.GetComponent<Brick>();
            foreach (var part in brick.parts)
            {
                foreach (var field in part.connectivity.connectionFields)
                {
                    var query = field.QueryConnections(out _);
                    foreach (var (connection, otherConnection) in query)
                    {
                        var position = connection.field.GetPosition(connection);
                        if (ConnectionField.IsConnectionValid(connection, otherConnection, position))
                        {
                            var connections = ConnectionField.Connect(connection, otherConnection, position);
                            if (connections.Count > 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateTouchTriggerCriteriaReference()
        {
            winAction = FindObjectsOfType<WinAction>().Where(action => action.CompareTag("TutorialRequirement")).FirstOrDefault();
            if (!winAction)
            {
                Debug.LogError("In order to be completed, this tutorial expects exactly one 'WinAction' brick tagged as 'TutorialRequirement', to which a 'TouchTrigger' brick is connected");
                return;
            }

            if (!TouchTrigger)
            {
                TouchTrigger = winAction.GetTargetingTriggers().First() as TouchTrigger;
                if (!TouchTrigger)
                {
                    Debug.LogError("'Touch Trigger' brick not found. In order to be completed, this tutorial expects exactly one 'WinAction' brick tagged as 'TutorialRequirement', to which a 'TouchTrigger' brick is connected");
                    return;
                }
            }


            ObjectReference referenceToBrick = new ObjectReference();
            referenceToBrick.sceneObjectReference = new SceneObjectReference();
            referenceToBrick.sceneObjectReference.Update(TouchTrigger.gameObject);

            var criteria = tutorialPage.paragraphs[2].criteria;
            foreach (var criterion in criteria)
            {
                if (criterion.criterion as RequiredSelectionCriterion)
                {
                    (criterion.criterion as RequiredSelectionCriterion).SetObjectReferences(new List<ObjectReference>() { referenceToBrick });
                    EditorUtility.SetDirty(tutorialPage);
                    AssetDatabase.SaveAssets();
                    break;
                }
            }
        }

        public void FindWinBrick()
        {
            winAction = FindObjectsOfType<WinAction>().Where(action => action.CompareTag("TutorialRequirement")).FirstOrDefault();
            if (!winAction)
            {
                Debug.LogError("In order to be completed, this tutorial expects exactly one 'WinAction' brick tagged as 'TutorialRequirement', to which a 'TouchTrigger' brick is connected");
                return;
            }

            if (!TouchTrigger)
            {
                TouchTrigger = winAction.GetTargetingTriggers().First() as TouchTrigger;
            }

            if (!TouchTrigger || triggerCopy) { return; }

            GameObject touchTriggerObject = TouchTrigger.gameObject;
            Transform touchTriggerTransform = touchTriggerObject.transform;
            triggerCopy = Instantiate(touchTriggerObject, touchTriggerTransform.parent);

            // Remove one-way connectivity references
            var brick = triggerCopy.GetComponent<Brick>();
            foreach (var part in brick.parts)
            {
                foreach (var field in part.connectivity.connectionFields)
                {
                    field.connected.Clear();
                    for (var i = 0; i < field.connections.Length; i++)
                    {
                        field.connectedTo[i].field = null;
                        field.connectedTo[i].indexOfConnection = -1;
                    }
                }
            }

            // The original TouchTrigger is referenced by the previous tutorial steps so it has a
            // SceneObjectGuid for it and we need to get rid duplicate GUID for the the clone.
            var sceneObjGuid = triggerCopy.GetComponent<SceneObjectGUIDComponent>();
            if (sceneObjGuid != null)
                DestroyImmediate(sceneObjGuid);
            triggerCopy.transform.localPosition = touchTriggerTransform.localPosition;
            triggerCopy.transform.localEulerAngles = touchTriggerTransform.localEulerAngles;
            triggerCopy.transform.localScale = touchTriggerTransform.localScale;
            triggerCopy.layer = touchTriggerObject.layer;
            triggerCopy.tag = touchTriggerObject.tag;
            triggerCopy.hideFlags = touchTriggerObject.hideFlags;
            triggerCopy.SetActive(false);
        }

        public bool HasBrickBeenDeleted()
        {
            return winAction && !TouchTrigger;
        }
    }
}
