using System;
using UnityEngine;

public class PlaceCanvas : MonoBehaviour
{
    //------------ PUBLIC VARIABLES -------------
    public Canvas canvas;
    public GameObject target;
    public bool visualDebug = false;

    //----------- PRIVATE VARIABLES -------------
    RectTransform canvasTransform;

    private int numXRays;
    private int numYRays;
    private float margin = 0.05f;    // Extra space added to seperate canvas from object

    private Vector3[] transformPreferences = new Vector3[8]
    {
        Vector3.left,
        Vector3.right,
        new Vector3(-1f, 1f),    // Up left
        new Vector3(1f, 1f),     // Up right
        Vector3.up,
        Vector3.down,
        new Vector3(-1f, -1f),   // Down left
        new Vector3(1f, -1f)     // Down right
    };

    private Color[] positionColors = new Color[8]
    {
        new Color(0f,0f,0f),
        new Color(0f,0f,1f),
        new Color(0f,1f,0f),
        new Color(0f,1f,1f),
        new Color(1f,0f,0f),
        new Color(1f,0f,1f),
        new Color(1f,1f,0f),
        new Color(1f,1f,1f)
    };

    //----------------------- UNITY FUNCTIONS ---------------------------------
    // Use this for initialization
    void Start()
    {
        Place(canvas, target);
    }

    //------------------------- PUBLIC FUNCTIONS ------------------------------
    public void Place(Canvas canvas, GameObject target, int resolution = 25)
    {
        canvasTransform = canvas.GetComponent<RectTransform>();

        // Calculate number of rays
        numXRays = (int)Math.Round(canvasTransform.sizeDelta.x / resolution, 0);
        numYRays = (int)Math.Round(canvasTransform.sizeDelta.y / resolution, 0);

        // Canvas faces target
        canvasTransform.position = transform.position;
        canvasTransform.LookAt(target.transform);

        // Calculate distance to target
        RaycastHit targetRayHit;
        float targetDistance = 0;
        if (Physics.Raycast(transform.position, (target.transform.position - canvas.transform.position).normalized, out targetRayHit))
        {
            targetDistance = targetRayHit.distance;
           
            if (visualDebug)
            {
                Debug.Log("Distance to target = " + targetDistance);
                CreateTestPoint("Distance hit", targetRayHit.point, Color.red);
                CreateVisibleRay("Distance ray", canvas.transform.position, targetRayHit.point, Color.red);
            }
        }

        // Get corners and size
        Vector3[] corners = new Vector3[4];
        canvasTransform.GetLocalCorners(corners);
        Vector2 canvasSize = corners[2] - corners[0];
        
        // Calculate local distance canvas must move to clear target
        Vector3[] offsets = CalculateOffset();

        // Find the best position...
        float bestDist = 0;
        int bestIndex = 0;
        Vector3 bestOffset = new Vector3();
        
        for (int i = 0; i < transformPreferences.Length; i++)
        {
            // Create offset vector for canvas at this position
            Vector3 offset = new Vector3();
            for (int j = 0; j < 3; j++)
            {
                if (transformPreferences[i][j] == 1)
                    offset[j] = offsets[1][j] / canvasTransform.localScale[j];
                else if (transformPreferences[i][j] == -1)
                    offset[j] = offsets[0][j] / canvasTransform.localScale[j];
                else
                    offset[j] = 0;
            }

            // Calculate closest canvas can get to target in this position
            float closestDist = targetDistance;

            for (int y = 0; y < numYRays; y++)
            {
                for (int x = 0; x < numXRays; x++)
                {
                    // Calculate ray position
                    Vector3 increment = new Vector3(canvasSize.x / (numXRays - 1) * x, canvasSize.y / (numYRays - 1) * y);
                    Vector3 point = canvasTransform.TransformPoint(corners[0] + increment + offset);

                    // Find out if anything is closer than target
                    RaycastHit rayHit;
                    float distance = targetDistance;
                    if (Physics.Raycast(point, canvas.transform.forward, out rayHit))
                    {
                        distance = rayHit.distance;

                        if (distance < closestDist)
                            closestDist = distance;

                        if (visualDebug)
                        {
                            CreateTestPoint("(" + point.x + "," + point.y + ") distance = " + distance, rayHit.point, positionColors[i]);
                            CreateVisibleRay("Distance ray", point, rayHit.point, positionColors[i]);
                        }                        
                    }
                }
            }

            if (closestDist > bestDist)
            {
                bestDist = closestDist;
                bestIndex = i;
                bestOffset = offset;
            }
            if (bestDist >= targetDistance)
                break;
        }

        // Move canvas to new location
        canvasTransform.localPosition = canvasTransform.TransformPoint(canvasTransform.localPosition + bestOffset);
        canvasTransform.Translate(new Vector3(0, 0, bestDist));
    }

    //---------------------------- PRIVATE VARIABLES --------------------------
    private Vector3[] CalculateOffset()
    {
        Vector3 min = new Vector3();
        Vector3 max = new Vector3();

        // Get target bounds
        MeshCollider collider = target.GetComponent<MeshCollider>();
        Vector3 maxBoundsInLocal = Vector3.Scale(canvasTransform.InverseTransformPoint(collider.bounds.max), canvasTransform.localScale);
        Vector3 minBoundsInLocal = Vector3.Scale(canvasTransform.InverseTransformPoint(collider.bounds.min), canvasTransform.localScale);

        // Recalculate bounds locally
        for (int i = 0; i < 3; i++)
        {
            if (maxBoundsInLocal[i] > minBoundsInLocal[i])
            {
                max[i] = maxBoundsInLocal[i];
                min[i] = minBoundsInLocal[i];
            }
            else
            {
                max[i] = minBoundsInLocal[i];
                min[i] = maxBoundsInLocal[i];
            }
        }

        // Get canvas size
        Vector3 canvasOffset = new Vector3(
            canvasTransform.sizeDelta.x / 2,
            canvasTransform.sizeDelta.y / 2,
            0
        );
        canvasOffset = Vector3.Scale(canvasOffset, canvasTransform.localScale);

        // Final offset = canvas size + target bounds + margin
        min.x -= canvasOffset.x - margin;
        min.y -= canvasOffset.y - margin;
        max.x += canvasOffset.x + margin;
        max.y += canvasOffset.y + margin;

        return new Vector3[] { min, max };
    }

    private void CreateTestPoint(string name, Vector3 point, Color color)
    {
        GameObject tester = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tester.name = name;
        tester.GetComponent<MeshRenderer>().material.color = color;
        tester.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        tester.transform.position = point;
    }

    private void CreateVisibleRay(string name, Vector3 from, Vector3 to, Color color)
    {
        GameObject visRay = new GameObject();
        visRay.name = name;
        LineRenderer line = visRay.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.01f;
        line.SetPositions(new Vector3[]{ from, to });
    }
}
