using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPCamera : MonoBehaviour
{
  public Transform target;
  [Range(5f, 15f)]
  public float _distance = 10f;
  [Range(5f, 15f)]
  public float _height = 5f;
  [Range(0.1f, 10f)]
  public float _lerpTime = 0.2f;
  void Awake() { }

  void LateUpdate()
  {
    if (!target) return;
    var targetPosition = target.position;

    var myPosition = transform.position;

    myPosition.x = targetPosition.x;
    myPosition.y = Mathf.Lerp(myPosition.y, targetPosition.y + _height, Time.deltaTime * _lerpTime);
    myPosition.z = Mathf.Lerp(myPosition.z, targetPosition.z - _distance, Time.deltaTime * _lerpTime);

    transform.position = myPosition;
    transform.LookAt(target);
  }
}
