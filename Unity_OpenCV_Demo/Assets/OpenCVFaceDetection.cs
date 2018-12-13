using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

// Define the functions which can be called from the .dll
internal static class OpenCVInterop
{
    [DllImport("OpenCV_Interface")]
    internal static extern int Init(ref int outCameraWidth, ref int outCameraHeight);

    [DllImport("OpenCV_Interface")]
    internal static extern void Close();

    [DllImport("OpenCV_Interface")]
    internal static extern void SetScale(int downscale);

    [DllImport("OpenCV_Interface")]
    internal static extern void FreeMemory();

    [DllImport("OpenCV_Interface")]
    internal unsafe static extern CvFrame Detect(CvCircle* outFaces, int maxOutFacesCount, ref int outDetectedFacesCount);
}

// Define the structure to be sequential and with the correct byte size 
[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct CvCircle
{
    public int X, Y, Radius; //  4 bytes * 3 ints = 12 bytes total
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
public struct CvFrame
{
    public IntPtr memPtr; //IntPtr is 4 bytes on x86 and 8 on x64
    public int x, y, bufferSize, error;
}

public class OpenCVFaceDetection : MonoBehaviour
{
    private static List<Vector2> NormalizedFacePositions { get; set; }
    private static Vector2 CameraResolution;

    private const int DetectionDownScale = 1; // scale down for faster performance

    private int _maxFaceDetectCount = 5;
    private CvCircle[] _faces;
    private CvFrame cvFrame;

    public RawImage rawImage;
    private Texture2D tex;

    private Vector2 oldFrameSize;
    private enum CameraState { connected, disconnected, connecting };
    private CameraState cameraState = CameraState.disconnected;
    private int lastError;

    void Start()
    {
        Initialize();
    }

    private bool Initialize()
    {
        cameraState = CameraState.connecting;
        //Debug.Log("Initialize OpenCV Face Detection");
        int camWidth = 0, camHeight = 0;
        int result = OpenCVInterop.Init(ref camWidth, ref camHeight);
        if (result < 0)
        {
            if (result == -1)
            {
                Debug.LogWarningFormat("[{0}] Failed to find cascades definition.", GetType());
            }
            else if (result == -2)
            {
                Debug.LogWarningFormat("[{0}] Failed to open camera stream.", GetType());
            }
            lastError = -4;
            cameraState = CameraState.disconnected;
            return false;
        }
        CameraResolution = new Vector2(camWidth, camHeight);
        _faces = new CvCircle[_maxFaceDetectCount];
        NormalizedFacePositions = new List<Vector2>();
        OpenCVInterop.SetScale(DetectionDownScale);
        lastError = 0;
        cameraState = CameraState.connected;
        return true;
    }

    void OnApplicationQuit()
    {
        if (cameraState == CameraState.connected)
        {
            OpenCVInterop.Close();
            Destroy(tex);
        }
    }

    void Update()
    {
        // attempt to reinitialize if connection was lost last frame or is not present on start
        if (cameraState == CameraState.disconnected || lastError < 0)
        {
            bool initialized = Initialize();
            if (!initialized) { return; }
        }

        // this is the main call to the dll that does the face detection processing on an OpenCV frame 
        // it returns the data needed to build output in Unity
        cvFrame = GetFaceDetectionResults();
        //Debug.LogFormat("from frame, x = {0}, y = {1}, bufferSize = {2}, memPtr = {3}, error = {4}", cvFrame.x, cvFrame.y, cvFrame.bufferSize, cvFrame.memPtr, cvFrame.error);

        // handle error cases returend by dll; i.e. disconnected usb device
        if (cvFrame.error < 0)
        {
            //Debug.LogFormat("RunFaceDetection returned error {0}", cvFrame.error);
            OpenCVInterop.Close();
            cameraState = CameraState.disconnected;
            lastError = cvFrame.error; // so we know to reinitialize rather than run demo next frame
            return;
        }

        // create array, marshal data from pointer, output, then free the pointer
        byte[] bytes = new byte[cvFrame.bufferSize];
        Marshal.Copy(cvFrame.memPtr, bytes, 0, cvFrame.bufferSize);
        OutputFrame(cvFrame, bytes);
        OpenCVInterop.FreeMemory();
    }

    public void OutputFrame(CvFrame cvf, byte[] bytes)
    {
        if (cvf.x != oldFrameSize.x || cvf.y != oldFrameSize.y)
        {
            //Debug.Log("Create new texture 2d");
            tex = new Texture2D(cvf.x, cvf.y, TextureFormat.RGB24, false); // must be called on main thread
            oldFrameSize.x = cvf.x;
            oldFrameSize.y = cvf.y;
        }
        tex.LoadRawTextureData(bytes);
        tex.Apply();
        rawImage.texture = tex;
        rawImage.SetNativeSize();
    }

    public CvFrame GetFaceDetectionResults()
    {
        CvFrame cvf;
        int detectedFaceCount = 0;
        unsafe
        {
            fixed (CvCircle* outFaces = _faces)
            {
                cvf = OpenCVInterop.Detect(outFaces, _maxFaceDetectCount, ref detectedFaceCount);
            }
        }

        NormalizedFacePositions.Clear();
        for (int i = 0; i < detectedFaceCount; i++)
        {
            NormalizedFacePositions.Add(new Vector2((_faces[i].X * DetectionDownScale) / CameraResolution.x, 1f - ((_faces[i].Y * DetectionDownScale) / CameraResolution.y)));
        }

        for (int i = 0; i < NormalizedFacePositions.Count; i++)
        {
            float x = NormalizedFacePositions[i].x;
            float y = NormalizedFacePositions[i].y;
            Debug.LogFormat("For face # {0} x = {1} and y = {2}", i, x, y); // output the face positions
        }
        return cvf;
    }
}
