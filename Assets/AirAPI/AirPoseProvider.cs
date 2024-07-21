using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
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
        {
            Debug.LogError("Connection error: return code " + code);
        }
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

        if (Input.GetMouseButton(1)) UpdateFromMouse();

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

    // the following rules are chosen to be
    //  compatible with alternative Windows driver (https://github.com/wheaney/OpenVR-xrealAirGlassesHMD)

    private Quaternion GetQuaternion_direct()
    {
        var ptr = GetQuaternion();
        // receiving in WIJK order (left hand)
        // see https://github.com/xioTechnologies/Fusion/blob/e7d2b41e6506fa9c85492b91becf003262f14977/Fusion/FusionMath.h#L36

        var arr = new float[4];
        Marshal.Copy(ptr, arr, 0, 4);

        var qRaw = new Quaternion(-arr[1], -arr[3], -arr[2], arr[0]);

        var q = qRaw * QNeutral;

        // converting to IKJW order (right hand)
        // see https://stackoverflow.com/questions/28673777/convert-quaternion-from-right-handed-to-left-handed-coordinate-system
        // neutral position is 90 degree pitch downward

        return q;
    }

    private Quaternion GetQuaternion_fromEuler()
    {
        var arr = GetEulerArray();
        // receiving in FRU order (left hand axes, right hand rotation)
        // Forward - roll
        // Right - pitch
        // Up - yaw

        var roll = arr[0];
        var pitch = arr[1];
        var yaw = arr[2];

        var arr2 = new Vector3(-(pitch - 90f), -yaw, -roll);
        var r = Quaternion.Euler(arr2[0], arr2[1], arr2[2]);

        Debug.Log($"Euler: {arr2[0]}, {arr2[1]}, {arr2[2]}");

        // converting to LDB order (right hand axes, right hand rotation)
        // Left - pitch
        // Down - yaw
        // Backward - roll
        // neutral position is 90 degree pitch downward

        return r;
    }

    public static Vector3 ClampTo180(Vector3 v)
    {
        return new Vector3(
            ClampAngle180(v.x),
            ClampAngle180(v.y),
            ClampAngle180(v.z)
        );
    }

    private static float ClampAngle180(float angle)
    {
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;
        return angle;
    }

    private Quaternion GetQuaternion_verified()
    {
        var r1 = GetQuaternion_direct();
        var r2 = GetQuaternion_fromEuler();

        var errorBound = 1f;
        Debug.Assert(r1 == r1.normalized);
        Debug.Assert(r2 == r2.normalized);

        // {
        //     var error = ClampTo180(r1.eulerAngles - r2.eulerAngles);
        //
        //     Debug.Assert(error.magnitude <= errorBound,
        //         $"error = {error}");
        // }

        {
            Vector3 fwd;
            {
                var q = Quaternion.Inverse(r1) * r2;
                Debug.Assert(r1 * q == r2, "fwd error!");
                fwd = ClampTo180(q.eulerAngles);
            }

            Vector3 rev;
            {
                var q = Quaternion.Inverse(r2) * r1;
                Debug.Assert(r2 * q == r1, "rev error!");
                rev = ClampTo180(q.eulerAngles);
            }

            var angle = Quaternion.Angle(r1, r2);

            Debug.Assert(fwd.magnitude <= errorBound,
                $"fwd = {fwd} ; rev = {rev} ; angle = {angle}");
        }

        var reading = r2;
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