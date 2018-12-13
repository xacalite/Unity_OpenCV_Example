#include "opencv2/core.hpp"
#include "opencv2/objdetect.hpp"
#include "opencv2/highgui.hpp"
#include "opencv2/objdetect.hpp"
#include "opencv2/videoio.hpp"
#include "opencv2/imgproc.hpp"
#include <iostream>
#include <stdio.h>

using namespace std;
using namespace cv;

// Declare structures to be used to pass data from C++ to Mono.
struct Circle
{
	Circle(int x, int y, int radius) : X(x), Y(y), Radius(radius) {}
	int X, Y, Radius;
};

struct Frame
{
	unsigned char* memPtr;
	int x, y, bufferSize, error;
};

CascadeClassifier _faceCascade;
String _windowName = "Unity OpenCV Face Detection and Interop Sample";
VideoCapture _capture;
int _scale = 1;
Frame _frameOut;
vector<Rect> _faces;
Mat flippedColorFrame;

/*
Error codes -

0  = no error
-1 = opencv capture not open
-2 = original frame is empty
-3 = color flipped frame is empty
-4 = additional error code used on Unity/C# side

*/
void BuildEmptyFrameWithErrorCode(int error)
{
	_frameOut.x = -1;
	_frameOut.y = -1;
	_frameOut.bufferSize = -1;
	_frameOut.memPtr = 0;
	_frameOut.error = error;
}

/*
Functions below can be called from C#/Unity
*/
extern "C" int __declspec(dllexport) __stdcall  Init(int& outCameraWidth, int& outCameraHeight)
{
	// Load LBP face cascade.
	if (!_faceCascade.load("lbpcascade_frontalface.xml")) { return -1; }
		
	// Open the stream.
	_capture.open(0);
	if (!_capture.isOpened()) { return -2; }
	
	// Get output size
	outCameraWidth = _capture.get(CAP_PROP_FRAME_WIDTH);
	outCameraHeight = _capture.get(CAP_PROP_FRAME_HEIGHT);

	cv::useOptimized();

	return 0;
}

extern "C" void __declspec(dllexport) __stdcall  Close()
{
	_capture.release();
}

extern "C" void __declspec(dllexport) __stdcall SetScale(int scale)
{
	_scale = scale;
}

extern "C" void __declspec(dllexport) __stdcall FreeMemory()
{
	_frameOut.memPtr = nullptr;
}

extern "C" Frame __declspec(dllexport) __stdcall Detect(Circle* outFaces, int maxOutFacesCount, int& outDetectedFacesCount)
{	
	if (!_capture.isOpened())
	{
		BuildEmptyFrameWithErrorCode(-1);
		return _frameOut;
	}

	Mat frame;
	_capture >> frame;
	
	// you get an empty frame if you unplug webcam during demo
	if (frame.empty())
	{
		Close();
		BuildEmptyFrameWithErrorCode(-2);
		return _frameOut;
	}

	// Convert the frame to grayscale for cascade detection.
	Mat grayscaleFrame;
	cvtColor(frame, grayscaleFrame, COLOR_BGR2GRAY);
	Mat resizedGray;

	// Scale down for better performance.
	resize(grayscaleFrame, resizedGray, Size(frame.cols / _scale, frame.rows / _scale));
	equalizeHist(resizedGray, resizedGray);

	// Detect faces.
	_faceCascade.detectMultiScale(resizedGray, _faces);

	// Draw faces.
	for (size_t i = 0; i < _faces.size(); i++)
	{
		Point center(_scale * (_faces[i].x + _faces[i].width / 2), _scale * (_faces[i].y + _faces[i].height / 2));
		ellipse(frame, center, Size(_scale * _faces[i].width / 2, _scale * _faces[i].height / 2), 0, 0, 360, Scalar(0, 0, 255), 4, 8, 0);

		// Send to application.
		outFaces[i] = Circle(_faces[i].x, _faces[i].y, _faces[i].width / 2);
		outDetectedFacesCount++;

		if (outDetectedFacesCount == maxOutFacesCount)
			break;
	}

	// OpenCV frame is BGR color but Unity uses RGB
	flippedColorFrame; // failing to cache this variable causes inconsistent pointer value(s) at startup, and crashes in Unity builds
	cvtColor(frame, flippedColorFrame, COLOR_BGR2RGB);

	if (flippedColorFrame.empty()) 
	{
		BuildEmptyFrameWithErrorCode(-3);
		return _frameOut;
	}

	_frameOut.memPtr = flippedColorFrame.data;
	_frameOut.x = flippedColorFrame.cols;
	_frameOut.y = flippedColorFrame.rows;
	_frameOut.bufferSize = flippedColorFrame.rows * flippedColorFrame.cols * flippedColorFrame.channels();
	_frameOut.error = 0;
	return _frameOut;
}