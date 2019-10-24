using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Pathfinding
{

	public class testTraversable : ITraversalProvider
	{
		public HashSet<GraphNode> blockedNodes;

		public bool CanTraverse(Path path, GraphNode node)
		{
			return !blockedNodes.Contains(node);
		}

		public uint GetTraversalCost(Path path, GraphNode node)
		{
			throw new System.NotImplementedException();
		}
	}

	/// <summary>
	/// Moves the target in example scenes.
	/// This is a simple script which has the sole purpose
	/// of moving the target point of agents in the example
	/// scenes for the A* Pathfinding Project.
	///
	/// It is not meant to be pretty, but it does the job.
	/// </summary>
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_target_mover.php")]
	public class TargetMover : MonoBehaviour
	{
		/// <summary>Mask for the raycast placement</summary>
		public LayerMask mask;

		public Transform target;
		IAstarAI[] ais;

		/// <summary>Determines if the target position should be updated every frame or only on double-click</summary>
		public bool onlyOnDoubleClick;

		public bool use2D;

		Camera cam;

		testTraversable traversable = new testTraversable();

		public void Start()
		{
			//Cache the Main Camera
			cam = Camera.main;
			// Slightly inefficient way of finding all AIs, but this is just an example script, so it doesn't matter much.
			// FindObjectsOfType does not support interfaces unfortunately.
			ais = FindObjectsOfType<MonoBehaviour>().OfType<IAstarAI>().ToArray();
			useGUILayout = false;

			HashSet<GraphNode> blockedNodes = new HashSet<GraphNode>();

			foreach (NavGraph graph in AstarPath.active.graphs)
			{
				graph.GetNodes(node =>
				{
					if (node.Tag != 0)
						blockedNodes.Add(node);
				});
			}

			traversable.blockedNodes = blockedNodes;
		}

		public void OnGUI()
		{
			if (onlyOnDoubleClick && cam != null && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
			{
				UpdateTargetPosition();
			}
		}

		/// <summary>Update is called once per frame</summary>
		void Update()
		{
			if (!onlyOnDoubleClick && cam != null)
			{
				UpdateTargetPosition();
			}
		}

		public void UpdateTargetPosition()
		{
			Vector3 newPosition = Vector3.zero;
			bool positionFound = false;

			if (use2D)
			{
				newPosition = cam.ScreenToWorldPoint(Input.mousePosition);
				newPosition.z = 0;
				positionFound = true;
			}
			else
			{
				// Fire a ray through the scene at the mouse position and place the target where it hits
				RaycastHit hit;
				if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, mask))
				{
					newPosition = hit.point;
					positionFound = true;
				}
			}

			if (positionFound && newPosition != target.position)
			{
				if (onlyOnDoubleClick)
				{
					GraphNode destinationNode = AstarPath.active.GetNearest(newPosition, NNConstraint.Default).node;
					GraphNode playerNode = AstarPath.active.GetNearest(ais[0].position, NNConstraint.Default).node;

					bool isPathPossible = PathUtilities.IsPathPossible(destinationNode, playerNode, traversable);
					Debug.Log(isPathPossible);
					
					if (isPathPossible)
					{
						for (int i = 0; i < ais.Length; i++)
						{
							if (ais[i] != null) ais[i].SearchPath();
						}
					}
					else
					{
						return;
					}
				}
				
				target.position = newPosition;
			}
		}
	}
}
