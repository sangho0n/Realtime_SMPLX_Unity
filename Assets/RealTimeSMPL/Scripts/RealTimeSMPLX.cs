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
        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public JointPoint Child = null;
        public JointPoint Parent = null;

        // For Kalman filter
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }

    public class Skeleton
    {
        public GameObject LineObject;
        public LineRenderer Line;

        public JointPoint start = null;
        public JointPoint end = null;
    }

    // Joint position and bone
    private JointPoint[] jointPoints;
    public JointPoint[] JointPoints { get { return jointPoints; } }

    private Vector3 rootPosition; // Initial center position

    private Quaternion InitGazeRotation;
    private Quaternion gazeInverse;

    public GameObject Nose;
    private Animator anim;

    // Move in z direction
    private float centerTall = 224 * 0.75f;
    private float tall = 224 * 0.75f;
    private float prevTall = 224 * 0.75f;
    public float ZScale = 0.8f;

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
        //jointPoints[PositionIndex.left_ear.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.left_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftEye);
        jointPoints[PositionIndex.left_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightEye);
        //jointPoints[PositionIndex.right_ear.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.right_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightEye);
        jointPoints[PositionIndex.right_eye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftEye);
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
        jointPoints[PositionIndex.right_elbow.Int()].Parent = jointPoints[PositionIndex.right_shoulder.Int()];

        // Left Arm
        jointPoints[PositionIndex.left_shoulder.Int()].Child = jointPoints[PositionIndex.left_elbow.Int()];
        jointPoints[PositionIndex.left_elbow.Int()].Child = jointPoints[PositionIndex.left_wrist.Int()];
        jointPoints[PositionIndex.left_elbow.Int()].Parent = jointPoints[PositionIndex.left_shoulder.Int()];

        // Fase

        // Right Leg
        jointPoints[PositionIndex.right_hip.Int()].Child = jointPoints[PositionIndex.right_knee.Int()];
        jointPoints[PositionIndex.right_knee.Int()].Child = jointPoints[PositionIndex.right_ankle.Int()];
        jointPoints[PositionIndex.right_ankle.Int()].Child = jointPoints[PositionIndex.right_foot_index.Int()];
        jointPoints[PositionIndex.right_ankle.Int()].Parent = jointPoints[PositionIndex.right_knee.Int()];

        // Left Leg
        jointPoints[PositionIndex.left_hip.Int()].Child = jointPoints[PositionIndex.left_knee.Int()];
        jointPoints[PositionIndex.left_knee.Int()].Child = jointPoints[PositionIndex.left_ankle.Int()];
        jointPoints[PositionIndex.left_ankle.Int()].Child = jointPoints[PositionIndex.left_foot_index.Int()];
        jointPoints[PositionIndex.left_ankle.Int()].Parent = jointPoints[PositionIndex.left_knee.Int()];

        // etc        
        //jointPoints[PositionIndex.head.Int()].Child = jointPoints[PositionIndex.nose.Int()];

        // Set Inverse
        var forward = TriangleNormal(pseudoNeckPosition, jointPoints[PositionIndex.left_hip.Int()].Transform.position, jointPoints[PositionIndex.right_hip.Int()].Transform.position);
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.InitRotation = jointPoint.Transform.rotation;
            }

            if (jointPoint.Child != null)
            {
                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.InitRotation;
            }
        }
        var hip = jointPoints[PositionIndex.hip.Int()];
        rootPosition = _mainCamera.transform.position + new Vector3(0, 0, 5.0f);
        hip.Inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        hip.InverseRotation = hip.Inverse * hip.InitRotation;

        // For Head Rotation
        var head = jointPoints[PositionIndex.head.Int()];
        head.InitRotation = jointPoints[PositionIndex.head.Int()].Transform.rotation;
        var gaze = jointPoints[PositionIndex.nose.Int()].Transform.position - jointPoints[PositionIndex.head.Int()].Transform.position;
        head.Inverse = Quaternion.Inverse(Quaternion.LookRotation(gaze));
        head.InverseRotation = head.Inverse * head.InitRotation;
        
        var lHand = jointPoints[PositionIndex.left_wrist.Int()];
        var lf = TriangleNormal(lHand.Pos3D, jointPoints[PositionIndex.left_pinky.Int()].Pos3D, jointPoints[PositionIndex.left_index.Int()].Pos3D);
        lHand.InitRotation = lHand.Transform.rotation;
        lHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.left_index.Int()].Transform.position - jointPoints[PositionIndex.left_pinky.Int()].Transform.position, lf));
        lHand.InverseRotation = lHand.Inverse * lHand.InitRotation;

        var rHand = jointPoints[PositionIndex.right_wrist.Int()];
        var rf = TriangleNormal(rHand.Pos3D, jointPoints[PositionIndex.right_index.Int()].Pos3D, jointPoints[PositionIndex.right_pinky.Int()].Pos3D);
        rHand.InitRotation = jointPoints[PositionIndex.right_wrist.Int()].Transform.rotation;
        rHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.right_index.Int()].Transform.position - jointPoints[PositionIndex.right_pinky.Int()].Transform.position, rf));
        rHand.InverseRotation = rHand.Inverse * rHand.InitRotation;

        return JointPoints;
    }

    public void PoseUpdate()
    {
        // calculate movement range of z-coordinate from height
        var pseudoNeckPosition = (jointPoints[PositionIndex.left_shoulder.Int()].Pos3D + jointPoints[PositionIndex.right_shoulder.Int()].Pos3D) / 2.0f;
        var t1 = Vector3.Distance(pseudoNeckPosition, jointPoints[PositionIndex.head.Int()].Pos3D);
        var t2 = Vector3.Distance(pseudoNeckPosition, jointPoints[PositionIndex.spine.Int()].Pos3D);
        var t3 = Vector3.Distance(jointPoints[PositionIndex.spine.Int()].Pos3D, jointPoints[PositionIndex.hip.Int()].Pos3D);
        var t4r = Vector3.Distance(jointPoints[PositionIndex.right_hip.Int()].Pos3D, jointPoints[PositionIndex.right_knee.Int()].Pos3D);
        var t4l = Vector3.Distance(jointPoints[PositionIndex.left_hip.Int()].Pos3D, jointPoints[PositionIndex.left_knee.Int()].Pos3D);
        var t4 = (t4r + t4l) / 2f;
        var t5r = Vector3.Distance(jointPoints[PositionIndex.right_knee.Int()].Pos3D, jointPoints[PositionIndex.right_ankle.Int()].Pos3D);
        var t5l = Vector3.Distance(jointPoints[PositionIndex.left_knee.Int()].Pos3D, jointPoints[PositionIndex.left_ankle.Int()].Pos3D);
        var t5 = (t5r + t5l) / 2f;
        var t = t1 + t2 + t3 + t4 + t5;


        // Low pass filter in z direction
        tall = t * 0.7f + prevTall * 0.3f;
        prevTall = tall;

        if (tall == 0)
        {
            tall = centerTall;
        }
        var dz = (centerTall - tall) / centerTall * ZScale;

        // movement and rotatation of center
        var currentRoot = rootPosition;// + new Vector3(jointPoints[PositionIndex.hip.Int()].Pos3D.x,0,jointPoints[PositionIndex.hip.Int()].Pos3D.z) * 10.0f;
        var forward = -TriangleNormal(pseudoNeckPosition, jointPoints[PositionIndex.left_hip.Int()].Pos3D, jointPoints[PositionIndex.right_hip.Int()].Pos3D);
        jointPoints[PositionIndex.hip.Int()].Transform.position = jointPoints[PositionIndex.hip.Int()].Pos3D * 0.005f + new Vector3(currentRoot.x, currentRoot.y, currentRoot.z + dz);
        jointPoints[PositionIndex.hip.Int()].Transform.rotation = Quaternion.LookRotation(forward) * jointPoints[PositionIndex.hip.Int()].InverseRotation;

        // rotate each of bones
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Parent != null)
            {
                var fv = jointPoint.Parent.Pos3D - jointPoint.Pos3D;
                jointPoint.Transform.rotation = Quaternion.LookRotation(jointPoint.Pos3D - jointPoint.Child.Pos3D, -fv) * jointPoint.InverseRotation;
            }
            else if (jointPoint.Child != null)
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
