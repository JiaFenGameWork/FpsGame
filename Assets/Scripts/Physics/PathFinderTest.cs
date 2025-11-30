using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinderTest : MonoBehaviour
{
    public Baker baker;
    public Transform targetPoint;

    private JPathFinder finder;
    private List<Vector3> currentPath;
    // Start is called before the first frame update
    void Start()
    {
        baker.Bake();

        if (baker.octree != null)
        {
            finder = new JPathFinder(baker.octree);
        }


            currentPath = finder.FindPath(transform.position, targetPoint.position);

            if (currentPath != null)
            {
                Debug.Log(currentPath.Count);
            }
            else
            {
                Debug.Log("wu");
            }

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnDrawGizmos()
    {
        if (currentPath == null) return;

        Gizmos.color = Color.green;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            Gizmos.DrawSphere(currentPath[i], 0.2f);
        }

    }
}
