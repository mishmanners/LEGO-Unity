// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace LEGOModelImporter
{
    [Serializable]
    public class Connection
    {
        public static readonly string connectivityConnectorLayerName = "ConnectivityConnector";
        public static readonly string connectivityReceptorLayerName = "ConnectivityReceptor";


        public enum ConnectionType
        {
            knob = 0,
            hollowKnob,
            knobFitInPegHole,           // Function similar to knob, plus fits in pegHole
            hollowKnobFitInPegHole,     // Function similar to hollowKnob, plus fits in pegHole
            squareKnob,                 // Fit only in square anti knob

            tube,                       // Connector on bottom of a round 1x1
            tubeWithRib,                // Function similar to tube, but has ribs sticking out to the sides rejecting tubeGrabber
            bottomTube,                 // Function similar to tube, but cannot connect to tubeGap or be occupied
            bottomTubeWithRib,          // Function similar to tubeWithRib, but cannot connect to tubeGap or be occupied

            secondaryPin,
            secondaryPinWithRib,
            secondaryPinWithTinyPinReceptor,
            secondaryPinWithRibAndTinyPinReceptor,

            fixedTube,                  // Tube that can not rotate, can only connect in the 4 90 degree orientations
            fixedTubeWithAntiKnob,      // Function similar to fixedTube, but knobs can also connect

            antiKnob,                   //
                                        //                       antiKnobWithSecondaryPin,   // Has a pin inside that reject knobs but not hollow knobs

            pegHole,                    // anti knob at end of peg holes, only connects to special knobs

            squareAntiKnob,             // Function similar to anti knob, plus squareKnob fit in it

            tubeGap,                    // Receptor located diagonally between 4 knobs
            tubeGrabber,                // Wine glass that connect to tube, rejects tubes "WithRib"

            tinyPin,
            tinyPinReceptor,

            edge,                       // edge of any kind and rib
            edgeGap,

            knobReject,                 // Reject knobs, but not hollow knobs, does not connect to either

            powerFuncLeftTop,
            powerFuncRightTop,
            powerFuncLeftBottom,
            powerFuncRightBottom,

            voidFeature,

            duploKnob,

            duploHollowKnob,
            duploAntiKnob,
            duploTube,
            duploFixedTube,
            duploTubeGap,
            duploAnimalKnob,
            duploAnimalTube,

            secondaryPinReceptor,       // Function similar to hollowKnob, but cannot connect on the outside

            duploFixedAnimalTube,
            duploSecondaryPinWithRib,   // Connect to duploHollowKnob but reject duploAnimalKnob
            duploSecondaryPin,          // Connect to both duploHollowKnob and duploAnimalKnob
            duploKnobReject,            // Reject duploKnob, but not duploHollowKnob nor duploAnimalKnob, does not connect to either of the latter
        }

        /// <summary>
        /// Every ConnectionPoint has some flags.
        /// </summary>
        public enum Flags
        {
            squareFeature = 1 << 0,                        // This feature is square (used for geometry optimization)
            roundFeature = 1 << 1,                         // This feature is round (used for geometry optimization)
            knobWithHole = 1 << 2,                         // Use hollow knob collision volumes ##################################### TODO: remove, using IsTechnicKnob() instead
            knobWithMiniFigHandHole = 1 << 3,              // Use mini-fig hand knob collision volumes
            knobWithSingleCollision = 1 << 4,              // Use single "hole" collision volume off to one side
            singleFeature = 1 << 5,                        // Feature is from a "single" sized field
            receptorDontRemoveKnobCollision = 1 << 6,      // When connecting don't remove the knob collision volume
            knobWithoutCollision = 1 << 7,                 // Knob should never have active collision volumes
        }

        public enum ConnectionMatch
        {
            reject,
            ignore,
            connect
        }

        public static readonly Flags flagsCoveringKnob = Flags.squareFeature | Flags.roundFeature;
        public static readonly Flags flagsCoveringTube = Flags.squareFeature;

        public ConnectionType connectionType;
        public int quadrants;
        public int index;
        public Flags flags;
        public ConnectionField field;
        public Knob knob;
        public List<Tube> tubes;

        public static void RegisterPrefabChanges(UnityEngine.Object changedObject)
        {
#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfAnyPrefab(changedObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(changedObject);
            }
#endif
        }

        public static bool IsKnob(Connection connection)
        {
            switch(connection.connectionType)
            {
                case ConnectionType.knob:
                case ConnectionType.hollowKnob:
                case ConnectionType.hollowKnobFitInPegHole:
                case ConnectionType.knobFitInPegHole:
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether a connection type can connect with anything
        /// </summary>
        /// <param name="connection">The connection to check</param>
        /// <returns></returns>
        public static bool IsConnectableType(Connection connection)
        {
            for(var i = 0; i < connectivityMatrix.GetLength(0); i++)
            {
                if(connectivityMatrix[(int)connection.connectionType,i] == ConnectionMatch.connect)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateKnobsAndTubes()
        {
            if (knob)
            {
                knob.UpdateVisibility();     
                RegisterPrefabChanges(knob.gameObject);
            }

            foreach (var tube in tubes)
            {
                if (tube)
                {  
                    tube.UpdateVisibility();
                    RegisterPrefabChanges(tube.gameObject);
                }
            }
        }

        public bool IsVisible()
        {
            var visible = false;
            if(knob)
            {
                visible = visible || knob.IsVisible();
            }

            foreach(var tube in tubes)
            {
                if(tube)
                {
                    visible = visible || tube.IsVisible();
                }
            }
            return visible;
        }

        public bool IsRelevantForTube()
        {
            // FIXME Temporary fix to tube removal while we work on connections that are related/non-rejecting but not connected.
            return connectionType == ConnectionType.antiKnob || connectionType == ConnectionType.squareAntiKnob;
        }

        /// <summary>
        /// Check whether a connection has been broken
        /// </summary>
        /// <returns>Whether or not a connection is still valid</returns>
        public static bool ConnectionValid(Connection lhs, Connection rhs, out ConnectionMatch match)
        {
            if (lhs == null || rhs == null)
            {
                match = ConnectionMatch.reject;
                return false;
            }

            match = MatchTypes(lhs.connectionType, rhs.connectionType);
            if (match == ConnectionMatch.reject)
            {
                return false;
            }

            var POS_EPSILON = 0.1f;
            var ROT_EPSILON = 3.0f;
            var lhsPosition = lhs.field.GetPosition(lhs);
            var rhsPosition = rhs.field.GetPosition(rhs);
            return Vector3.Distance(lhsPosition, rhsPosition) < POS_EPSILON && Vector3.Angle(lhs.field.transform.up, rhs.field.transform.up) < ROT_EPSILON;
        }   

        /// <summary>
        /// The connectivity matrix
        /// Generated from connectivity matrix sheet through python script load_connectivity_matrix.py
        /// </summary>
        /// <value></value>
        private static ConnectionMatch[,] connectivityMatrix = new ConnectionMatch[43, 43] {
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.connect, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.connect, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
            {ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.ignore, ConnectionMatch.reject, ConnectionMatch.reject, ConnectionMatch.reject, },
        };

        /// <summary>
        /// Get the connection relationship between two types
        /// </summary>
        public static ConnectionMatch MatchTypes(ConnectionType lhs, ConnectionType rhs)
        {
            return connectivityMatrix[(int)lhs, (int)rhs];
        }

        public static Vector3 GetPreconnectOffset(Connection dst)
        {
            switch (dst.connectionType)
            {
                case ConnectionType.knob:
                case ConnectionType.hollowKnob:
                case ConnectionType.knobFitInPegHole:
                case ConnectionType.hollowKnobFitInPegHole:
                case ConnectionType.tubeGap:
                case ConnectionType.edgeGap:
                    {
                        return dst.field.transform.TransformDirection(Vector3.up * 0.1f);
                    }
                case ConnectionType.tube:
                case ConnectionType.antiKnob:
                case ConnectionType.squareAntiKnob:
                case ConnectionType.bottomTube:
                case ConnectionType.bottomTubeWithRib:
                case ConnectionType.fixedTubeWithAntiKnob:
                case ConnectionType.edge:
                case ConnectionType.secondaryPin:      
                case ConnectionType.pegHole: 
                    {
                        return dst.field.transform.TransformDirection(Vector3.down * 0.1f);
                    }
            }
            
            return Vector3.zero;
        }
    }
}