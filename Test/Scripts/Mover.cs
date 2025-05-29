using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private List<Transform> _patrolPoints;
    [SerializeField] private float _speed = 2f;
    private int _currentPointIndex = 0;
    void FixedUpdate()
    {
        if (_patrolPoints.Count == 0) return;

        Transform targetPoint = _patrolPoints[_currentPointIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        transform.position += direction * _speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            _currentPointIndex = (_currentPointIndex + 1) % _patrolPoints.Count;
        }
        
    }
}
