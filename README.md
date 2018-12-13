# Unity_OpenCV_Example
Demo project demonstrating a DLL interop interface for accessing OpenCV C++ in Unity/C#

##Overview:

This is an example project OpenCV C++ (V4.0.0) compiled, included, and accessed as a dynamic library in Unity.

OpenCV_For_Unity is the VS project for compiling the DLL, which is then included in the Unity project.

mixcast-vision-unity is a Unity project that demonstrates usage of the DLL for facial recognition

This project implements this tutorial series by Thomas Mountainborn, and extends it to demonstrate passing a byte array pointer in order to output the OpenCV feed in a Unity scene:

<http://thomasmountainborn.com/2016/09/11/unity-and-opencv-part-one-install/>

<http://thomasmountainborn.com/2016/09/12/unity-and-opencv-part-two-project-setup/>

<http://thomasmountainborn.com/2017/03/05/unity-and-opencv-part-three-passing-detection-data-to-unity/>


It also provide handling for unplugging and plugging back in devices (hotswapping).

##Notes:
-currently the DLL must be built as Release, and the Unity project will only build for x64
-the lbpcascade_frontface.xml file must be included in the build folder for the demo build to work

##To-do:
-there is currently a delay (freeze of the Unity scene) when hotswapping. This should be fixed by multithreading the code in Update()

