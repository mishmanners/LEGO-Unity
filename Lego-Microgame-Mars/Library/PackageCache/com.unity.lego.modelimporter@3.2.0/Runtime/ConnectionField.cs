// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace LEGOModelImporter
{
    #if UNITY_EDITOR
    [ExecuteAlways]
    #endif
    public class ConnectionField : MonoBehaviour
    {
        public enum FieldType
        {
            custom2DField,
            axel
        }

        public enum FieldKind
        {
            connector,
            receptor
        }

        public Vector2Int gridSize;
        public FieldType fieldType;
        public FieldKind kind;
        [HideInInspector]
        public Connection[] connections;

        public Connectivity connectivity;

        [HideInInspector]
        public List<int> connected = new List<int>();
        [HideInInspector]
        public int connectableConnections;

        [Serializable]
        public class ConnectionTuple
        {
            public ConnectionField field;
            public int indexOfConnection;
        }

        [HideInInspector]
        public ConnectionTuple[] connectedTo;

        /// <summary>
        /// Check if a connection is connected to something
        /// </summary>
        /// <param name="connection">The connection to check</param>
        /// <returns>Boolean saying whether or not this connection has a connection</returns>
        public bool HasConnection(Connection connection)
        {
            return GetConnection(connection) != null;
        }

        /// <summary>
        /// Get the connection this connection is connected to
        /// </summary>
        /// <param name="connection">The connection to check</param>
        /// <returns>Connection that the given connection is connected to</returns>
        public Connection GetConnection(Connection connection)
        {
            return GetConnection(connection.index);
        }

        /// <summary>
        /// Get the connection this connection is connected to
        /// </summary>
        /// <param name="index">The index to the connection to check</param>
        /// <returns>Connection that the given connection is connected to</returns>
        public Connection GetConnection(int index)
        {
            if(index < 0 || index >= connections.Length)
            {
                return null;
            }
            var entry = connectedTo[index];
            if(entry == null || !entry.field)
            {
                return null;
            }
            return entry.field.connections[entry.indexOfConnection];
        }

        /// <summary>
        /// Function to tell the connection field that a connection has changed connection state.
        /// NOTE: Does not register undo on editor time.
        /// </summary>
        /// <param name="connection">The changed connection</param>
        public void OnConnectionChanged(Connection connection)
        {            
            var index = connection.index;
            if(HasConnection(connection))
            {
                if(!connected.Contains(index))
                {
                    connected.Add(index);
                }
            }
            else
            {
                if(connected.Contains(index))
                {
                    connected.Remove(index);
                }
            }
        }

        /// <summary>
        /// Check if any connections on a field match with any on another field
        /// </summary>
        /// <param name="f1">The first field</param>
        /// <param name="f2">The field to check against</param>
        /// <returns></returns>
        public static bool MatchTypes(ConnectionField f1, ConnectionField f2)
        {
            if(f1 == null || f2 == null)
            {
                return false;
            }

            foreach(var c1 in f1.connections)
            {
                foreach(var c2 in f2.connections)
                {
                    var match = Connection.MatchTypes(c1.connectionType, c2.connectionType);
                    if(match == Connection.ConnectionMatch.connect)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Convert a local 3D position to a 2D/XZ grid position
        /// </summary>
        /// <param name="localPos">The local position to convert</param>
        /// <returns></returns>
        internal static Vector2Int ToGridPos(Vector3 localPos)
        {
            return new Vector2Int(Mathf.RoundToInt(localPos.x / BrickBuildingUtility.LU_5) * -1, Mathf.RoundToInt(localPos.z / BrickBuildingUtility.LU_5));
        }

        /// <summary>
        /// Given a world position, check if this field has any connections
        /// </summary>
        /// <param name="worldPos">The world position to check</param>
        /// <returns></returns>
        internal Connection GetConnectionAt(Vector3 worldPos)
        {            
            var localPos = transform.InverseTransformPoint(worldPos);            
            return GetConnectionAt(ToGridPos(localPos));
        }

        /// <summary>
        /// Get a connection at the given coordinates, null if there are none
        /// </summary>
        /// <param name="localCoordinates">Local grid coordinates</param>
        /// <returns></returns>
        internal Connection GetConnectionAt(Vector2Int localCoordinates)
        {            
            if(localCoordinates.x > gridSize.x + 1 || localCoordinates.y > gridSize.y + 1 ||
                localCoordinates.x < 0 || localCoordinates.y < 0)
            {
                return null;
            }
                        
            var index = localCoordinates.x + (gridSize.x + 1) * localCoordinates.y;
            if(index >= connections.Length || index < 0)
            {            
                return null;
            }
            return connections[index];
        }

        /// <summary>
        /// Get the world position of a connection
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <returns>The world position</returns>
        public Vector3 GetPosition(Connection connection)
        {
            var row = connection.index % (gridSize.x + 1);
            var column = connection.index / (gridSize.x + 1);

            var x = row * -BrickBuildingUtility.LU_5;
            var z = (column * BrickBuildingUtility.LU_5);            
            var worldPos = transform.TransformPoint(new Vector3(x, 0.0f, z));
            return worldPos;
        }

        /// <summary>
        /// Get name of physics layer of the field kind
        /// </summary>
        /// <param name="kind">The kind to get the physics layer of</param>
        /// <returns></returns>
        public static string GetLayer(FieldKind kind)
        {
            return kind == FieldKind.connector ? Connection.connectivityConnectorLayerName : Connection.connectivityReceptorLayerName;
        }

        /// <summary>
        /// Query the possible connections for this field
        /// </summary>
        /// <param name="match">Out parameter that signifies the type of match (ignore, reject or connect)</param>
        /// <param name="bothkinds">Optional boolean to specify whether we want to check for both connection field kinds</param>
        /// <param name="onlyConnectTo">An optional filter field if you only want to check connections on specific bricks</param>
        /// <returns>A list of tuples for the possible connections</returns>
        public HashSet<(Connection, Connection)> QueryConnections(out Connection.ConnectionMatch match, HashSet<Brick> onlyConnectTo, bool bothkinds = false)
        {
            HashSet<ConnectionField> onlyConnectToFields = null;
            if(onlyConnectTo != null && onlyConnectTo.Count > 0)
            {
                onlyConnectToFields = new HashSet<ConnectionField>();
                foreach(var brick in onlyConnectTo)
                {
                    foreach(var part in brick.parts)
                    {
                        if(!part.connectivity)
                        {
                            continue;
                        }
                        
                        onlyConnectToFields.UnionWith(part.connectivity.connectionFields);
                    }
                }
            }
            return QueryConnections(out match, bothkinds, onlyConnectToFields);
        }

        /// <summary>
        /// Query the possible connections for this field
        /// </summary>
        /// <param name="match">Out parameter that signifies the type of match (ignore, reject or connect)</param>
        /// <param name="bothkinds">Optional boolean to specify whether we want to check for both connection field kinds</param>
        /// <param name="onlyConnectTo">An optional filter field if you only want to check connections on specific fields</param>
        /// <returns>A list of tuples for the possible connections</returns>
        public HashSet<(Connection, Connection)> QueryConnections(out Connection.ConnectionMatch match, bool bothkinds = false, ICollection<ConnectionField> onlyConnectTo = null)
        {            
            LayerMask mask;
            if(bothkinds)
            {
                mask = LayerMask.GetMask(GetLayer(FieldKind.receptor), GetLayer(FieldKind.connector));
            }
            else
            {
                var opposite = kind == FieldKind.connector ? FieldKind.receptor : FieldKind.connector;
                mask = LayerMask.GetMask(GetLayer(opposite));
            }            

            HashSet<(Connection, Connection)> validConnections = new HashSet<(Connection, Connection)>();           
            match = Connection.ConnectionMatch.ignore;

            // PhysicsScene
            var physicsScene = gameObject.scene.GetPhysicsScene();
            var size = new Vector3((gridSize.x + 1) * BrickBuildingUtility.LU_5, BrickBuildingUtility.LU_1 * 2, (gridSize.y + 1) * BrickBuildingUtility.LU_5);
            var center = new Vector3((size.x - BrickBuildingUtility.LU_5) * -0.5f, 0.0f, (size.z - BrickBuildingUtility.LU_5) * 0.5f);

            var hits = physicsScene.OverlapBox(transform.TransformPoint(center), size * 0.5f, BrickBuildingUtility.colliderBuffer, transform.rotation, mask, QueryTriggerInteraction.Collide);
            for(var i = 0; i < hits; i++)
            {
                var overlap = BrickBuildingUtility.colliderBuffer[i];
                var field = overlap.GetComponent<ConnectionField>();
                if(field == null || field == this)
                {
                    continue;
                }

                if(onlyConnectTo != null && !onlyConnectTo.Contains(field))
                {
                    continue;
                }

                if(Mathf.Abs(Vector3.Dot(field.transform.up, transform.up)) < 0.95f)
                {
                    continue;
                }

                if(!GetOverlap(field, this, out Vector2Int min, out Vector2Int max))
                {
                    continue;
                }

                for(var x = min.x; x < max.x + 1; x++)
                {
                    for(var z = min.y; z < max.y + 1; z++)
                    {
                        var localPos = new Vector3(x * BrickBuildingUtility.LU_5, 0.0f, z * BrickBuildingUtility.LU_5);
                        var fieldConnection = field.GetConnectionAt(ConnectionField.ToGridPos(localPos));
                        if(fieldConnection != null && !field.HasConnection(fieldConnection))
                        {                            
                            var worldPos = field.GetPosition(fieldConnection);
                            var connection = GetConnectionAt(worldPos);
                            if(connection != null && !HasConnection(connection))
                            {                                        
                                // Note: ConnectionValid checks both rejection and distance (position + rotation) so we need
                                //       to make sure we take care of both in case of false.
                                if(!Connection.ConnectionValid(fieldConnection, connection, out Connection.ConnectionMatch pairMatch))
                                {                                                   
                                    if(pairMatch != Connection.ConnectionMatch.reject)
                                    {                                        
                                        continue;
                                    }
                                    else
                                    {
                                        match = pairMatch;
                                        validConnections.Clear();
                                        return validConnections;
                                    }
                                }

                                if(pairMatch == Connection.ConnectionMatch.connect)
                                {
                                    validConnections.Add((connection, fieldConnection));
                                }
                            }
                        }
                    }
                }
            }

            match = Connection.ConnectionMatch.connect;
            return validConnections;
        }

        /// <summary>
        /// Compute the overlap between two fields in their current transformations
        /// </summary>
        /// <param name="f1">The first field</param>
        /// <param name="f2">The second field</param>
        /// <param name="min">Out parameter for the minimum position of the overlap</param>
        /// <param name="max">Out parameter for the maximum position of the overlap</param>
        /// <returns></returns>
        public static bool GetOverlap(ConnectionField f1, ConnectionField f2, out Vector2Int min, out Vector2Int max)
        {
            var f1Size = new Vector3(f1.gridSize.x, 0.0f, f1.gridSize.y) * BrickBuildingUtility.LU_5;
            var f2Size = new Vector3(f2.gridSize.x, 0.0f, f2.gridSize.y) * BrickBuildingUtility.LU_5;

            var f1_1 = new Vector3(0.0f, 0.0f, 0.0f);
            var f1_2 = new Vector3(-f1Size.x, 0.0f, f1Size.z);
            var f1_3 = new Vector3(-f1Size.x, 0.0f, 0.0f);
            var f1_4 = new Vector3(0.0f, 0.0f, f1Size.z);

            var f2_1 = new Vector3(0.0f, 0.0f, 0.0f);
            var f2_2 = new Vector3(-f2Size.x, 0.0f, f2Size.z);
            var f2_3 = new Vector3(-f2Size.x, 0.0f, 0.0f);
            var f2_4 = new Vector3(0.0f, 0.0f, f2Size.z);

            var s1 = f1.transform.InverseTransformPoint(f2.transform.TransformPoint(f2_1));
            var s2 = f1.transform.InverseTransformPoint(f2.transform.TransformPoint(f2_2));
            var s3 = f1.transform.InverseTransformPoint(f2.transform.TransformPoint(f2_3));
            var s4 = f1.transform.InverseTransformPoint(f2.transform.TransformPoint(f2_4));

            var sMinX = Mathf.Min(s1.x, Mathf.Min(s2.x, Mathf.Min(s3.x, s4.x)));
            var sMinZ = Mathf.Min(s1.z, Mathf.Min(s2.z, Mathf.Min(s3.z, s4.z)));

            var sMaxX = Mathf.Max(s1.x, Mathf.Max(s2.x, Mathf.Max(s3.x, s4.x)));
            var sMaxZ = Mathf.Max(s1.z, Mathf.Max(s2.z, Mathf.Max(s3.z, s4.z)));

            var fMinX = Mathf.Min(f1_1.x, Mathf.Min(f1_2.x, Mathf.Min(f1_3.x, f1_4.x)));
            var fMinZ = Mathf.Min(f1_1.z, Mathf.Min(f1_2.z, Mathf.Min(f1_3.z, f1_4.z)));

            var fMaxX = Mathf.Max(f1_1.x, Mathf.Max(f1_2.x, Mathf.Max(f1_3.x, f1_4.x)));
            var fMaxZ = Mathf.Max(f1_1.z, Mathf.Max(f1_2.z, Mathf.Max(f1_3.z, f1_4.z)));

            if (sMinX > fMaxX || fMinX > sMaxX || sMinZ > fMaxZ || fMinZ > sMaxZ)
            {
                min = Vector2Int.zero;
                max = Vector2Int.zero;
                return false;
            }

            var minX = Mathf.RoundToInt(Mathf.Max(sMinX, fMinX) / BrickBuildingUtility.LU_5);
            var maxX = Mathf.RoundToInt(Mathf.Min(sMaxX, fMaxX) / BrickBuildingUtility.LU_5);
            var minZ = Mathf.RoundToInt(Mathf.Max(sMinZ, fMinZ) / BrickBuildingUtility.LU_5);
            var maxZ = Mathf.RoundToInt(Mathf.Min(sMaxZ, fMaxZ) / BrickBuildingUtility.LU_5);

            min = new Vector2Int(minX, minZ);
            max = new Vector2Int(maxX, maxZ);
            return true;
        }

        /// <summary>
        /// Get a list of connectable connections that are connected
        /// </summary>
        /// <returns></returns>
        public List<int> GetConnectedConnections()
        {
            return connected;
        }

        /// <summary>
        /// Check if this field has any available connections
        /// </summary>
        /// <returns>Whether or not the connection field has any available connections</returns>
        public bool HasAvailableConnections()
        {
            return connected.Count < connectableConnections;
        }

        /// <summary>
        /// Disconnect all connections for this field.
        /// </summary>
        /// <returns>The fields that were disconnected</returns>
        public HashSet<ConnectionField> DisconnectAll(bool updateKnobsAndTubes = true)
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(Connection, Connection)> toBeDisconnected = new List<(Connection, Connection)>();

            var connected = GetConnectedConnections();
            foreach (var connection in connected)
            {
                var otherConnection = GetConnection(connection);
                toBeDisconnected.Add((connections[connection], otherConnection));
                result.Add(otherConnection.field);
            }
            Disconnect(toBeDisconnected, updateKnobsAndTubes);

            return result;
        }

        /// <summary>
        /// Disconnect all invalid connections for this field.
        /// </summary>
        /// <returns>The fields that were disconnected</returns>
        public HashSet<ConnectionField> DisconnectAllInvalid()
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(Connection, Connection)> toBeDisconnected = new List<(Connection, Connection)>();

            var connected = GetConnectedConnections();
            foreach (var connection in connected)
            {
                var otherConnection = GetConnection(connection);
                if (!Connection.ConnectionValid(connections[connection], otherConnection, out _))
                {
                    toBeDisconnected.Add((connections[connection], otherConnection));
                    result.Add(otherConnection.field);
                }
            }
            Disconnect(toBeDisconnected);

            return result;
        }

        /// <summary>
        /// Disconnect from all connections not connected to a list of bricks.
        /// Used to certain cases where you may want to keep connections with a 
        /// selection of bricks.
        /// </summary>
        /// <param name="bricksToKeep">List of bricks to keep connections to</param>
        /// <returns></returns>
        public HashSet<ConnectionField> DisconnectInverse(HashSet<Brick> bricksToKeep)
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(Connection, Connection)> toBeDisconnected = new List<(Connection, Connection)>();
            var connected = GetConnectedConnections();
            foreach(var connection in connected)
            {
                var connectedTo = GetConnection(connection);
                if(!bricksToKeep.Contains(connectedTo.field.connectivity.part.brick))
                {
                    toBeDisconnected.Add((connections[connection], connectedTo));
                    result.Add(connectedTo.field);
                }
            }
            Disconnect(toBeDisconnected);
            return result;
        }

        private static void Disconnect(List<(Connection, Connection)> toBeDisconnected, bool updateKnobsAndTubes = true)
        {
#if UNITY_EDITOR
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                HashSet<UnityEngine.Object> toRecord = new HashSet<UnityEngine.Object>();
                foreach (var (c1, c2) in toBeDisconnected)
                {
                    toRecord.Add(c1.field);
                    if (c1.knob)
                    {
                        toRecord.Add(c1.knob.gameObject);
                    }
                    foreach (var tube in c1.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                    toRecord.Add(c2.field);
                    if (c2.knob)
                    {
                        toRecord.Add(c2.knob.gameObject);
                    }
                    foreach (var tube in c2.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                }
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Recording all connections, knobs and tubes before disconnecting.");
            }
#endif
            foreach(var (c1, c2) in toBeDisconnected)
            {                
                c1.field.Disconnect(c1, updateKnobsAndTubes);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }
        }

        /// <summary>
        /// Create a rotation that aligns the orientation of a transform to another
        /// The rotation will not be applied in this function.
        /// </summary>
        /// <param name="source">The transform we want to align</param>
        /// <param name="destination">The transform we want to align to</param>
        /// <param name="resultRotation">Output parameter for the resulting rotation</param>
        /// <returns></returns>
        public static bool AlignRotation(Transform source, Transform destination, out Quaternion resultRotation)
        {            
            // Find rotation needed to align up vectors
            var alignedRotation = Quaternion.FromToRotation(source.up, destination.up);

            // Compute the angle between the rotations. To check if it is too large
            var angle = Quaternion.Angle(source.rotation, alignedRotation * source.rotation);

            // Ignore if we need to rotate more than 90 degrees (plus a small epsilon to account for randomized rotations) to align up vectors. 
            if (angle > 91.0f)
            {
                resultRotation = Quaternion.identity;
                return false;
            }

            // Cache the old rotation
            var oldRotation = source.rotation;

            // Set the rotation to the aligned rotation
            source.rotation = alignedRotation * source.rotation;

            // Find the rotation needed to align to the destination
            resultRotation = MathUtils.AlignRotation(new Vector3[2]{source.right, source.forward}, destination.localToWorldMatrix);

            // Combine up-alignment with forward/right alignment
            resultRotation = resultRotation * alignedRotation;
            source.rotation = oldRotation;
            return true;
        }

        /// <summary>
        /// Get the relative position and rotation of a possible connection
        /// </summary>
        /// <param name="src">The feature connecting</param>
        /// <param name="dst">The feature being connected to</param>
        /// <param name="pivot">The pivot we rotate around</param>
        /// <param name="offset">out parameter for the relative position</param>
        /// <param name="angle">out parameter for the angle needed for the rotation</param>
        /// <param name="axis">out parameter for the axis needed for the rotation</param>
        public static void GetConnectedTransformation(Connection src, Connection dst, Vector3 pivot, out Vector3 offset, out float angle, out Vector3 axis)
        {
            if(src.field == null || dst.field == null)
            {
                // Unsupported connectivity type
                offset = Vector3.zero;
                angle = 0.0f;
                axis = Vector3.zero;
                return;
            }

            var part = src.field.connectivity.part;
            var brick = part.brick;

            // Find the required rotation for the source to align with the destination
            AlignRotation(src.field.transform, dst.field.transform, out Quaternion rot);

            var oldRot = brick.transform.rotation;
            var oldPos = brick.transform.position;

            // We rotate around a pivot, so we need angle and axis
            rot.ToAngleAxis(out angle, out axis);
            brick.transform.RotateAround(pivot, axis, angle);

            var srcPosition = src.field.GetPosition(src);
            var dstPosition = dst.field.GetPosition(dst);

            // Offset of connections after pivot rotation
            offset = dstPosition - srcPosition;

            brick.transform.rotation = oldRot;
            brick.transform.position = oldPos;
        }

        /// <summary>
        /// Check if any knobs and tubes are visible
        /// </summary>
        /// <returns>True or false depending on any knobs or tubes are visible on this field</returns>
        public bool IsVisible()
        {
            var visible = false;
            foreach(var connection in connections)
            {
                visible = visible || connection.IsVisible();
            }
            return visible;
        }

        /// <summary>
        /// Check whether connecting to connection features is possible.
        /// </summary>
        /// <param name="src">The feature connecting</param>
        /// <param name="dst">The feature being connected to</param>
        /// <param name="pivot">The pivot we rotate around</param>
        /// <param name="ignoredBricks">Set of bricks to ignore when checking collision</param>
        /// <returns>Whether or not the connection is valid</returns>
        public static bool IsConnectionValid(Connection src, Connection dst, Vector3 pivot, HashSet<Brick> ignoredBricks = null)
        {
            // Make sure types match
            var match = Connection.MatchTypes(src.connectionType, dst.connectionType);
            if(match != Connection.ConnectionMatch.connect)
            {
                return false;
            }

            // Prevent connecting to itself
            if (src == dst)
            {
                return false;
            }

            if (src.field == null || dst.field == null)
            {
                // Could be due to an unsupported connection type
                return false;
            }            

            var part = src.field.connectivity.part;
            var brick = part.brick;

            var dstField = dst.field;
            var otherPart = dstField.connectivity.part;

            //FIXME: Can parts connect to themselves?
            if (otherPart == part)
            {
                return false;
            }

            if(brick.colliding || otherPart.brick.colliding)
            {
                return false;
            }

            // Get the relative position and rotation for this brick of a possible connection
            GetConnectedTransformation(src, dst, pivot, out Vector3 conOffset, out float conAngle, out Vector3 conAxis);

            // Cache position before we check collision
            var oldPosition = brick.transform.position;
            var oldRotation = brick.transform.rotation;

            brick.transform.RotateAround(pivot, conAxis, conAngle);
            brick.transform.position += conOffset;

            // Check if we collide with anything
            var parts = brick.parts;
            foreach (var p in parts)
            {
                if (Part.IsColliding(p, BrickBuildingUtility.colliderBuffer, out _, ignoredBricks))
                {

                    // We collided with something. Make sure to reset position and rotation to original.
                    brick.transform.position = oldPosition;
                    brick.transform.rotation = oldRotation;
                    return false;
                }
            }

            // Reset position and rotation to original
            brick.transform.position = oldPosition;
            brick.transform.rotation = oldRotation;
            return true;
        }

        /// <summary>
        /// Connect two fields through a src and dst connection.
        /// Connects to all fields possible through this connection.
        /// </summary>
        /// <param name="src">The feature connecting</param>
        /// <param name="dst">The feature being connected to</param>
        /// <param name="pivot">The pivot we rotate around</param>
        /// <returns>The fields that were connected to</returns>
        public static HashSet<ConnectionField> Connect(Connection src, Connection dst, Vector3 pivot, HashSet<Brick> onlyConnectTo = null, HashSet<Brick> ignoreForCollision = null)
        {
            // Return the fields that were connected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            //FIXME: Is this even possible if we have non-null connections?
            if (src.field == null || dst.field == null)
            {
                // Unsupported field types
                return result;
            }

            if (!IsConnectionValid(src, dst, pivot, ignoreForCollision))
            {
                // Connection is invalid: Mismatched connection types or collision
                return result;
            }

            var dstField = dst.field;
            var srcField = src.field;

            // We know the connection is valid, so first we remove all old connections.
            // We will detect any connections resulting from this connection later anyway

            foreach (var field in srcField.connectivity.connectionFields)
            {
                field.DisconnectAll();
            }

            List<(Connection, Connection)> toBeConnected = new List<(Connection, Connection)>();

            // The initial connection
            toBeConnected.Add((src, dst));
            result.Add(dstField);
            Physics.SyncTransforms();

            // Now we look in the fields of the src part for other possible connections
            foreach (var field in srcField.connectivity.connectionFields)
            {
                // Make a new ray query for all nearby connections
                var connections = field.QueryConnections(out _, onlyConnectTo);
                foreach (var connection in connections)
                {
                    if(connection.Item1 == src || connection.Item2 == dst)
                    {
                        continue;
                    }

                    // If there already is a connection, ignore.
                    if (connection.Item1.field.HasConnection(connection.Item1) || connection.Item2.field.HasConnection(connection.Item2))
                    {
                        continue;
                    }

                    // Check for validity of connection
                    if (Connection.ConnectionValid(connection.Item1, connection.Item2, out Connection.ConnectionMatch match))
                    {
                        if(match == Connection.ConnectionMatch.connect)
                        {
                            toBeConnected.Add(connection);
                            result.Add(connection.Item2.field);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                HashSet<UnityEngine.Object> toRecord = new HashSet<UnityEngine.Object>();
                foreach (var (c1, c2) in toBeConnected)
                {
                    toRecord.Add(c1.field);
                    if (c1.knob)
                    {
                        toRecord.Add(c1.knob.gameObject);
                    }
                    foreach (var tube in c1.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                    toRecord.Add(c2.field);
                    if (c2.knob)
                    {
                        toRecord.Add(c2.knob.gameObject);
                    }
                    foreach (var tube in c2.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                }
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Recording all connections, knobs and tubes before connecting.");
            }
#endif
            foreach (var (c1, c2) in toBeConnected)
            {
                c1.field.Connect(c1, c2);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }

            return result;
        }

        internal void Disconnect(int index, bool updateKnobsAndTubes = true)
        {
            if(index >= connectedTo.Length)
            {
                return;
            }

            var entry = connectedTo[index];
            var otherField = entry.field;            
            var indexOfConnection = entry.indexOfConnection;

            if(!otherField)
            {
                return;
            }            
            
            connectedTo[index].field = null;
            connectedTo[index].indexOfConnection = -1;
            OnConnectionChanged(connections[index]);            
            otherField.Disconnect(indexOfConnection, updateKnobsAndTubes);

            if(updateKnobsAndTubes)
            {
                connections[index].UpdateKnobsAndTubes();
            }
        }

        internal void Disconnect(Connection connection, bool updateKnobsAndTubes = true)
        {
            if(connection.index >= connections.Length)
            {
                return;
            }
            Disconnect(connection.index, updateKnobsAndTubes);            
        }

        internal void Connect(int src, Connection dst, bool updateKnobsAndTubes = true)
        {
            // Ignore if this index is out of bounds (could be that it doesn't belong to this field)
            if(src >= connections.Length)
            {
                return;
            }

            var srcConnection = connections[src];            
            // Ignore if same field
            if(dst != null && srcConnection.field == dst.field)
            {
                return;
            }

            var entry = connectedTo[src];            

            if(dst != null && entry != null && dst.field == entry.field)
            {
                return;
            }

            // Disconnect old connection.
            if(entry != null && entry.field != null)
            {
                entry.field.Disconnect(entry.indexOfConnection, updateKnobsAndTubes);
            }

            if(dst == null)
            {
                connectedTo[src] = null;
                OnConnectionChanged(srcConnection);
                return;
            }
                     
            var dstIndex = dst.index;

            // If the same connection has already been made, ignore as well.
            if(entry != null && entry.field == dst.field && entry.indexOfConnection == dstIndex)
            {
                return;
            }

            connectedTo[src] = new ConnectionTuple{field = dst.field, indexOfConnection = dstIndex};            
            dst.field.Connect(dstIndex, srcConnection);

            if(updateKnobsAndTubes)
            {
                srcConnection.UpdateKnobsAndTubes();
            }

            OnConnectionChanged(srcConnection);            
        }

        /// <summary>
        /// Connect two connections
        /// </summary>
        /// <param name="src">The source connection</param>
        /// <param name="dst">The destination connection</param>
        /// <param name="updateKnobsAndTubes">Whether or not to update knob and tube visibility</param>
        public void Connect(Connection src, Connection dst, bool updateKnobsAndTubes = true)
        {
            Connect(src.index, dst, updateKnobsAndTubes);
        }

#if UNITY_EDITOR
        public static event System.Action<ICollection<ConnectionField>> dirtied;
#endif

        private void OnDestroy()
        {
            #if UNITY_EDITOR
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var toRecord = new HashSet<ConnectionField>();
                var toDisconnect = new HashSet<int>();
                foreach(var connection in connected)
                {
                    var otherConnection = connectedTo[connection];
                    if(otherConnection.field)
                    {
                        toRecord.Add(otherConnection.field);
                    }
                    toDisconnect.Add(connection);
                }
                
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Destroying connection field");
                Undo.RegisterCompleteObjectUndo(this, "Destroying connection field");

                foreach(var connection in toDisconnect)
                {
                    var field = connectedTo[connection].field;
                    Disconnect(connection, false);
                    Connection.RegisterPrefabChanges(field);
                }

                Connection.RegisterPrefabChanges(this);

                dirtied?.Invoke(toRecord);
            }
            #endif
        }
    }
}