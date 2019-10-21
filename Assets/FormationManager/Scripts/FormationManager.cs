using System;
using Extensions;
using Pathfinding;
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
    
    private Quaternion previousRotation;
    private int previousTransposeDirection;

    private FormationPoint[] formationPoints;
    private AIBase[][] agents;

    public GameObject agent;
    
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
        agents = new AIBase[rowCount][];

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                FormationPoint newFormationPoint = new FormationPoint();
                formationPoints[i * columnCount + j] = newFormationPoint;

                if (enableDebug)
                {
                    newFormationPoint.debugTransform = Instantiate(debugObject).transform;
                }
            }
        }

        for (int i = 0; i < rowCount; i++)
        {
            agents[i] = new AIBase [columnCount];
            
            for (int j = 0; j < columnCount; j++)
            {
                AIBase newAgent = Instantiate(agent).GetComponent<AIBase>();
                agents[i][j] = newAgent;
            }
        }
        
        previousRotation = formationCenterTransform != null ? formationCenterTransform.rotation : transform.rotation;
        
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
    
    private void CheckNeedAgentMatrixRotation()
    {
        float angleDiff = Mathf.DeltaAngle(previousRotation.eulerAngles.y, formationCenterRotation.eulerAngles.y);
        if ((previousTransposeDirection == 0 || Math.Abs(previousTransposeDirection - Mathf.Sign(angleDiff)) > 0.1f) && angleDiff > 45)
        {
            RotateAgentMatrix(angleDiff);
        }
        else if (Math.Abs(previousTransposeDirection - Mathf.Sign(angleDiff)) < 0.1f && angleDiff > 90)
        {
            RotateAgentMatrix(angleDiff);
        }
    }

    private void RotateAgentMatrix(float angleDiff)
    {
        //RelocateAgents
        previousRotation = formationCenterRotation;
        previousTransposeDirection = (int) Mathf.Sign(angleDiff);
        
        if (previousTransposeDirection < 0)
        {
            RotateAgentMatrixClockWise();
        }
        else
        {
            RotateAgentMatrixCounterClockWise();
        }
    }

    private void RotateAgentMatrixClockWise()
    {
        float x  = Mathf.Floor(rowCount / 2f);
        int y  = rowCount - 1;
        
        for (int i = 0; i < x; i++)
        {
            for (int j = i; j < y - i; j++)
            {
                AIBase swapAgent = agents[i][j];
                agents[i][j] = agents[y - j][i];
                agents[y - j][i] = agents[y - i][y - j];
                agents[y - i][y - j] = agents[j][y - i];
                agents[j][y - i] = swapAgent;
            }
        }
    }
    
    private void RotateAgentMatrixCounterClockWise()
    {
        float x  = Mathf.Floor(rowCount / 2f);
        int y  = rowCount - 1;
        
        for (int i = 0; i < x; i++)
        {
            for (int j = i; j < y - i; j++)
            {
                AIBase swapAgent = agents[j][i];
                agents[j][i] = agents[i][y - j];
                agents[i][y - j] = agents[y - j][y - i];
                agents[y - j][y - i] = agents[y - i][j];
                agents[y - i][j] = swapAgent;
            }
        }
    }

    public void UpdateAgentsDestination()
    {
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                AIBase currentAgent = agents[i][j];
                currentAgent.destination = formationPoints[i * columnCount + j].position;
            }
        }
    }
    
    // Update is called once per frame
    private void Update()
    {
        UpdateFormation();
        CheckNeedAgentMatrixRotation();
        UpdateAgentsDestination();
    }
}
