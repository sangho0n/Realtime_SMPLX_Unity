using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedPipe2HumanoidAvatar : MonoBehaviour
{
    private string[] _mediaPipeJointNames = {"nose", "left_eye_inner", "left_eye", "left_eye_outer", "right_eye_inner", "right_eye", "right_eye_outer", "left_ear", "right_ear", "mouth_left",
                                                "mouth_right", "left_shoulder", "right_shoulder", "left_elbow", "right_elbow", "left_wrist", "right_wrist", "left_pinky", "right_pinky", "left_index",
                                                "right_index", "left_thumb", "right_thumb", "left_hip", "right_hip", "left_knee", "right_knee", "left_ankle", "right_ankle", "left_heel",
                                                "right_heel", "left_foot_index" , "right_foot_index", "head", "hip", "spine"}; // 33 joints + 3 unity mandatory joints
    private Dictionary<string, int> _mediaPipeJointNameToIndex = new Dictionary<string, int>();

    [SerializeField]
    private RealTimeSMPLX _targetSMPL;
    private RealTimeSMPLX.JointPoint[] jointPoints;
    private float _spineHipWeight = 0.75f;

    public float KalmanParamQ;
    public float KalmanParamR;
    
    void Start()
    {
        for(int i = 0; i < _mediaPipeJointNames.Length; i++)
        {
            _mediaPipeJointNameToIndex.Add(_mediaPipeJointNames[i], i);
        }
        _targetSMPL = gameObject.GetComponent<RealTimeSMPLX>();
        jointPoints = _targetSMPL.Init();
    }

    public async void Convert(string landmarks)
    {
        Debug.Log("converting,,,");
        Vector3[] allJointPos = new Vector3[36];
        SimpleJSON.JSONArray jointPosArray;
        SimpleJSON.JSONNode landmarkJson = SimpleJSON.JSON.Parse(landmarks);
        
        jointPosArray = landmarkJson["landmark"].AsArray;

        for(int i = 0; i < jointPosArray.Count; i++){
            var joint = jointPosArray[i];
            var pos = Mediapipe.Unity.CoordinateSystem.RealWorldCoordinate.RealWorldToLocalPoint(joint["x"].AsFloat, joint["y"].AsFloat, joint["z"].AsFloat, 
                50, 50, 50, 
                Mediapipe.Unity.RotationAngle.Rotation0,
                isMirrored: true);
            //var pos = new Vector3(joint["x"].AsFloat, joint["y"].AsFloat, joint["z"].AsFloat);
            
            //allJointPos[i] = pos;
            if(i == 0) allJointPos[i] = pos;
            else if(i >= 1 && i <= 3) allJointPos[i + 3] = pos;
            else if (i >= 4 && i <= 6) allJointPos[i - 3] = pos;
            else if (i % 2 == 1) allJointPos[i + 1] = pos;
            else allJointPos[i - 1] = pos;
            
            //Debug.Log("joints pos : " + pos);
        }

        //head
        allJointPos[33] = (allJointPos[_mediaPipeJointNameToIndex["left_ear"]] + allJointPos[_mediaPipeJointNameToIndex["right_ear"]]) / 2.0f; 
        //hip
        allJointPos[34] = (allJointPos[_mediaPipeJointNameToIndex["left_hip"]] + allJointPos[_mediaPipeJointNameToIndex["right_hip"]]) / 2.0f; 
        //spine
        var pseudoNeckPosition = (allJointPos[_mediaPipeJointNameToIndex["left_shoulder"]] + allJointPos[_mediaPipeJointNameToIndex["right_shoulder"]]) / 2.0f;
        allJointPos[35] = pseudoNeckPosition * (1.0f - _spineHipWeight) + allJointPos[_mediaPipeJointNameToIndex["hip"]] * _spineHipWeight;

        PredictPose(allJointPos);
    }

    private void PredictPose(Vector3[] allJointPos)
    {
        for(int j = 0; j < allJointPos.Length; j++)
        {
            jointPoints[j].Now3D = allJointPos[j];
        }

        foreach(var joint in jointPoints)
        {
            KalmanUpdate(joint);  
        }
    }

    private void KalmanUpdate(RealTimeSMPLX.JointPoint measurement)
    {
        measurementUpdate(measurement);
        measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
        measurement.X = measurement.Pos3D;
    }

	private void measurementUpdate(RealTimeSMPLX.JointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
        measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
        measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);
        measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
        measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
        measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);
    }
}
