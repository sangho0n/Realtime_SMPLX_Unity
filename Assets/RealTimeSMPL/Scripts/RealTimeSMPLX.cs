using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Position index of joint points
/// </summary>
public enum PositionIndex : int
{
  nose = 0,
  left_eye_inner, 
  left_eye, 
  left_eye_outer, 
  right_eye_inner, 
  right_eye, 
  right_eye_outer, 
  left_ear, 
  right_ear, 
  mouth_left,

  mouth_right, 
  left_shoulder, 
  right_shoulder, 
  left_elbow, 
  right_elbow, 
  left_wrist, 
  right_wrist, 
  left_pinky, 
  right_pinky, 
  left_index,

  right_index, 
  left_thumb, 
  right_thumb, 
  left_hip, 
  right_hip, 
  left_knee, 
  right_knee, 
  left_ankle, 
  right_ankle, 
  left_heel,

  right_heel, 
  left_foot_index, 
  right_foot_index,
  // media pipe's 33 joints

  head, // 33; interp two ears' position
  hip, // 34; interp two hips' position
  spine, // 35; interp hips and pseudoNeck (3 : 1)
  // Unity humanoid's mandatory joint

  Count // 36
}

public static partial class EnumExtend
{
    public static int Int(this PositionIndex i)
    {
        return (int)i;
    }
}

public class RealTimeSMPLX : SMPLX
{
    public class JointPoint
    {
        public Vector3 Pos3D = new Vector3();
        public Vector3 Now3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];

        // Bones
        public Transform Transform = null;
        public Quaternion DefaultPoseRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public JointPoint Child = null;
        public JointPoint Parent = null;

        // For Kalman filter
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }

    // Joint position and bone
    private JointPoint[] jointPoints;
    public JointPoint[] JointPoints { get { return jointPoints; } }

    private Vector3 rootPosition; // Initial center position

    private Quaternion InitGazeRotation;
    private Quaternion gazeInverse;

    public GameObject Nose;
    private Animator anim;

    private Camera _mainCamera;

    public new void Awake() {
        base.Awake();
        _mainCamera = Camera.main;
    }

    private new void Update()
    {
        base.Update();
        if (jointPoints != null && jointPoints[0].Pos3D != null)
        {
            PoseUpdate();
        }
    }

    /// <summary>
    /// Initialize joint points
    /// </summary>
    /// <returns></returns>
    public JointPoint[] Init()
    {
        jointPoints = new JointPoint[PositionIndex.Count.Int()];
        for (var i = 0; i < PositionIndex.Count.Int(); i++) jointPoints[i] = new JointPoint();

        anim = gameObject.GetComponent<Animator>();

        // Right Arm
        jointPoints[PositionIndex.right_shoulder.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        jointPoints[PositionIndex.right_elbow.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        jointPoints[PositionIndex.right_wrist.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightHand);
        jointPoints[PositionIndex.right_thumb.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        jointPoints[PositionIndex.right_index.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        jointPoints[PositionIndex.right_pinky.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightLittleProximal);        

        // Left Arm
        jointPoints[PositionIndex.left_shoulder.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        jointPoints[PositionIndex.left_elbow.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        jointPoints[PositionIndex.left_wrist.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        jointPoints[PositionIndex.left_thumb.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        jointPoints[PositionIndex.left_index.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        jointPoints[PositionIndex.left_pinky.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLittleProximal);

        // Face
        jointPoints[PositionIndex.left_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftEye); 
        jointPoints[PositionIndex.right_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightEye);
        jointPoints[PositionIndex.nose.Int()].Transform = Nose.transform;

        // Right Leg
        jointPoints[PositionIndex.right_hip.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        jointPoints[PositionIndex.right_knee.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        jointPoints[PositionIndex.right_ankle.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        jointPoints[PositionIndex.right_foot_index.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightToes);

        // Left Leg
        jointPoints[PositionIndex.left_hip.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        jointPoints[PositionIndex.left_knee.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        jointPoints[PositionIndex.left_ankle.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        jointPoints[PositionIndex.left_foot_index.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftToes);

        // etc
        var pseudoNeckPosition = (jointPoints[PositionIndex.right_shoulder.Int()].Transform.position - jointPoints[PositionIndex.left_shoulder.Int()].Transform.position) / 2.0f;
        jointPoints[PositionIndex.head.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.hip.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Hips);
        jointPoints[PositionIndex.spine.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Spine);

        // Child Settings
        // Right Arm
        jointPoints[PositionIndex.right_shoulder.Int()].Child = jointPoints[PositionIndex.right_elbow.Int()];
        jointPoints[PositionIndex.right_elbow.Int()].Child = jointPoints[PositionIndex.right_wrist.Int()];
        jointPoints[PositionIndex.right_wrist.Int()].Parent = jointPoints[PositionIndex.right_elbow.Int()];
        jointPoints[PositionIndex.right_elbow.Int()].Parent = jointPoints[PositionIndex.right_shoulder.Int()];

        // Left Arm
        jointPoints[PositionIndex.left_shoulder.Int()].Child = jointPoints[PositionIndex.left_elbow.Int()];
        jointPoints[PositionIndex.left_elbow.Int()].Child = jointPoints[PositionIndex.left_wrist.Int()];
        jointPoints[PositionIndex.left_wrist.Int()].Parent = jointPoints[PositionIndex.left_elbow.Int()];
        jointPoints[PositionIndex.left_elbow.Int()].Parent = jointPoints[PositionIndex.left_shoulder.Int()];

        // Right Leg
        jointPoints[PositionIndex.right_hip.Int()].Child = jointPoints[PositionIndex.right_knee.Int()];
        jointPoints[PositionIndex.right_knee.Int()].Child = jointPoints[PositionIndex.right_ankle.Int()];
        jointPoints[PositionIndex.right_ankle.Int()].Child = jointPoints[PositionIndex.right_foot_index.Int()];
        jointPoints[PositionIndex.right_foot_index.Int()].Parent = jointPoints[PositionIndex.right_ankle.Int()];
        jointPoints[PositionIndex.right_ankle.Int()].Parent = jointPoints[PositionIndex.right_knee.Int()];
        jointPoints[PositionIndex.right_knee.Int()].Parent = jointPoints[PositionIndex.right_hip.Int()];

        // Left Leg
        jointPoints[PositionIndex.left_hip.Int()].Child = jointPoints[PositionIndex.left_knee.Int()];
        jointPoints[PositionIndex.left_knee.Int()].Child = jointPoints[PositionIndex.left_ankle.Int()];
        jointPoints[PositionIndex.left_ankle.Int()].Child = jointPoints[PositionIndex.left_foot_index.Int()];
        jointPoints[PositionIndex.right_foot_index.Int()].Parent = jointPoints[PositionIndex.left_ankle.Int()];
        jointPoints[PositionIndex.left_ankle.Int()].Parent = jointPoints[PositionIndex.left_knee.Int()];
        jointPoints[PositionIndex.left_knee.Int()].Parent = jointPoints[PositionIndex.left_hip.Int()];

        // Set Inverse
        var forward = TriangleNormal(pseudoNeckPosition, jointPoints[PositionIndex.left_hip.Int()].Transform.position, jointPoints[PositionIndex.right_hip.Int()].Transform.position);
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.DefaultPoseRotation = jointPoint.Transform.rotation;
            }

            if (jointPoint.Child != null)
            {
                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.DefaultPoseRotation;
            }
        }
        var hip = jointPoints[PositionIndex.hip.Int()];
        rootPosition = _mainCamera.transform.position + new Vector3(0, 0, 5.0f);
        hip.Inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        hip.InverseRotation = hip.Inverse * hip.DefaultPoseRotation;

        // For Head Rotation
        var head = jointPoints[PositionIndex.head.Int()];
        head.DefaultPoseRotation = jointPoints[PositionIndex.head.Int()].Transform.rotation;
        var gaze = jointPoints[PositionIndex.nose.Int()].Transform.position - jointPoints[PositionIndex.head.Int()].Transform.position;
        head.Inverse = Quaternion.Inverse(Quaternion.LookRotation(gaze));
        head.InverseRotation = head.Inverse * head.DefaultPoseRotation;
        
        var lHand = jointPoints[PositionIndex.left_wrist.Int()];
        var lf = TriangleNormal(lHand.Pos3D, jointPoints[PositionIndex.left_pinky.Int()].Pos3D, jointPoints[PositionIndex.left_index.Int()].Pos3D);
        lHand.DefaultPoseRotation = lHand.Transform.rotation;
        lHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.left_index.Int()].Transform.position - jointPoints[PositionIndex.left_pinky.Int()].Transform.position, lf));
        lHand.InverseRotation = lHand.Inverse * lHand.DefaultPoseRotation;
        
        var rHand = jointPoints[PositionIndex.right_wrist.Int()];
        var rf = TriangleNormal(rHand.Pos3D, jointPoints[PositionIndex.right_index.Int()].Pos3D, jointPoints[PositionIndex.right_pinky.Int()].Pos3D);
        rHand.DefaultPoseRotation = jointPoints[PositionIndex.right_wrist.Int()].Transform.rotation;
        rHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.right_index.Int()].Transform.position - jointPoints[PositionIndex.right_pinky.Int()].Transform.position, rf));
        rHand.InverseRotation = rHand.Inverse * rHand.DefaultPoseRotation;

        return JointPoints;
    }

    public void PoseUpdate()
    {
        // calculate movement range of z-coordinate from height
        var pseudoNeckPosition = (jointPoints[PositionIndex.left_shoulder.Int()].Pos3D + jointPoints[PositionIndex.right_shoulder.Int()].Pos3D) / 2.0f;

        // movement and rotation of center
        var currentRoot = rootPosition;
        var forward = -TriangleNormal(pseudoNeckPosition, jointPoints[PositionIndex.left_hip.Int()].Pos3D, jointPoints[PositionIndex.right_hip.Int()].Pos3D); // mirrored body
        jointPoints[PositionIndex.hip.Int()].Transform.position = currentRoot;
        jointPoints[PositionIndex.hip.Int()].Transform.rotation = Quaternion.LookRotation(forward) * jointPoints[PositionIndex.hip.Int()].InverseRotation;

        // rotate each of bones
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Child != null)
            {
                jointPoint.Transform.rotation = Quaternion.LookRotation(jointPoint.Pos3D - jointPoint.Child.Pos3D, forward) * jointPoint.InverseRotation;
            }
        }

        // Head Rotation
        var gaze = jointPoints[PositionIndex.nose.Int()].Pos3D - jointPoints[PositionIndex.head.Int()].Pos3D;
        var f = TriangleNormal(jointPoints[PositionIndex.nose.Int()].Pos3D, jointPoints[PositionIndex.right_ear.Int()].Pos3D, jointPoints[PositionIndex.left_ear.Int()].Pos3D);
        var head = jointPoints[PositionIndex.head.Int()];
        head.Transform.rotation = Quaternion.LookRotation(gaze, f) * head.InverseRotation;
        
        // Wrist rotation (Test code)
        var lHand = jointPoints[PositionIndex.left_wrist.Int()];
        var lf = TriangleNormal(lHand.Pos3D, jointPoints[PositionIndex.left_pinky.Int()].Pos3D, jointPoints[PositionIndex.left_index.Int()].Pos3D);
        lHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.left_index.Int()].Pos3D - jointPoints[PositionIndex.left_pinky.Int()].Pos3D, lf) * lHand.InverseRotation;

        var rHand = jointPoints[PositionIndex.right_wrist.Int()];
        var rf = TriangleNormal(rHand.Pos3D, jointPoints[PositionIndex.right_index.Int()].Pos3D, jointPoints[PositionIndex.right_pinky.Int()].Pos3D);
        rHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.right_index.Int()].Pos3D - jointPoints[PositionIndex.right_pinky.Int()].Pos3D, rf) * rHand.InverseRotation;
    }

    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }
}
