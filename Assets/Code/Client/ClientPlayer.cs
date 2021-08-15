using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientPlayer : MonoBehaviour
{
  void Start()
  {

  }

  void Update()
  {
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit, maxDistance: 300f))
    {
      var target = hit.point;
      target.y = transform.position.y;
      transform.LookAt(target);
    }
  }
}
