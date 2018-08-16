//Attach this script to a visible GameObject.
//Click on the GameObject to expand it and output the Bound extents to the Console.

using UnityEngine;

public class OutputExtents : MonoBehaviour
{
    Collider m_ObjectCollider;

    void Start()
    {
        //Fetch the GameObject's collider (make sure they have a Collider component)
        m_ObjectCollider = gameObject.GetComponent<Collider>();
        //Output the GameObject's Collider Bound extents
        PrintBounds();
    }

    //Detect when the user clicks the GameObject
    void OnMouseDown()
    {
        PrintBounds();
    }

    private void PrintBounds()
    {
        Debug.Log(this.name + " global max : " + m_ObjectCollider.bounds.max);
        Debug.Log(this.name + " global min : " + m_ObjectCollider.bounds.min);
        Debug.Log(this.name + " local max : " + transform.InverseTransformDirection(m_ObjectCollider.bounds.max));
        Debug.Log(this.name + " local min : " + transform.InverseTransformDirection(m_ObjectCollider.bounds.min));
    }
}