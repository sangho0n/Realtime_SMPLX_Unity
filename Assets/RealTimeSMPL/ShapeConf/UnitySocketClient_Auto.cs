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

  [SerializeField] private InputField inputField;

  //private int num = 0;
  Byte[] result;
  Byte[] ans = new byte[80]; //float인데, 8byte가 : 80+24+63*8 = 608
  float[] val = new float[10];
  public byte gen = 0;
  double[] body_pose = new double[66];
  private float height_F = 164.8f;
  private float midHeight_F = 1.001f;
  private float height_M = 178.1f;
  private float midHeight_M = 0.5465f;
  [SerializeField] private TextAsset ipConf;
  private StringReader sr;

  private void Start()
  {
    sr = new StringReader(ipConf.text);
  }

  public void alterGen()
  {
    genderModels[this.gen].gameObject.SetActive(false);
    //1 for male, 0 for female
    this.gen = (byte)(this.gen ^ System.Convert.ToByte(1));
    genderModels[this.gen].gameObject.SetActive(true);
    targetModel = genderModels[this.gen];
    for (int i = 0; i < 10; i++)
    {
      targetModel.betas[i] = 0f;
    }

    targetModel.SetBetaShapes();
  }

  private byte[] takeAPicture()
  {
    Texture2D camTex = new Texture2D(camTexture.textureWidth, camTexture.textureHeight, TextureFormat.RGB24, false);
    camTex.SetPixels(camTexture.GetPixels);
    camTex.Apply();
    return ImageConversion.EncodeToJPG(camTex);
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
    TcpClient socketConnection = new TcpClient();
    NetworkStream stream;
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
    if (socketConnection != null)
    {
      try
      {
        stream = socketConnection.GetStream();
        if (stream.CanWrite)
        {
          stream.Write(b, 0, b.Length);
        }
      }
      catch (SocketException socketException)
      {
        Debug.Log("Socket exception: " + socketException);
        //yield break;
        return;
      }

      Debug.Log("Sending Actual Data Done");
    }
    else
    {
      Debug.Log("Failed to send data");
      Quit();
    }

    try
    {
      stream = socketConnection.GetStream();
      await stream.ReadAsync(ans, 0, ans.Length);
      SetBetas();
    }
    catch (SocketException socketException)
    {
      Debug.Log("Socket exception: " + socketException);
      //yield break;
      return;
    }

    socketConnection.Close();
    //yield break;
    return;
  }

  private async void SetBetas()
  {
    Debug.Log("beta");
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

    //1. temporary height change
    //TODO: 예외처리는 하지 않았음. 비어있으면 맞기고, 키를 넣었다면 처리함.
    if (inputField.text != "")
    {
      float temp = float.Parse(inputField.text);
      if (gen == 0) //female
      {
        temp = ((temp - height_F) / midHeight_F) * 0.1f;
        Debug.Log(temp);
      }
      else
      {
        temp = ((height_M - temp) / midHeight_M) * 0.1f;
        Debug.Log(temp);
      }

      if (temp > 5)
      {
        temp = 5.0f;
      }
      else if (temp < -5)
      {
        temp = -5.0f;
      }

      Debug.Log(temp);
      targetModel.betas[0] = temp;
    }

    targetModel.SetBetaShapes();
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
    {
      //StartCoroutine(giveServerPic());
      giveServerPic();
    }
  }

  private void ParseIpConf(out string hostname, out int port)
  {
    try
    {
      sr = new StringReader(ipConf.text);
      hostname = sr.ReadLine();
      port = int.Parse(sr.ReadLine());
    }
    catch (Exception e)
    {
      Debug.LogWarning(e.Message);
      hostname = null;
      port = 0;
    }
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
