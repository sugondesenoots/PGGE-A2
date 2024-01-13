using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGGE
{
    public class TPCTopDown : TPCBase
    {
        public TPCTopDown(Transform cameraTransform, Transform playerTransform)
            : base(cameraTransform, playerTransform)
        {
        }

        public override void Update()
        {
            // For topdown camera we do not use the x and z offsets.
            Vector3 targetPos = mPlayerTransform.position;
            targetPos.y += CameraConstants.CameraPositionOffset.y;
            Vector3 position = Vector3.Lerp(mCameraTransform.position, targetPos, Time.deltaTime * CameraConstants.Damping);
            mCameraTransform.position = position;
            mCameraTransform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }
    }
}
