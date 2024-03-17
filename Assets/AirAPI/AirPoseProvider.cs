using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;
using UnityEngine.SpatialTracking;
public class AirPoseProvider : BasePoseProvider
{
#if UNITY_EDITOR_WIN
    
    [DllImport("AirAPI_Windows", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StartConnection();

    [DllImport("AirAPI_Windows", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StopConnection();

    [DllImport("AirAPI_Windows", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetQuaternion();

    [DllImport("AirAPI_Windows", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEuler();
    
#elif UNITY_EDITOR_LINUX
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StartConnection();
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StopConnection();

    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEuler();
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetQuaternion();
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Dummy();
    
    private float[] RawDummy()
    {
        var ptr = Dummy();
        var r = new float[3];
        Marshal.Copy(ptr, r, 0, 3);
        return r;
    }
    
#endif

    protected enum ConnectionStates
    {
        Disconnected = 0,
        Offline = 1,
        StandBy = 2,

        Connected = 3
    }

    protected static ConnectionStates connectionState = ConnectionStates.Disconnected;

    public bool IsConnecting()
    {
        return connectionState is ConnectionStates.Connected or ConnectionStates.StandBy;
    }

    public void TryConnect()
    {
        // Start the connection
        var code = StartConnection();
        if (code == 1)
        {
            connectionState = ConnectionStates.StandBy;
            Debug.Log("Glasses standing by");
        }
        else
            Debug.LogError("Connection error: return code " + code);
    }


    public void TryDisconnect()
    {
        if (IsConnecting())
        {
            var code = StopConnection();
            if (code == 1)
            {
                connectionState = ConnectionStates.Disconnected;
                Debug.Log("Glass disconnected");
            }
            else
            {
                connectionState = ConnectionStates.Offline;
                Debug.LogWarning("Glass disconnected with error: return code " + code);
            }
        }
        else
        {
            Debug.Log("Glass not connected, no need to disconnect");
        }
    }

    public float mouseSensitivity = 100.0f;

    protected Quaternion FromGlasses = Quaternion.identity;

    protected static Quaternion NO_READING_Q = Quaternion.Euler(90f, 0, 0);

    protected Vector3 FromMouse_Euler = Vector3.zero;
    protected Quaternion FromMouse = Quaternion.identity;

    protected Vector3 FromZeroing_Euler = Vector3.zero;
    protected Quaternion FromZeroing = Quaternion.identity;

    // Start is called before the first frame update
    private void Start()
    {
        TryConnect();
    }

    private void OnDestroy()
    {
        TryDisconnect();
    }

    // Update Pose
    public override PoseDataFlags GetPoseFromProvider(out Pose output)
    {
        if (IsConnecting()) UpdateFromGlasses();

        if (Input.GetMouseButton(1))
        {
            UpdateFromMouse();
        }

        var compound = FromMouse * FromZeroing * FromGlasses;

        output = new Pose(new Vector3(0, 0, 0), compound);
        return PoseDataFlags.Rotation;
    }
    
    private float[] RawEuler()
    {
        var ptr = GetEuler();
        var r = new float[3];
        Marshal.Copy(ptr, r, 0, 3);
        return r;
    }
    
    private void UpdateFromGlasses()
    {
        // var arr = RawDummy();
        
        var arr = RawEuler();
        
        // Debug.Log("glasses input: " + new Vector3(arr[0], arr[1], -arr[2]).ToString());

        var reading = Quaternion.Euler(-arr[1] + 90.0f, -arr[2], -arr[0]);
        
        if (connectionState == ConnectionStates.StandBy)
        {
            if (!reading.Equals(NO_READING_Q))
            {
                connectionState = ConnectionStates.Connected;
                Debug.Log("Glasses connected, start reading");
                FromGlasses = reading;
            }
        }
        else
        {
            FromGlasses = reading;
        }
    }

    private void UpdateFromMouse()
    {
        var deltaY = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        var deltaX = -Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        // Mouse & Unity XY axis are opposite

        FromMouse_Euler += new Vector3(deltaX, deltaY, 0.0f);

        // Debug.Log("mouse pressed:" + FromMouseXY);

        FromMouse = Quaternion.Euler(-FromMouse_Euler);
    }

    public void ZeroY()
    {
        var fromGlassesY = (FromGlasses * FromMouse).eulerAngles.y;
        FromZeroing_Euler.y = -fromGlassesY;
        FromZeroing = Quaternion.Euler(FromZeroing_Euler);
    }
}