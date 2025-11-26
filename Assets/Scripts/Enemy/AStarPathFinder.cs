using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AStarPathFinder : MonoBehaviour
{
    public Transform target;
    public float MoveSpeed = 1.0f;
    public float turnSpeed = 10f;
    public float stopDisance = 0.5f;

    NavMeshPath NavMeshPath;
    int CurrentPoint;
    Vector3[] Points;
    // Start is called before the first frame update
    void Start()
    {
        NavMeshPath = new NavMeshPath();
    }

    void CaculatePathToTarget()
    {
        NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, NavMeshPath);
        if (NavMeshPath.status != NavMeshPathStatus.PathComplete && NavMeshPath.status != NavMeshPathStatus.PathPartial)
        {
            return;
        }
            Points = NavMeshPath.corners;
            CurrentPoint = 1;
 
    }
    void MoveAlongPath()
    {
        if(Points==null||Points.Length<2) return;
        if(CurrentPoint>Points.Length) return;

        Vector3 targetCorner = Points[CurrentPoint];
        Vector3 direction = (targetCorner-transform.position).normalized;
        //这里要把y设置为0，以防寻路的时候带有纵向的位移。
        direction.y = 0;

        if(direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }
    void Update()
    {

    }
}
