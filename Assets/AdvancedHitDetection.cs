using System;
using UnityEngine;

namespace Exiin
{
    public class AdvancedHitDetection : MonoBehaviour
    {
        public BoxCollider _collider;

        public Transform currentSword;
        public Transform previousSword;

        private Vector3 currentCenterPos;
        private Vector3 previousCenterPos;

        private Quaternion currentRot;
        private Quaternion previousRot;

        public float sphereSize = 0.3f;

        public Vector3 size;

        private void OnDrawGizmos()
        {
            currentCenterPos = currentSword.TransformPoint(_collider.center);
            currentRot = currentSword.rotation;

            previousCenterPos = previousSword.TransformPoint(_collider.center);
            previousRot = previousSword.rotation;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentCenterPos, sphereSize);
            Gizmos.DrawSphere(previousCenterPos, sphereSize);

            Vector3 direction = (currentCenterPos - previousCenterPos).normalized;
            Quaternion rotationDirection = Quaternion.LookRotation(direction, transform.up);
            
            Vector3 center = (currentCenterPos + previousCenterPos) / 2f;
            Gizmos.DrawLine(center, center + direction);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(center, sphereSize);

            Vector3 localTopLeftPoint = (_collider.size / 2) + _collider.center;
            Vector3 localBottomRightPoint = (_collider.size / -2) + _collider.center;

            Gizmos.color = Color.green;
            
            //-------------------------------------------
            
            Matrix4x4 currentPosMatrix4X4 = Matrix4x4.Translate(currentSword.position);
            Matrix4x4 previousPosMatrix4X4 = Matrix4x4.Translate(previousSword.position);

            Matrix4x4 currentLocalRotMatrix4x4 = Matrix4x4.Rotate(currentRot);
            Matrix4x4 previousLocalRotMatrix4x4 = Matrix4x4.Rotate(previousRot);

            Matrix4x4 localTopPosMatrix4X4 = Matrix4x4.Translate(localTopLeftPoint);
            Matrix4x4 localBottomPosMatrix4X4 = Matrix4x4.Translate(localBottomRightPoint);

            //Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(Vector3.Scale(Quaternion.Lerp(currentRot, previousRot, 0.5f).eulerAngles, Vector3.right * -1)));
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(-rotationDirection.eulerAngles.x, 0,0)));
            //Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotationDirection);
            
            Vector3 topLeftCorrected = (rotationMatrix * currentLocalRotMatrix4x4 * localTopPosMatrix4X4 * currentPosMatrix4X4).MultiplyPoint(Vector3.zero);
            Vector3 bottomRightCorrected = (rotationMatrix * previousLocalRotMatrix4x4 * localBottomPosMatrix4X4 * previousPosMatrix4X4).MultiplyPoint(Vector3.zero);
            
            Gizmos.DrawSphere(topLeftCorrected, sphereSize);
            Gizmos.DrawSphere(bottomRightCorrected, sphereSize);
            
            //-------------------------------------------

            Matrix4x4 bbTranslate = Matrix4x4.Translate(center);
            Matrix4x4 bbRotate = Matrix4x4.Rotate(rotationDirection);

            Matrix4x4 trs = bbTranslate * bbRotate;
                
            Gizmos.matrix = trs;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}