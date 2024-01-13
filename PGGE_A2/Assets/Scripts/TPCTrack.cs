using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGGE
{
    public class TPCTrack : TPCBase
    {
        public TPCTrack(Transform cameraTransform, Transform playerTransform)
            : base(cameraTransform, playerTransform)
        {
        }

        public override void Update()
        {
            Vector3 targetPos = mPlayerTransform.position;
            targetPos.y += CameraConstants.CameraPositionOffset.y;
            mCameraTransform.LookAt(targetPos);
        }
    }
}
