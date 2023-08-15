using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassBendingCamera : MonoBehaviour
{
    public Transform playerTrans;

    void Update()
    {
        //跟随玩家
        transform.position = playerTrans.position + new Vector3(0,5,0);
    }
}