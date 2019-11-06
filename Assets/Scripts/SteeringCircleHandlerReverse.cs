using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using Pathfinding;

namespace None {

    public class SteeringPathPoint
    {
        public Vector2 position;
        public bool isReverse;
    }

	public class SteeringCircleHandlerReverse : MonoBehaviour {
		/// <summary>Mask for the raycast placement</summary>
		public LayerMask mask;

		public Transform targetTransform;
        public Transform startTransform;
        private bool hasSetDirection;

        public float turnRadius = 3;
        public float angleStep = 0.5f;

        public List<SteeringPathPoint> finalPath = new List<SteeringPathPoint>();

        Camera cam;

        public bool preferRight = true;

        private bool hasReversed;

        public void Start () {
			//Cache the Main Camera
			cam = Camera.main;
		}

        private void Update()
        {
            //CalculateFormationPath();
        }

        public void OnGUI () {
			if (cam != null) {
                if (Event.current.type == EventType.MouseDown)
                { 
                    UpdateTargetPosition();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    UpdateTargetDirection();
                    hasSetDirection = true;
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    UpdateTargetDirection();
                    hasSetDirection = false;
                }
			}
		}

        public void UpdateTargetPosition()
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, mask))
            {
                Vector3 newPosition = hit.point;

                if (newPosition != targetTransform.position)
                {
                    startTransform.position = targetTransform.position;
                    targetTransform.position = newPosition;
                }
            }
        }

        public void UpdateTargetDirection()
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, mask))
            {
                Vector3 newDirection = hit.point;

                Vector3 lookDir = newDirection - targetTransform.position;
                lookDir.y = 0;

                if (!hasSetDirection)
                    startTransform.forward = targetTransform.forward;

                targetTransform.rotation = Quaternion.LookRotation(lookDir);

                CalculateFormationPath();
            }
        }

        public void CalculateFormationPath()
        {
            hasReversed = false;
            
            Vector2 startPos = startTransform.position.ToVector2();
            Vector2 startDir = startTransform.forward.ToVector2().normalized;

            Vector2 endPos = targetTransform.position.ToVector2();
            Vector2 endDir = targetTransform.forward.ToVector2().normalized;

            finalPath.Clear();

            CalculateUsing2SteeringCircles(ref finalPath, startPos, startDir, endPos, -endDir);
        }

        public void CalculateUsing2SteeringCircles(ref List<SteeringPathPoint> path, Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir)
        {
            startDir = startDir.normalized;
            endDir = endDir.normalized;
            
            
            Vector2 startCircleCenterPos;
            Vector2 endCircleCenterPos;

            Vector2 startCircleExitPos;
            Vector2 endCircleEnterPos;

            float startCircleExitAngle;
            float endCircleEnterAngle;

            Vector2 startPerpendicular, endPerpendicular;
            Vector2 directionVec;

            Vector2 centerDir;

            // 1) Calculate the starting steering circle
            directionVec = (endPos - startPos).normalized;
            startPerpendicular = startDir.Perpendicular(directionVec);
            startCircleCenterPos = startPos + startPerpendicular * turnRadius;

            // 2) Calculate the ending steering circle
            endPerpendicular = endDir.Perpendicular(directionVec * -1f);
            endCircleCenterPos = endPos + endPerpendicular * turnRadius;

            //swapPerpendicular = !swapPerpendicular;

            centerDir = endCircleCenterPos - startCircleCenterPos;

            if (centerDir.magnitude < 2 * turnRadius && !hasReversed)
            {
                //Issue With Direct path!
                //Try Reversing?
                Calculate3PointTurn(ref path, startPos, startDir, endPos, endDir);
                return;
            }

            int sideStart;
            int sideEnd;

            if (startPerpendicular == startDir.RightPerp())
                sideStart = 0;
            else
                sideStart = 1;

            if (endPerpendicular == endDir.RightPerp())
                sideEnd = 0;
            else
                sideEnd = 1;

            // 3) Calculate the starting circle exit point    
            if (sideStart != sideEnd)
            {
                float halfCenterDistance = centerDir.magnitude / 2;
                float angle1 = turnRadius > halfCenterDistance ? 1 : Mathf.Acos(turnRadius / halfCenterDistance);
                float angle2 = centerDir.ConvertToAngle();

                if (sideStart == 1 && sideEnd == 0)
                    startCircleExitAngle = angle2 + angle1;
                else
                    startCircleExitAngle = angle2 - angle1;

                startCircleExitPos.x = startCircleCenterPos.x + turnRadius * Mathf.Cos(startCircleExitAngle);
                startCircleExitPos.y = startCircleCenterPos.y + turnRadius * Mathf.Sin(startCircleExitAngle);
            }
            else
            {
                if (sideStart == 1)
                    startCircleExitPos = centerDir.LeftPerp().normalized * turnRadius;
                else
                    startCircleExitPos = centerDir.RightPerp().normalized * turnRadius;

                startCircleExitAngle = startCircleExitPos.ConvertToAngle();
                startCircleExitPos = startCircleCenterPos + startCircleExitPos;
            }

            // 4) Calculate the ending circle entry point
            if (sideStart != sideEnd)
            {
                float halfCenterDistance = centerDir.magnitude / 2;
                float angle1 = turnRadius > halfCenterDistance ? 1 : Mathf.Acos(turnRadius / halfCenterDistance);
                float angle2 = (startCircleCenterPos - endCircleCenterPos).ConvertToAngle();

                if (sideStart == 1 && sideEnd == 0)
                    endCircleEnterAngle = angle2 + angle1;
                else
                    endCircleEnterAngle = angle2 - angle1;

                endCircleEnterPos.x = endCircleCenterPos.x + turnRadius * Mathf.Cos(endCircleEnterAngle);
                endCircleEnterPos.y = endCircleCenterPos.y + turnRadius * Mathf.Sin(endCircleEnterAngle);
            }
            else
            {
                if (sideEnd == 1)
                    endCircleEnterPos = centerDir.LeftPerp().normalized * turnRadius;
                else
                    endCircleEnterPos = centerDir.RightPerp().normalized * turnRadius;

                endCircleEnterAngle = endCircleEnterPos.ConvertToAngle();
                endCircleEnterPos = endCircleCenterPos + endCircleEnterPos;
            }

            // 5) Calculate startAngle and endAngle for path

            //Start Circle
            Vector2 startVec = startPos - startCircleCenterPos;

            float startAngle = startVec.ConvertToAngle();
            float endAngle = startCircleExitAngle; //endVec.ConvertToAngle();

            // Generate points on the starting circle
            if (sideStart == 0) // clockwise
                GeneratePath_Clockwise_OLD(startAngle, endAngle, startCircleCenterPos, ref path);
            else // Counter-clockwise
                GeneratePath_CounterClockwise_OLD(startAngle, endAngle, startCircleCenterPos, ref path);

            //End Circle
            Vector2 endVec = endPos - endCircleCenterPos;

            startAngle = endCircleEnterAngle; //startVec.ConvertToAngle();
            endAngle = endVec.ConvertToAngle();

            // Generate points on the starting circle
            if (sideEnd == 0) // clockwise
                GeneratePath_Clockwise_OLD(startAngle, endAngle, endCircleCenterPos, ref path);
            else // Counter-clockwise
                GeneratePath_CounterClockwise_OLD(startAngle, endAngle, endCircleCenterPos, ref path);
        }

        public void Calculate3PointTurn(ref List<SteeringPathPoint> path, Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir)
        {
            hasReversed = true;
            
            //Turn 180
            CalculateSimpleTurn(ref path, startPos, startDir, out Vector2 exitPos, out Vector2 exitDir, false, preferRight);
            CalculateSimpleTurn(ref path, exitPos, exitDir, out Vector2 finalExitPos, out Vector2 finalExitDir, true, preferRight);

            //Continue Path
            CalculateUsing2SteeringCircles(ref path, finalExitPos, finalExitDir, endPos, endDir);
        }

        public void CalculateSimpleTurn(ref List<SteeringPathPoint> path, Vector2 pos, Vector2 dir, out Vector2 exitPos, out Vector2 exitDir, bool isReverse = false, bool turnRight = true)
        {
            if (isReverse)
            {
                dir *= -1;
            }

            //Start Circle
            Vector2 dirPerpendicular = turnRight ? dir.RightPerp() : dir.LeftPerp();
            Vector2 circleCenterPos = pos + dirPerpendicular * turnRadius;

            exitPos = circleCenterPos + dir * turnRadius;

            Vector2 startVec = pos - circleCenterPos;
            float startAngle = startVec.ConvertToAngle(); //startVec.ConvertToAngle();
            float endAngle = (exitPos - circleCenterPos).ConvertToAngle();

            // Generate points on the starting circle
            if (turnRight) // clockwise
                GeneratePath_Clockwise(startAngle, endAngle, circleCenterPos, isReverse, ref path);
            else // Counter-clockwise
                GeneratePath_CounterClockwise(startAngle, endAngle, circleCenterPos, isReverse, ref path);

            exitDir = dirPerpendicular;
        }

        public void GeneratePath_Clockwise (float startAngle, float endAngle, Vector3 center, bool isReverse, ref List<SteeringPathPoint> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle < endAngle)
                endAngle -= 2 * Mathf.PI;
            
            float curAngle = startAngle;
            while (curAngle > endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = isReverse
                });

                curAngle -= angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = isReverse
                });
            }
        }

        public void GeneratePath_CounterClockwise (float startAngle, float endAngle, Vector3 center, bool isReverse, ref List<SteeringPathPoint> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle > endAngle)
                endAngle += 2 * Mathf.PI;

            float curAngle = startAngle;
            while (curAngle < endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = isReverse
                });

                curAngle += angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = isReverse
                });
            }
        }

        public void GeneratePath_Clockwise_OLD (float startAngle, float endAngle, Vector3 center, ref List<SteeringPathPoint> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle > endAngle)
                endAngle += 2 * Mathf.PI;

            float curAngle = startAngle;
            while (curAngle < endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = false
                });

                curAngle += angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = false
                });
            }
        }

        public void GeneratePath_CounterClockwise_OLD (float startAngle, float endAngle, Vector3 center, ref List<SteeringPathPoint> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle < endAngle)
                startAngle += 2 * Mathf.PI;

            float curAngle = startAngle;
            while (curAngle > endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = false
                });

                curAngle -= angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(new SteeringPathPoint()
                {
                    position = p,
                    isReverse = false
                });
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startTransform.position, startTransform.position + startTransform.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.forward);
            Gizmos.color = Color.white;
                       
            if (finalPath != null)
            {
                Vector3 previousPoint = Vector3.zero;

                for (int i = 0; i < finalPath.Count; i++)
                {
                    Vector3 currentPoint = finalPath[i].position.ToVector3();

                    if (finalPath[i].isReverse)
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }

                    Gizmos.DrawSphere(currentPoint, 0.25f);

                    if (i != 0)
                    {

                        Gizmos.DrawLine(previousPoint, currentPoint);
                    }

                    previousPoint = currentPoint;
                }
            }
        }
    }
}
