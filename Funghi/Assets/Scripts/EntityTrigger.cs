﻿using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class EntityTrigger: MonoBehaviour
{
    public string triggerIdentifier = "LeaveWorld";
    public PatrolPath linkedPath;

    SphereCollider sc;
    void Start()
    {
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
        }
        else
        {
            Debug.Log("No collider assigned");
        }
    }

	void OnTriggerEnter(Collider other)
    {
        Human e = GetComponent<Human>();
        if (e != null)
        {
            e.TriggerBehaviour(triggerIdentifier, linkedPath);
        }
    }

    Color gizmoSColor = new Color(1, 1, 0, 0.25f);
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoSColor;
        if (sc == null)
        {
            sc = GetComponent<SphereCollider>();
        }
        if (sc)
        {
            Gizmos.DrawSphere(transform.position, sc.radius);
        }
    }
}
