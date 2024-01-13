using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoints : MonoBehaviour
{
    public List<Transform> mSpawnPoints = new List<Transform>();

    public Transform GetSpawnPoint()
    {
        if (mSpawnPoints.Count == 0) return this.transform;
        return mSpawnPoints[Random.Range(0, mSpawnPoints.Count)].transform;
    }
}
