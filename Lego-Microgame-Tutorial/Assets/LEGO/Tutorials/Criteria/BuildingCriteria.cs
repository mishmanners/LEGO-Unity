using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;
using System.Linq;
using LEGOModelImporter;
using Unity.LEGO.Behaviours.Triggers;
using Unity.InteractiveTutorials;
using Unity.LEGO.EditorExt;

namespace Unity.LEGO.Tutorials
{
    /// <summary>
    /// Contains all the callbacks needed for the tutorial steps that check for connected bricks
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCriteria", menuName = "Tutorials/LEGO/BuildingCriteria")]
    class BuildingCriteria : ScriptableObject
    {
        WinAction winAction;
        GameObject fenceInstance;
        GameObject elevatorInstance;
        public FutureObjectReference futureElevatorBrickInstance = default;
        public FutureObjectReference futureElevatorTouchTriggerInstance = default;
        public FutureObjectReference futureGaloInstance = default;
        public FutureObjectReference futureAltarInstance = default;
        public FutureObjectReference futureExplodeInstance = default;
        GameObject ElevatorBrickInstance { get { return futureElevatorBrickInstance.sceneObjectReference.ReferencedObjectAsGameObject; } }
        GameObject ElevatorTouchTriggerInstance { get { return futureElevatorTouchTriggerInstance.sceneObjectReference.ReferencedObjectAsGameObject; } }
        GameObject GaloInstance { get { return futureGaloInstance.sceneObjectReference.ReferencedObjectAsGameObject; } }
        GameObject AltarInstance { get { return futureAltarInstance.sceneObjectReference.ReferencedObjectAsGameObject; } }
        GameObject ExplodeInstance { get { return futureExplodeInstance.sceneObjectReference.ReferencedObjectAsGameObject; } }
        TouchTrigger touchTriggerConnectedToAltar;

        public bool IsBrickBuildingEnabled()
        {
            return SceneBrickBuilder.GetToggleBrickBuildingStatus();
        }

        public bool IsAnyElevatorConnected()
        {
            if (!ElevatorBrickInstance) { return false; }
            return ElevatorBrickInstance.GetComponent<Brick>().GetConnectedBricks(false).Any();
        }

        public bool HasTouchTriggerBrickBeenConnectedToElevator()
        {
            if (!ElevatorTouchTriggerInstance) { return false; }

            Brick brick = ElevatorTouchTriggerInstance.GetComponent<Brick>();
            if (!brick) { return false; }

            if (!elevatorInstance)
            {
                elevatorInstance = ElevatorBrickInstance.GetComponent<Brick>().GetConnectedBricks(false).First().transform.parent.parent.gameObject;
            }

            foreach (var connectedBrick in brick.GetConnectedBricks(true))
            {
                if ((connectedBrick.gameObject == ElevatorBrickInstance)
                || (connectedBrick.transform.parent.parent.gameObject == elevatorInstance))
                {
                    return true;
                }
            }
            return false;
        }

        public void FindWinBrick()
        {
            winAction = FindObjectsOfType<WinAction>().Where(action => action.CompareTag("TutorialRequirement")).FirstOrDefault();
            if (!winAction)
            {
                Debug.LogError("In order to be completed, this tutorial expects exactly one 'WinAction' brick tagged as 'TutorialRequirement'");
                return;
            }
        }

        public bool HasPickupTriggerBeenConnected()
        {
            if (!winAction) //needed because of AutoAdvance triggering this immediately instead of respecting the setup callbacks
            {
                FindWinBrick();
                return false;
            }
            Trigger trigger = winAction.GetTargetingTriggers().FirstOrDefault();
            if (!trigger) { return false; }
            return (trigger as PickupTrigger) != null;
        }

        public void FindFence()
        {
            GameObject[] tutorialObjects = GameObject.FindGameObjectsWithTag("TutorialRequirement");
            foreach (var item in tutorialObjects)
            {
                if (item.name != "Fence") { continue; }
                fenceInstance = item;
                return;
            }
            Debug.LogError("In order to be completed, this tutorial expects exactly one 'Fence' object tagged as 'TutorialRequirement'");
            fenceInstance = null;
        }

        public bool HasShootBrickBeenConnectedToGalo()
        {
            if (!GaloInstance) //needed because of AutoAdvance triggering this immediately instead of respecting the setup callbacks
            {
                return false;
            }
            Brick shootActionBrick = FindObjectsOfType<ShootAction>()
                                      .Select(shoot => shoot.GetComponent<Brick>())
                                      .Where(brick => brick != null && brick.GetConnectedBricks(false).Any()).FirstOrDefault();
            if (!shootActionBrick) { return false; }
            return shootActionBrick.GetConnectedBricks(false).First().transform.root.gameObject == GaloInstance;
        }

        public bool HasLookAtBrickBeenConnectedToGalo()
        {
            if (!GaloInstance) //needed because of AutoAdvance triggering this immediately instead of respecting the setup callbacks
            {
                return false;
            }
            Brick lookAtActionBrick = FindObjectsOfType<LookAtAction>()
                                      .Select(shoot => shoot.GetComponent<Brick>())
                                      .Where(brick => brick != null && brick.GetConnectedBricks(false).Any()).FirstOrDefault();
            if (!lookAtActionBrick) { return false; }
            return lookAtActionBrick.GetConnectedBricks(true).Where(brick => brick.transform.root.gameObject == GaloInstance).Any();
        }

        public bool HasExplodeBrickBeenConnectedToFence()
        {
            if (!fenceInstance) //needed because of AutoAdvance triggering this immediately instead of respecting the setup callbacks
            {
                FindFence();
                return false;
            }
            Brick searchedBrick = FindObjectsOfType<ExplodeAction>()
                                      .Select(shoot => shoot.GetComponent<Brick>())
                                      .Where(brick => brick != null && brick.GetConnectedBricks(false).Any()).FirstOrDefault();
            if (!searchedBrick) { return false; }

            //note: the first check is needed for the HazardBrick, which has a different parent structure
            return searchedBrick.GetConnectedBricks(true).Where(brick => brick.transform.parent.gameObject == fenceInstance || brick.transform.parent.parent.gameObject == fenceInstance).Any();
        }

        public bool HasTouchTriggerBrickBeenConnectedToAltar()
        {
            if (!AltarInstance) //needed because of AutoAdvance triggering this immediately instead of respecting the setup callbacks
            {
                return false;
            }
            TouchTrigger[] triggersInScene = FindObjectsOfType<TouchTrigger>();
            foreach (var touchTrigger in triggersInScene)
            {
                Brick brick = touchTrigger.GetComponent<Brick>();
                if (!brick) { continue; }
                foreach (var connectedBrick in brick.GetConnectedBricks(false))
                {
                    if (connectedBrick.transform.root.gameObject != AltarInstance) { continue; }
                    touchTriggerConnectedToAltar = touchTrigger;
                    return true;
                }
            }
            return false;
        }

        public bool HasAltarTouchTriggerBeenSetUpProperly()
        {
            (Trigger.Target target, int actionsCount) = touchTriggerConnectedToAltar.GetTargetAndActionsCount();
            return (target == Trigger.Target.SpecificActions) && (actionsCount == 1);
        }

        public bool HasExplodeTriggerBeenAssignedToTouchTrigger()
        {
            if (EditorWindow.HasOpenInstances<ActionPicker>()) { return false; }
            
            System.Collections.Generic.HashSet<Action> actions = touchTriggerConnectedToAltar.GetTargetedActions();
            if (actions.Count == 0) { return false; }
            return actions.ElementAt(0) as ExplodeAction;
        }

        public bool SelectedObjectIsNotBeingMoved()
        {
            return SceneBrickBuilder.CurrentSelectionState != SceneBrickBuilder.SelectionState.moving;
        }
    }
}
