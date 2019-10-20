using System;
using Extensions;
using UnityEngine;

[Serializable]
public class FormationPoint
{
    public Vector3 position;
    public Quaternion rotation;

    public Transform debugTransform;
}

public class FormationManager : MonoBehaviour
{
    public int rowCount;
    public int columnCount;
    private int totalCount;

    public float rowSpacing = 1f;
    public float columnSpacing = 1f;

    public Transform formationCenterTransform;
    private Vector3 formationCenterPoint;
    private Quaternion formationCenterRotation;

    private FormationPoint[] formationPoints;

    public bool enableDebug;
    public GameObject debugObject;

    // Start is called before the first frame update
    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        totalCount = rowCount * columnCount;
        formationPoints = new FormationPoint [totalCount];

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                FormationPoint newFormationPoint = formationPoints[i * columnCount + j] = new FormationPoint();

                if (enableDebug)
                {
                    newFormationPoint.debugTransform = Instantiate(debugObject).transform;
                }
            }
        }

        UpdateFormation();
    }

    public void UpdateFormation()
    {
        formationCenterPoint = formationCenterTransform != null ? formationCenterTransform.position : transform.position;
        formationCenterRotation = formationCenterTransform != null ? formationCenterTransform.rotation : transform.rotation;
        
        float rowCenter = rowCount * rowSpacing / 2f;
        float columnCenter = columnCount * columnSpacing / 2f;
        
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                Vector3 positionInFormation = Vector3.zero;
                positionInFormation.x = rowCenter * (i * 2f / rowCount - 1);
                positionInFormation.z = columnCenter * (j * 2f / columnCount - 1);

                Matrix4x4 formationPointTranslationMatrix = Matrix4x4.Translate(positionInFormation);
                Matrix4x4 formationRotationMatrix = Matrix4x4.Rotate(formationCenterRotation);
                Matrix4x4 formationCenterTranslationMatrix = Matrix4x4.Translate(formationCenterPoint);
                

                Matrix4x4 resultMatrix = formationCenterTranslationMatrix * formationRotationMatrix * formationPointTranslationMatrix;


                FormationPoint currentFormationPoint = formationPoints[i * columnCount + j];
                currentFormationPoint.position = resultMatrix.GetPosition();
                currentFormationPoint.rotation = resultMatrix.GetRotation();

                if (enableDebug)
                {
                    UpdateDebugTransform(currentFormationPoint);
                }
            }
        }
    }

    private static void UpdateDebugTransform(FormationPoint currentFormationPoint)
    {
        currentFormationPoint.debugTransform.position = currentFormationPoint.position;
        currentFormationPoint.debugTransform.rotation = currentFormationPoint.rotation;
    }
    
    // Update is called once per frame
    private void Update()
    {
        UpdateFormation();
    }
}
