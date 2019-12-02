using System;
using UnityEngine;

namespace Exiin
{
    public class AdvancedHitDetection : MonoBehaviour
    {
        public BoxCollider _collider;
        public Transform previousPos;
        public BoxCollider _previousCollider;


        public bool enabledHitDetection;

        private Vector3 previousCenter;

        public float sphereSize = 0.3f;

        public Vector3 size;
        
        private void OnDrawGizmos()
        {
            if (enabledHitDetection)
            {
                previousCenter = previousPos.TransformPoint(_previousCollider.center);
                Vector3 currentCenter = transform.TransformPoint(_collider.center);
                                
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(previousCenter, sphereSize);
                Gizmos.DrawSphere(currentCenter, sphereSize);
                
                Vector3 direction = (currentCenter - previousCenter).normalized;
                Quaternion rotationDirection = Quaternion.LookRotation(direction, transform.up);
                Quaternion rotationBounding = Quaternion.LookRotation(Vector3.Reflect((direction.normalized + transform.forward).normalized, direction).normalized * -1, transform.up);
                
                Vector3 center = (currentCenter + previousCenter) / 2f;
                Gizmos.DrawLine(center, center + direction);// (direction.normalized + transform.forward).normalized);

                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(center, sphereSize);
                
                Vector3 localTopLeftPoint = (_collider.size / 2) + _collider.center;
                Vector3 worldTopLeftPoint = transform.TransformPoint(localTopLeftPoint);
                
                Vector3 localTopLeftPoint2 = (_collider.size / -2) + _previousCollider.center;
                Vector3 worldTopLeftPoint2 = previousPos.TransformPoint(localTopLeftPoint2);
                
                Gizmos.color = Color.green;
                
                Gizmos.DrawSphere(worldTopLeftPoint, sphereSize / 2f);
                Gizmos.DrawSphere(worldTopLeftPoint2, sphereSize / 2f);

                var translateMatrix = Matrix4x4.Translate(center);
                var rotationMatrix = Matrix4x4.Rotate(rotationDirection);

                var trs = translateMatrix * rotationMatrix;

                
                Gizmos.matrix = trs;

                Gizmos.DrawWireCube(Vector3.zero, size);
                
/*                Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                
                var testPoint = Vector3.Reflect(direction, Vector3.up);
                rotationBounding = Quaternion.LookRotation(testPoint, Vector3.up);
                
                rotationMatrix = Matrix4x4.Rotate(rotationBounding);

                trs = rotationMatrix;
                
                Gizmos.matrix = trs;
                
                Gizmos.DrawLine(Vector3.zero, direction);*/


                Matrix4x4 localPosMatrix4X4 = Matrix4x4.Translate(localTopLeftPoint);
                Matrix4x4 posMatrix = Matrix4x4.Translate(transform.position - center);
                Matrix4x4 centerPosMatrix = Matrix4x4.Translate(center);
                rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(Vector3.Scale(Quaternion.Lerp(transform.rotation, previousPos.rotation, 0.5f).eulerAngles, Vector3.right * -1)));
                Matrix4x4 resultMatrix = posMatrix * rotationMatrix;

                Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                
                Vector3 testy = (rotationMatrix * localPosMatrix4X4).MultiplyPoint(Vector3.zero);
                Gizmos.DrawSphere(testy, sphereSize);
            }
        }
    }
}