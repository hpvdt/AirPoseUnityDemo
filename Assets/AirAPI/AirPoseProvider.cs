using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
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

#else
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StartConnection();
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StopConnection();

    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEuler();
    
    [DllImport("libar_drivers.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetQuaternion();

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
                Debug.Log("Glassed disconnected");
            }
            else
            {
                connectionState = ConnectionStates.Offline;
                Debug.LogWarning("Glassed disconnected with error: return code " + code);
            }
        }
        else
        {
            Debug.Log("Glassed not connected, no need to disconnect");
        }
    }

    public float mouseSensitivity = 100.0f;

    private Quaternion _fromGlasses = Quaternion.identity;

    private static readonly Quaternion Qid = Quaternion.identity.normalized;

    private Vector3 _fromMouseEuler = Vector3.zero;
    private Quaternion _fromMouse = Quaternion.identity;

    private Vector3 _fromZeroingEuler = Vector3.zero;
    private Quaternion _fromZeroing = Quaternion.identity;

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

        var compound = _fromMouse * _fromZeroing * _fromGlasses;

        output = new Pose(new Vector3(0, 0, 0), compound);
        return PoseDataFlags.Rotation;
    }

    private float[] GetEulerArray()
    {
        var ptr = GetEuler();
        var r = new float[3];
        Marshal.Copy(ptr, r, 0, 3);
        return r;
    }

    private static readonly Quaternion QNeutral = Quaternion.Euler(90f, 0f, 0f).normalized;

    private Quaternion GetQuaternion_direct()
    {
        var ptr = GetQuaternion();
        // always in WXYZ order
        // see https://github.com/xioTechnologies/Fusion/blob/e7d2b41e6506fa9c85492b91becf003262f14977/Fusion/FusionMath.h#L36

        var arr = new float[4];
        Marshal.Copy(ptr, arr, 0, 4);

        var qRaw = new Quaternion(-arr[1], -arr[3], -arr[2], arr[0]);
        // chiral conversion
        // see https://stackoverflow.com/questions/28673777/convert-quaternion-from-right-handed-to-left-handed-coordinate-system

        var q = qRaw * QNeutral;
        // var q = Quaternion.Euler(qRaw.eulerAngles + new Vector3(0f, 0f, 0f));
        return q;
    }

    private Quaternion GetQuaternion_fromEuler()
    {
        // yaw : Up-Down, pitch : Left-Right, roll : Forward-Backward

        // receiving data in right-hand BLD frame?
        // chosen to be compatible with alternative Windows driver (https://github.com/wheaney/OpenVR-xrealAirGlassesHMD)

        var arr = GetEulerArray();
        var roll = arr[0];
        var pitch = arr[1];
        var yaw = arr[2];

        var r = Quaternion.Euler(-pitch + 90f, -yaw, -roll);

        return r;
    }

    private Quaternion GetQuaternion_verified()
    {
        var r1 = GetQuaternion_direct();
        var r2 = GetQuaternion_fromEuler();

        var errorBound = 10f;
        Debug.Assert(r1 == r1.normalized);
        Debug.Assert(r2 == r2.normalized);

        var error = r1.eulerAngles - r2.eulerAngles; // TODO: need 360 degree wrap-around

        Debug.Assert(error.magnitude <= errorBound,
            $"error = {error}");

        var reading = r1;
        return reading;
    }

    private void UpdateFromGlasses()
    {
        var reading = GetQuaternion_verified();

        var effective = reading;
        // var effective = (reading * Q_NEUTRAL).normalized;

        if (connectionState == ConnectionStates.StandBy)
        {
            if (!reading.Equals(Qid))
            {
                connectionState = ConnectionStates.Connected;
                Debug.Log("Glasses connected, start reading");
                _fromGlasses = effective;
            }
        }
        else
        {
            _fromGlasses = effective;
        }
    }

    private void UpdateFromMouse()
    {
        var deltaY = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        var deltaX = -Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        // Mouse & Unity XY axis are opposite

        _fromMouseEuler += new Vector3(deltaX, deltaY, 0.0f);

        // Debug.Log("mouse pressed:" + FromMouseXY);

        _fromMouse = Quaternion.Euler(-_fromMouseEuler);
    }

    public void ZeroY()
    {
        var fromGlassesY = (_fromGlasses * _fromMouse).eulerAngles.y;
        _fromZeroingEuler.y = -fromGlassesY;
        _fromZeroing = Quaternion.Euler(_fromZeroingEuler);
    }
}