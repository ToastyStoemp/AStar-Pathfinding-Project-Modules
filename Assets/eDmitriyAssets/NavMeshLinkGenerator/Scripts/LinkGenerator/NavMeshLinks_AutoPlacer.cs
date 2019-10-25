﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
 
 
#if UNITY_EDITOR
using UnityEditor;
#endif
 
 
namespace eDmitriyAssets.NavmeshLinksGenerator
{
 
    public class NavMeshLinks_AutoPlacer : MonoBehaviour
    {
        #region Variables
 
        public Transform linkPrefab;
        public Transform OnewayLinkPrefab;
        public float tileWidth = 5f;
 
        [Header("OffMeshLinks")]
        public float maxJumpHeight = 3f;
        public float maxJumpDist = 5f;
        public LayerMask raycastLayerMask = -1;
        public float sphereCastRadius = 1f;
 
        //how far over to move spherecast away from navmesh edge to prevent detecting the same edge
        public float cheatOffset = .25f;
 
        //how high up to bump raycasts to check for walls (to prevent forming links through walls)
        public float wallCheckYOffset = 0.5f;
 
        [Header("EdgeNormal")] public bool invertFacingNormal = false;
        public bool dontAllignYAxis = false;
 
 
        //private List< Vector3 > spawnedLinksPositionsList = new List< Vector3 >();
        private Mesh currMesh;
        private List<Edge> edges = new List<Edge>();
 
        public float agentRadius = 2;
 
        private Vector3 ReUsableV3;
        private Vector3 offSetPosY;
 
        #endregion
 
 
 
 
 
 
        #region GridGen
 
        public void Generate()
        {
            if (linkPrefab == null) return;
            //agentRadius = NavMesh.GetSettingsByIndex(0).agentRadius;
 
            edges.Clear();
            //spawnedLinksPositionsList.Clear();
 
            CalcEdges();
            PlaceTiles();
 
 
#if UNITY_EDITOR
            if( !Application.isPlaying ) EditorSceneManager.MarkSceneDirty( gameObject.scene );
#endif
 
        }
 
 
 
        public void ClearLinks()
        {
            List<NodeLink2> navMeshLinkList = GetComponentsInChildren<NodeLink2>().ToList();
            while (navMeshLinkList.Count > 0)
            {
                GameObject obj = navMeshLinkList[0].gameObject;
                if (obj != null) DestroyImmediate(obj);
                navMeshLinkList.RemoveAt(0);
            }
        }
 
        private void PlaceTiles()
        {
            if (edges.Count == 0) return;
 
            ClearLinks();
 
 
            foreach (Edge edge in edges)
            {
                int tilesCountWidth = (int)Mathf.Clamp(edge.length / tileWidth, 0, 10000);
                float heightShift = 0;
 
 
                for (int columnN = 0; columnN < tilesCountWidth; columnN++) //every edge length segment
                {
                    Vector3 placePos = Vector3.Lerp(
                                           edge.start,
                                           edge.end,
                                           (float)columnN / (float)tilesCountWidth //position on edge
                                           + 0.5f / (float)tilesCountWidth //shift for half tile width
                                       ) + edge.facingNormal * Vector3.up * heightShift;
 
                    //spawn up/down links
                    CheckPlacePos(placePos, edge.facingNormal);
                    //spawn horizontal links
                    CheckPlacePosHorizontal(placePos, edge.facingNormal);
                }
            }
        }
 
 
 
 
        bool CheckPlacePos(Vector3 pos, Quaternion normal)
        {
            bool result = false;
 
            Vector3 startPos = pos + normal * Vector3.forward * agentRadius * 2;
            Vector3 endPos = startPos - Vector3.up * maxJumpHeight * 1.1f;
 
            //Debug.DrawLine ( startPos, endPos, Color.white, 2 );
            
            RaycastHit raycastHit = new RaycastHit();
            if (Physics.Linecast(startPos, endPos, out raycastHit, raycastLayerMask.value,QueryTriggerInteraction.Ignore))
            {
                NNInfo nodeInfo = AstarPath.active.GetNearest(raycastHit.point, NNConstraint.Default);
                Vector3 closestPos = nodeInfo.position;

                if (nodeInfo.node != null)
                {
                    //Debug.DrawLine( pos, navMeshHit.position, Color.black, 15 );
 
                    if (Vector3.Distance(pos, closestPos) > 1.1f)
                    {
                        //added these 2 line to check to make sure there aren't flat horizontal links going through walls
                        Vector3 calcV3 = (pos - normal * Vector3.forward * 0.02f);
                        if ((calcV3.y - closestPos.y) > 1f)
                        {
 
                            //SPAWN NAVMESH LINKS
                            Transform spawnedTransf = Instantiate(linkPrefab.transform, calcV3, normal);
 
                            NodeLink2 nmLink = spawnedTransf.GetComponent<NodeLink2>();
                            GameObject endPoint = new GameObject("end");
                            endPoint.transform.position = closestPos;
                            endPoint.transform.SetParent(spawnedTransf, true);
                            nmLink.end = endPoint.transform;

                            spawnedTransf.SetParent(transform);
                        }
                    }
                }
            }
 
            return result;
        }
 
        bool CheckPlacePosHorizontal(Vector3 pos, Quaternion normal)
        {
            bool result = false;
 
            Vector3 startPos = pos + normal * Vector3.forward * agentRadius * 2;
            Vector3 endPos = startPos - normal * Vector3.back * maxJumpDist * 1.1f;
            // Cheat forward a little bit so the sphereCast doesn't touch this ledge.
            Vector3 cheatStartPos = LerpByDistance(startPos, endPos, cheatOffset);
            //Debug.DrawRay(endPos, Vector3.up, Color.blue, 2);
            //Debug.DrawLine ( cheatStartPos , endPos, Color.white, 2 );
            //Debug.DrawLine(startPos, endPos, Color.white, 2);
 
            
            RaycastHit raycastHit = new RaycastHit();
 
            //calculate direction for Spherecast
            ReUsableV3 = endPos - startPos;
            // raise up pos Y value slightly up to check for wall/obstacle
            offSetPosY = new Vector3(pos.x, (pos.y + wallCheckYOffset), pos.z);
            // ray cast to check for walls
            if (!Physics.Raycast(offSetPosY, ReUsableV3, (maxJumpDist/2), raycastLayerMask.value))
            {
                //Debug.DrawRay(pos, ReUsableV3, Color.yellow, 15);
                Vector3 ReverseRayCastSpot = (offSetPosY + (ReUsableV3));
                //now raycast back the other way to make sure we're not raycasting through the inside of a mesh the first time.
                if (!Physics.Raycast(ReverseRayCastSpot, -ReUsableV3, (maxJumpDist+1), raycastLayerMask.value))
                {
                    //Debug.DrawRay(ReverseRayCastSpot, -ReUsableV3, Color.red, 15);
                    //Debug.DrawRay(ReverseRayCastSpot, -ReUsableV3, Color.red, 15);
                   
                    //if no walls 1 unit out then check for other colliders using the Cheat offset so as to not detect the edge we are spherecasting from.
                    if (Physics.SphereCast(cheatStartPos, sphereCastRadius, ReUsableV3, out raycastHit, maxJumpDist, raycastLayerMask.value, QueryTriggerInteraction.Ignore))
                    //if (Physics.Linecast(startPos, endPos, out raycastHit, raycastLayerMask.value, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 cheatRaycastHit = LerpByDistance(raycastHit.point, endPos, .2f);

                        NNInfo nodeInfo = AstarPath.active.GetNearest(cheatRaycastHit, NNConstraint.Default);
                        Vector3 closestPos = nodeInfo.position;

                        if (nodeInfo.node != null)
                        {
                            //Debug.Log("Success");
                            //Debug.DrawLine( pos, navMeshHit.position, Color.black, 15 );
 
                            if (Vector3.Distance(pos, closestPos) > 1.1f)
                            {
                                //SPAWN NAVMESH LINKS
                                Transform spawnedTransf = Instantiate(
                                    OnewayLinkPrefab.transform,
                                    pos - normal * Vector3.forward * 0.02f,
                                    normal
                                ) as Transform;
                                
                                NodeLink2 nmLink = spawnedTransf.GetComponent<NodeLink2>();
                                GameObject endPoint = new GameObject("end");
                                endPoint.transform.position = closestPos;
                                endPoint.transform.SetParent(spawnedTransf, true);
                                nmLink.end = endPoint.transform;

                                spawnedTransf.SetParent(transform);
                            }
                        }
                    }
                }
            }
            return result;
        }
 
 
        #endregion
        //Just a helper function I added to calculate a point between normalized distance of two V3s
        public Vector3 LerpByDistance(Vector3 A, Vector3 B, float x)
        {
            Vector3 P = x * Vector3.Normalize(B - A) + A;
            return P;
        }
 
 
        #region EdgeGen
 
 
        float triggerAngle = 0.999f;
 
        private void CalcEdges()
        {
            List<Vector3> edgepoints = GraphUtilities.GetContours(AstarPath.active.data.recastGraph);
 
            for (int i = 0; i < edgepoints.Count - 1; i += 2)
            {
                //CALC FROM MESH OPEN EDGES vertices
                TrisToEdge(edgepoints[i], edgepoints[i + 1]);
            }

            foreach (Edge edge in edges)
            {
                //EDGE LENGTH
                edge.length = Vector3.Distance(
                    edge.start,
                    edge.end
                );
 
                //FACING NORMAL
                if (!edge.facingNormalCalculated)
                {
                    edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, Vector3.up));
 
 
                    if (edge.startUp.sqrMagnitude > 0)
                    {
                        var vect = Vector3.Lerp(edge.endUp, edge.startUp, 0.5f) - Vector3.Lerp(edge.end, edge.start, 0.5f);
                        edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, vect));

                        //FIX FOR NORMALs POINTING DIRECT TO UP/DOWN
                        if (Mathf.Abs(Vector3.Dot(Vector3.up, (edge.facingNormal * Vector3.forward).normalized)) > triggerAngle)
                        {
                            edge.startUp += new Vector3(0, 0.1f, 0);
                            vect = Vector3.Lerp(edge.endUp, edge.startUp, 0.5f) -
                                   Vector3.Lerp(edge.end, edge.start, 0.5f);
                            edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, vect));
                        }
                    }
 
                    if (dontAllignYAxis)
                    {
                        edge.facingNormal = Quaternion.LookRotation(
                            edge.facingNormal * Vector3.forward,
                            Quaternion.LookRotation(edge.end - edge.start) * Vector3.up
                        );
                    }
 
                    edge.facingNormalCalculated = true;
                }
                if (invertFacingNormal) edge.facingNormal = Quaternion.Euler(Vector3.up * 180) * edge.facingNormal;
 
            }
        }
 
 
 
        private void TrisToEdge(Vector3 val1, Vector3 val2)
        {
            if (val1 == val2)
                return;

            Edge newEdge = new Edge(val1, val2);
 
            //remove duplicate edges
            foreach (Edge edge in edges)
            {
                if (edge.start == val1 & edge.end == val2 || edge.start == val2 & edge.end == val1)
                {
                    edges.Remove(edge);
                    return;
                }
            }
 
            edges.Add(newEdge);
        }
 
        #endregion
    }
 
 
 
    [Serializable]
    public class Edge
    {
        public Vector3 start;
        public Vector3 end;
 
        public Vector3 startUp;
        public Vector3 endUp;
 
        public float length;
        public Quaternion facingNormal;
        public bool facingNormalCalculated = false;
 
 
        public Edge(Vector3 startPoint, Vector3 endPoint)
        {
            start = startPoint;
            end = endPoint;
        }
    }
 
 
 
 
 
#if UNITY_EDITOR
 
    [CustomEditor( typeof( NavMeshLinks_AutoPlacer ) )]
    [CanEditMultipleObjects]
    public class NavMeshLinks_AutoPlacer_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
 
            if( GUILayout.Button( "Generate" ) )
            {
                foreach ( var targ in targets )
                {
                    ( ( NavMeshLinks_AutoPlacer ) targ ).Generate();
                }
            }
 
            if ( GUILayout.Button ( "ClearLinks" ) )
            {
                foreach ( var targ in targets )
                {
                    ( (NavMeshLinks_AutoPlacer)targ ).ClearLinks();
                }
            }
        }
    }
 
#endif
}
 
 