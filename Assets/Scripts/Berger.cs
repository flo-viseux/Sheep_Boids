using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Berger : MonoBehaviour
{
    [SerializeField] private float speed;

    NavMeshAgent agent;
    Vector3 hitpoint;
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();   
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Do something with the hit information
                //Debug.Log("Hit at: " + hit.point);
                hitpoint = hit.point;
                agent.SetDestination(hitpoint);
            }
        }

        float Yaxis = Input.GetAxis("Vertical");
        float Xaxis = Input.GetAxis ("Horizontal");

        //transform.position += new Vector3(Xaxis, 0, Yaxis) * speed * Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && SheepHerd.Instance._debug)
        {
            Gizmos.DrawSphere(hitpoint, 1);
        }
    }
}
