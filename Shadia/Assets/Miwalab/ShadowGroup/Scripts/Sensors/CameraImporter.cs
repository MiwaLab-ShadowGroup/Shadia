﻿using UnityEngine;
using System.Collections;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using System;

public class CameraImporter : ASensorImporter
{

    public Mat mat;
    public VideoCapture video;
    Window check;
    public bool isShowImage;
    // Use this for initialization
    void Start()
    {
        video = VideoCapture.FromCamera(1);
        if (!video.IsOpened())
            throw new System.Exception("capture initialization failed");

        mat = new Mat();
        check = new Window("Check");
    }

    // Update is called once per frame
    void Update()
    {
        if (mat.Empty())
            return;
        video.Read(mat);
        
        if (this.isShowImage)
        {
            check.ShowImage(mat);
        }
    }

    public override Mat getCvMat()
    {
        return this.mat;
    }

    void OnApplicationQuit()
    {
        Window.DestroyAllWindows();
    }

    public override MatType getMatType()
    {
        return MatType.CV_8UC3;
    }

    public override void setUpUI()
    {

    }
}
