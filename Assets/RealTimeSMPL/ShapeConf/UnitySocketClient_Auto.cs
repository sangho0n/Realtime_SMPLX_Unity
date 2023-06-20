using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UnitySocketClient_Auto : MonoBehaviour
{
  //private TcpClient socketConnection;

  /// //////////////////////////////////////
  public RawImage display;

  public Vector2 camwh;

  public int camIndex = 0;

  //private WebCamTexture camTexture;
  [SerializeField] private Mediapipe.Unity.WebCamSource camTexture;
  [SerializeField] private RealTimeSMPLX targetModel;
  [SerializeField] private RealTimeSMPLX[] genderModels = new RealTimeSMPLX[2];

  //[SerializeField] private InputField inputField;

  //private int num = 0;

  Byte[] result = Encoding.UTF8.GetBytes("Yes");
  Byte[] ans = new byte[608]; //float인데, 8byte가 : 80+24+63*8 = 608
  float[] val = new float[10];

  public byte gen = 0;
  double[] body_pose = new double[66];

  public bool realTimeEst = false;

  public bool useHeight = false;

  private float height_F = 164.8f;
  private float midHeight_F = 1.001f;
  private float height_M = 178.1f;
  private float midHeight_M = 0.5465f;

  [SerializeField] private TextAsset ipConf;

  public void alterGen()
  {
    genderModels[this.gen].gameObject.SetActive(false);
    //1 for male, 0 for female
    this.gen = (byte)(this.gen ^ System.Convert.ToByte(1));
    genderModels[this.gen].gameObject.SetActive(true);
    targetModel = genderModels[this.gen];
    targetModel.gameObject.transform.rotation = Quaternion.Euler(0f, 216f, 0f);
    for (int i = 0; i < 10; i++)
    {
      targetModel.betas[i] = 0f;
    }

    targetModel.SetBetaShapes();
  }

  public void alterMode()
  {
    targetModel.SetBodyPose(RealTimeSMPLX.BodyPose.T);
    if (!this.realTimeEst)
    {
      targetModel.transform.localRotation = Quaternion.Euler(new Vector3(180.0f, 180.0f, 0.0f));
    }
    else
    {
      targetModel.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 216.0f));
    }

    this.realTimeEst = !this.realTimeEst;
  }

  private byte[] takeAPicture()
  {
    Texture2D camTex = new Texture2D(camTexture.textureWidth, camTexture.textureHeight, TextureFormat.RGB24, false);
    camTex.SetPixels(camTexture.GetPixels);
    camTex.Apply();

    return ImageConversion.EncodeToJPG(camTex);
  }

  public void alterUseHeight()
  {
    useHeight = !useHeight;
    if (useHeight)
    {
      Debug.Log("사용자의 키를 사용합니다.");
    }
    else
    {
      Debug.Log("키를 estimation에게 전담합니다.");
    }
  }

  // Start is called before the first frame update
  void OnApplicationQuit()
  {
    camTexture.Stop();
    //socketConnection.Close();
  }

  //IEnumerator giveServerPic()
  public async void giveServerPic()
  {
    TcpClient socketConnection;
    //Try connecting to Python
    try
    {
      string hostname;
      int port;
      ParseIpConf(out hostname, out port);
      socketConnection = new TcpClient(hostname, port);
      //Debug.Log("Connecting Done");
    }

    catch (Exception e)
    {
      Debug.Log("On client connect exception " + e);
      //yield break;
      return;
    }

    byte[] b = takeAPicture();
    byte[] size = BitConverter.GetBytes(b.Length);
    Debug.Log(b.Length);
    b = ByteHelper.Combine(size, b);

    //Sending the Actual Data
    if (socketConnection != null)
    {
      //Debug.Log("Sending Actual Data...");
      try
      {
        NetworkStream stream = socketConnection.GetStream();
        if (stream.CanWrite)
        {
          await stream.WriteAsync(b, 0, b.Length);
        }
      }
      catch (SocketException socketException)
      {
        Debug.Log("Socket exception: " + socketException);
        //yield break;
        return;
      }
      //Debug.Log("Sending Actual Data Done");
      //TODO : You have to check if everything went well!
    }
    else
    {
      Debug.Log("Failed to send data");
      Quit();
    }

    while (true)
    {
      //Receiving if the acutal data got there correctly
      //Debug.Log("Receiving Answer...");
      try
      {
        NetworkStream stream = socketConnection.GetStream();
        if (stream.CanRead)
        {
          await stream.ReadAsync(ans, 0, ans.Length);
          //targetModel.SetBodyPose(SMPLX.BodyPose.T, false);
          if (gen == 0) //female
          {
            targetModel.betas[0] = (float)System.BitConverter.ToDouble(ans, 0);
          }
          else
          {
            targetModel.betas[0] = -1 * (float)System.BitConverter.ToDouble(ans, 0);
          }

          //for(int i = 0; i < 10; i++)
          for (int i = 1; i < 10; i++)
          {
            val[i] = (float)System.BitConverter.ToDouble(ans, i * 8);
            //print("Shape[" + i + "] = " + val[i]);

            //code for using HuManiFlow
            targetModel.betas[i] = -1 * val[i];

            //targetModel.betas[i] = val[i] * 10;
            //simple change to make it more 'dramatic'. Actually you shouldn't do this.
          }
          
          Debug.Log("betas : " + targetModel.betas);

          //1. temporary height change
          /*
                    if (useHeight && inputField.text != "")
                    {
                      float temp = float.Parse(inputField.text);
                      if (gen == 0)//female
                      {

                        temp = ((temp - height_F) / midHeight_F) * 0.1f;
                        Debug.Log(temp);
                      }
                      else
                      {
                        temp = ((height_M - temp) / midHeight_M) * 0.1f;
                        Debug.Log(temp);
                      }
                      if (temp > 5) { temp = 5.0f; }
                      else if (temp < -5) { temp = -5.0f; }
                      Debug.Log(temp);
                      targetModel.betas[0] = temp;
                    }*/
          targetModel.SetBetaShapes();

          break;
        }
      }
      catch (SocketException socketException)
      {
        Debug.Log("Socket exception: " + socketException);
        //yield break;
        return;
      }
    }

    socketConnection.Close();

    //yield break;
    return;
  }

  private void Update()
  {
    if (!realTimeEst && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1)))
    {
      //StartCoroutine(giveServerPic());
      giveServerPic();
    }

    if (realTimeEst)
    {
      giveServerPic();
    }
  }

  private void ParseIpConf(out string hostname, out int port)
  {
    string[] tokens = ipConf.text.Split("\n");
    hostname = tokens[0];
    port = int.Parse(tokens[1]);
  }

  public void Quit()
  {
#if UNITY_STANDALONE
    Application.Quit();
#endif
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }
}
