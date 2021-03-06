﻿using UnityEngine;
using System.Collections;
using Windows.Kinect;
using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using Miwalab.ShadowGroup.Network;
using Miwalab.ShadowGroup.Data;
using Miwalab.ShadowGroup.Thread;

public class KinectImporter : ASensorImporter
{
    public enum LightSourceMode
    {
        Normal,
        CanMoveOne,
        CanMoveCircle,
    }

    private CameraSpacePoint _position;
    private LightSourceMode _lightMode;


    public DepthSourceManager _depthManager;
    public BodySourceManager _bodyManager;
    private KinectSensor m_sensor;
    private ushort[] m_depthData;
    public ushort[] m_SaveDepth;
    private CoordinateMapper m_mapper;
    private Mat m_mat;
    public MatType m_matType = MatType.CV_8UC3;
    private FrameDescription m_frameDescription;
    private DepthFrameSource m_depthFrameSource;
    private CameraSpacePoint[] m_cameraSpacePoints;
    private float m_left = -1;
    private float m_right = 1;
    private float m_top = 1;
    private float m_bottom = -1;
    private float m_front = 8;
    private float m_rear = 0.5f;
    private float m_ViewRange = 1;
    private float m_CircleCut = 25;
    private ReadData m_readdata;
    public GameObject ReadData;
    private bool IsArchive = false;

    private bool m_useBody = true;

    public bool IsDepthStream { get; private set; }

    public float m_kinectRotation_rx = 0;
    public float m_kinectRotation_ry = 0;
    public float m_ScreenR = 2.5f;
    public float m_LightR = 2.5f;

    /// <summary>
    /// 仮想光源の位置
    /// 変更することで画面を変更可能
    /// </summary>
    public CameraSpacePoint VirtualLightResource { set; get; }

    #region 3D
    public BodyImage3D BodyImage3D;
    public CameraMatAttacher CameraAttacher;
    #endregion

    #region Circle
    public Mat m_mat3DObjectsRendered;
    #endregion


    #region 送受信用

    public RemoteShadowImageManager RSIM;

    #endregion

    // Use this for initialization
    void Start()
    {
        if (this.RSIM == null) return;
        this.InitializeNetwork();
        this.InitializeField();
        m_sensor = KinectSensor.GetDefault();
        this.m_ImagerProcesserList = new System.Collections.Generic.List<Miwalab.ShadowGroup.ImageProcesser.AShadowImageProcesser>();
        this.m_AfterEffectList = new System.Collections.Generic.List<Miwalab.ShadowGroup.AfterEffect.AAfterEffect>();
        if (m_sensor != null)
        {
            Debug.Log("The Kinect ID : " + m_sensor.UniqueKinectId);
            m_mapper = this.m_sensor.CoordinateMapper;
            if (!m_sensor.IsOpen)
            {
                m_sensor.Open();
                m_frameDescription = m_sensor.DepthFrameSource.FrameDescription;
                m_depthFrameSource = m_sensor.DepthFrameSource;
                this.m_mat = new Mat(new Size(m_frameDescription.Width, m_frameDescription.Height), this.m_matType);
                this.m_mat3DObjectsRendered = m_mat.Clone(); 
            }
            m_cameraSpacePoints = new CameraSpacePoint[m_frameDescription.Width * m_frameDescription.Height];
        }
        m_readdata = ReadData.GetComponent<ReadData>();
        //this.Colorimagemat = new Mat(1080, 1920, MatType.CV_8UC3, colors);

    }

    private void InitializeField()
    {
        this.m_left = 10f;
        this.m_right = 10f;
        this.m_top = 10f;
        this.m_bottom = 10f;
        this.m_rear = 10f;
        this.m_front = 10f;

        this.IsDepthStream = false;
    }

    private void InitializeNetwork()
    {
        //m_remoteManager = new RemoteManager(this.RemoteEPSettings);
        //m_HumanCenterPositions = new HumanPoints();
        //m_networkHost = NetworkHost.GetInstance();
        //m_networkHost.AddClient(NetworkSettings.KinectImporter_PositionSendPort, clientName);
        //m_threadHost = ThreadHost.GetInstance();
        //m_threadHost.CreateNewThread(new ContinuouslyThread(SendMethod), clientName);
        //m_threadHost.ThreadStart(clientName);

    }

    /// <summary>
    /// 送信用関数
    /// </summary>
    private void SendMethod()
    {
        //if (m_IsUpdatedSendData == true)
        //{
        //    byte[] data = this.m_HumanCenterPositions.getData();
        //    if (data == null)
        //    {
        //        return;
        //    }
        //    m_networkHost.SendTo(clientName, data, m_remoteManager.RemoteEPs);
        //    this.m_IsUpdatedSendData = false;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (_depthManager == null)
        {
            return;
        }

        m_depthData = _depthManager.GetData();
        m_SaveDepth = m_depthData;
        //this.colordata = _colormanager.GetColorData();

        if (IsArchive)
        {
            if (m_readdata.IsRead)
            {
                Merge(m_readdata.ReadDepthData, m_depthData);
            }
        }
        m_mapper.MapDepthFrameToCameraSpace(m_depthData, m_cameraSpacePoints);

        switch (Miwalab.ShadowGroup.Core.ApplicationSettings.CurrentMode)
        {
            case Miwalab.ShadowGroup.Core.ShadowMediaMode.ShadowMedia3D:
                this.BodyImage3D.SetupVertices(m_cameraSpacePoints);
                this.CameraAttacher.Attach(ref m_mat);
                break;
            case Miwalab.ShadowGroup.Core.ShadowMediaMode.ShadowMedia2D:
                this.ConvertDepthToMat();
                break;
            case Miwalab.ShadowGroup.Core.ShadowMediaMode.CircleShadow:
                this.CameraAttacher.Attach(ref m_mat3DObjectsRendered);
                this.ConvertDepthToMat();
                this.m_mat += m_mat3DObjectsRendered;
                break;
            case Miwalab.ShadowGroup.Core.ShadowMediaMode.Particle3D:
                this.CameraAttacher.Attach(ref m_mat3DObjectsRendered);
                this.ConvertDepthToMat();
                break;
        }
        
        foreach (var imageProcesser in this.m_ImagerProcesserList)
        {
            if (this.m_useBody)
            {
                imageProcesser.SetBody(_bodyManager.GetData());
                imageProcesser.UpdateBodyIndexList();
            }
            imageProcesser.ImageProcess(ref this.m_mat, ref this.m_mat);

        }

        for (int i = 0; i < this.m_AfterEffectList.Count; ++i)
        {
            var afterEffect = this.m_AfterEffectList[i];
            afterEffect.ImageProcess(ref this.m_mat, ref this.m_mat);
        }

        if (Miwalab.ShadowGroup.Core.ApplicationSettings.CurrentMode == Miwalab.ShadowGroup.Core.ShadowMediaMode.Particle3D)
        {
            this.m_mat += m_mat3DObjectsRendered;
        }
    }

    private void ConvertDepthToMat()
    {
        switch (_lightMode)
        {
            case LightSourceMode.Normal:
                this.NormalConvertDepthToMat();
                break;
            case LightSourceMode.CanMoveOne:
                this.CanMoveOneLightModeConvertDepthToMat();
                break;
            case LightSourceMode.CanMoveCircle:
                this.CanMoveCircleLightModeConvertDepthToMat();
                break;
        }

    }

    private void CanMoveCircleLightModeConvertDepthToMat()
    {
        m_mat *= 0;
        unsafe
        {
            byte* data = (byte*)m_mat.Data;
            int length_X = this.m_frameDescription.Width;
            int length_Y = this.m_frameDescription.Height;
            int length_X_Half = this.m_frameDescription.Width / 2;
            int length_Y_Half = this.m_frameDescription.Height / 2;
            int length = m_cameraSpacePoints.Length * 3;
            float depth = 0;
            int k;
            CameraSpacePoint point;
            //CameraSpacePoint _point;
            //int _count;
            int depthPoint_X;
            int depthPoint_Y;

            Vector3 pos =new Vector3(0,0,0);
            Quaternion quat = Quaternion.Euler(m_kinectRotation_rx, m_kinectRotation_ry, 0);
            for (int y = 0; y < length_Y; ++y)
            {
                for (int x = 0; x < length_X; ++x)
                {
                    point = this.m_cameraSpacePoints[(y * length_X + x)];
                    depth = point.Z;
                    ///とりあえずカメラの位置で減算
                    point.decrease(ref this._position);

                    //if (point.X * point.X + point.Z * point.Z > m_CircleCut) { continue; }
                    if (point.Y < this.m_bottom) { continue; }
                    if (point.Y > this.m_top) { continue; }

                    ///拡大率の計算
                    if (point.X * point.X + point.Z * point.Z < m_CircleCut )
                    {
                        continue;
                    }

                    pos.x = point.X;
                    pos.y = point.Y;
                    pos.z = point.Z;

                    pos = quat * pos;

                    ///新規のXY位置を計算
                    depthPoint_X = (int)(length_X_Half- pos.x * length_X_Half /pos.z);
                    depthPoint_Y = (int)(length_Y_Half - pos.y * length_Y_Half * (m_LightR- m_ScreenR) / (m_ScreenR * (m_LightR- pos.z)));
                    if (depthPoint_X < 0 || depthPoint_X > length_X) continue;
                    if (depthPoint_Y < 0 || depthPoint_Y > length_Y) continue;
                    if(pos.z < 0)
                    {
                        continue;
                    }


                    k = (((int)depthPoint_Y) * length_X + (int)depthPoint_X) * 3;
                    if (k >= length || k < 0) continue;

                    data[k] = 255;
                    data[k + 1] = 255;
                    data[k + 2] = 255;
                }
            }
        }
    }

    private void CanMoveOneLightModeConvertDepthToMat()
    {
        m_mat *= 0;
        unsafe
        {
            byte* data = (byte*)m_mat.Data;
            int length_X = this.m_frameDescription.Width;
            int length_Y = this.m_frameDescription.Height;
            int length_X_Half = this.m_frameDescription.Width / 2;
            int length_Y_Half = this.m_frameDescription.Height / 2;
            int length = m_cameraSpacePoints.Length * 3;
            int k;
            CameraSpacePoint point;
            //CameraSpacePoint _point;
            //int _count;
            int depthPoint_X;
            int depthPoint_Y;

            Vector3 pos = new Vector3(0, 0, 0);
            Quaternion quat = Quaternion.Euler(m_kinectRotation_rx, m_kinectRotation_ry, 0);
            for (int y = 0; y < length_Y; ++y)
            {
                for (int x = 0; x < length_X; ++x)
                {
                    point = this.m_cameraSpacePoints[(y * length_X + x)];
                    ///とりあえずカメラの位置で減算
                    point.decrease(ref this._position);
                    

                    //if (point.X * point.X + point.Z * point.Z > m_CircleCut) { continue; }
                    if (point.Y < this.m_bottom) { continue; }
                    if (point.Y > this.m_top) { continue; }
                    ///拡大率の計算
                    if (point.X * point.X + point.Z * point.Z > m_CircleCut)
                    {
                        continue;
                    }
                    ///ViewRange変換
                    point.multiply(m_ViewRange);

                    pos.x = point.X;
                    pos.y = point.Y;
                    pos.z = point.Z;

                    pos = quat * pos;


                    ///新規のXY位置を計算
                    depthPoint_X = (int)(length_X_Half - pos.x * length_X_Half / pos.z / m_ViewRange);
                    depthPoint_Y = (int)(length_Y_Half - pos.y * length_Y_Half /  pos.z/ m_ViewRange);
                    if (depthPoint_X < 0 || depthPoint_X > length_X) continue;
                    if (depthPoint_Y < 0 || depthPoint_Y > length_Y) continue;
                    if (pos.z < 0)
                    {
                        continue;
                    }


                    k = (((int)depthPoint_Y) * length_X + (int)depthPoint_X) * 3;
                    if (k >= length || k < 0) continue;

                    data[k] = 255;
                    data[k + 1] = 255;
                    data[k + 2] = 255;
                }
            }
        }
    }

    private void NormalConvertDepthToMat()
    {
        unsafe
        {
            byte* data = (byte*)m_mat.Data;
            int length = this.m_depthData.Length * 3;
            //CameraSpacePoint _point;
            //int _count;
            for (int i = 0; i < length; i += 3)
            {
                CameraSpacePoint point = this.m_cameraSpacePoints[i / 3];
                if (point.X > m_left && point.X < m_right && point.Y > m_bottom && point.Y < m_top && point.Z > m_rear && point.Z < m_front)
                {
                    if (IsDepthStream)
                    {
                        byte s = (byte)(point.Z / 8f * 255);
                        data[i] = s;
                        data[i + 1] = s;
                        data[i + 2] = s;
                    }
                    else
                    {
                        data[i] = 255;
                        data[i + 1] = 255;
                        data[i + 2] = 255;
                    }
                }
                else
                {
                    data[i] = 0;
                    data[i + 1] = 0;
                    data[i + 2] = 0;
                }
            }
        }
    }

    unsafe private static void Merge(ushort[] from, ushort[] dest)
    {
        fixed (ushort* _data1 = &from[0])
        fixed (ushort* _dest = &dest[0])
        {
            for (int i = 0; i < dest.Length; i++)
            {
                if (from[i] < dest[i] && from[i] != 0)
                {
                    dest[i] = from[i];
                }
            }
        }
    }

    public override void setUpUI()
    {
        (ShadowMediaUIHost.GetUI("Kinect_x_min") as ParameterSlider).ValueChanged += KinectImporter_x_min_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_x_max") as ParameterSlider).ValueChanged += KinectImporter_x_max_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_y_min") as ParameterSlider).ValueChanged += KinectImporter_y_min_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_y_max") as ParameterSlider).ValueChanged += KinectImporter_y_max_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_z_min") as ParameterSlider).ValueChanged += KinectImporter_z_min_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_z_max") as ParameterSlider).ValueChanged += KinectImporter_z_max_ValueChanged;


        (ShadowMediaUIHost.GetUI("Kinect_pos_x") as ParameterSlider).ValueChanged += KinectImporter_pos_x_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_pos_y") as ParameterSlider).ValueChanged += KinectImporter_pos_y_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_pos_z") as ParameterSlider).ValueChanged += KinectImporter_pos_z_ValueChanged;

        (ShadowMediaUIHost.GetUI("Kinect_rot_x") as ParameterSlider).ValueChanged += KinectImporter_rot_x_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_rot_y") as ParameterSlider).ValueChanged += KinectImporter_rot_y_ValueChanged;

        (ShadowMediaUIHost.GetUI("Kinect_screen_r") as ParameterSlider).ValueChanged += Kinect_screen_r_ValueChanged;
        (ShadowMediaUIHost.GetUI("Kinect_light_r") as ParameterSlider).ValueChanged += Kinect_light_r_ValueChanged;


        (ShadowMediaUIHost.GetUI("Kinect_LightMode") as ParameterDropdown).ValueChanged += KinectImporter_LightModeChanged;
        (ShadowMediaUIHost.GetUI("Kinect_ViewRange") as ParameterSlider).ValueChanged += KinectImporter_ViewRangeChanged;
        (ShadowMediaUIHost.GetUI("Kinect_CircleCut") as ParameterSlider).ValueChanged += KinectImporter_CircleCutChanged;




        //(ShadowMediaUIHost.GetUI("Kinect_Cut_y") as ParameterSlider).ValueChanged += KinectImporter_Cut_y_ValueChanged;
        //(ShadowMediaUIHost.GetUI("Kinect_Cut_diff") as ParameterSlider).ValueChanged += KinectImporter_Cut_diff_ValueChanged;

        (ShadowMediaUIHost.GetUI("Archive") as ParameterCheckbox).ValueChanged += KinectImporter_ValueChanged;

        (ShadowMediaUIHost.GetUI("Kinect_Depth") as ParameterCheckbox).ValueChanged += KinectImporter_KinectDepth_ValueChanged;
        (ShadowMediaUIHost.GetUI("kinect_use_bone") as ParameterCheckbox).ValueChanged += KinectImporter_kinect_use_bone_ValueChanged;
        

        (ShadowMediaUIHost.GetUI("Kinect_x_min") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_x_max") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_y_min") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_y_max") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_z_min") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_z_max") as ParameterSlider).ValueUpdate();

        (ShadowMediaUIHost.GetUI("Kinect_pos_x") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_pos_y") as ParameterSlider).ValueUpdate();
        (ShadowMediaUIHost.GetUI("Kinect_pos_z") as ParameterSlider).ValueUpdate();

        //(ShadowMediaUIHost.GetUI("Kinect_Cut_y") as ParameterSlider).ValueUpdate();
        //(ShadowMediaUIHost.GetUI("Kinect_Cut_diff") as ParameterSlider).ValueUpdate();

        (ShadowMediaUIHost.GetUI("Archive") as ParameterCheckbox).ValueUpdate();

        (ShadowMediaUIHost.GetUI("Kinect_Depth") as ParameterCheckbox).ValueUpdate();


    }

    private void KinectImporter_kinect_use_bone_ValueChanged(object sender, EventArgs e)
    {
        m_useBody = (e as ParameterCheckbox.ChangedValue).Value;
    }

    private void Kinect_light_r_ValueChanged(object sender, EventArgs e)
    {
        m_LightR = (e as ParameterSlider.ChangedValue).Value;
    }

    private void Kinect_screen_r_ValueChanged(object sender, EventArgs e)
    {
        m_ScreenR = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_rot_y_ValueChanged(object sender, EventArgs e)
    {
        m_kinectRotation_ry = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_rot_x_ValueChanged(object sender, EventArgs e)
    {
        m_kinectRotation_rx = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_CircleCutChanged(object sender, EventArgs e)
    {
        m_CircleCut = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_ViewRangeChanged(object sender, EventArgs e)
    {
        m_ViewRange = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_LightModeChanged(object sender, EventArgs e)
    {
        _lightMode = (LightSourceMode)(e as ParameterDropdown.ChangedValue).Value;
    }

    private void KinectImporter_KinectDepth_ValueChanged(object sender, EventArgs e)
    {
        this.IsDepthStream = (e as ParameterCheckbox.ChangedValue).Value;
    }

    private void KinectImporter_ValueChanged(object sender, EventArgs e)
    {
        this.IsArchive = (e as ParameterCheckbox.ChangedValue).Value;
    }

    private void KinectImporter_pos_x_ValueChanged(object sender, EventArgs e)
    {
        this._position.X = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_pos_y_ValueChanged(object sender, EventArgs e)
    {
        this._position.Y = (e as ParameterSlider.ChangedValue).Value;
    }

    private void KinectImporter_pos_z_ValueChanged(object sender, EventArgs e)
    {
        this._position.Z = (e as ParameterSlider.ChangedValue).Value;
    }

    //private void KinectImporter_Cut_diff_ValueChanged(object sender, EventArgs e)
    //{
    //    this.m_gettingHeightDiff = (e as ParameterSlider.ChangedValue).Value;
    //}

    //private void KinectImporter_Cut_y_ValueChanged(object sender, EventArgs e)
    //{
    //    this.m_gettingPlaneHeight = (e as ParameterSlider.ChangedValue).Value;
    //}

    private void KinectImporter_x_min_ValueChanged(object sender, EventArgs e)
    {
        this.m_left = (e as ParameterSlider.ChangedValue).Value;
    }
    private void KinectImporter_x_max_ValueChanged(object sender, EventArgs e)
    {
        this.m_right = (e as ParameterSlider.ChangedValue).Value;
    }
    private void KinectImporter_y_min_ValueChanged(object sender, EventArgs e)
    {
        this.m_bottom = (e as ParameterSlider.ChangedValue).Value;
    }
    private void KinectImporter_y_max_ValueChanged(object sender, EventArgs e)
    {
        this.m_top = (e as ParameterSlider.ChangedValue).Value;
    }
    private void KinectImporter_z_min_ValueChanged(object sender, EventArgs e)
    {
        this.m_rear = (e as ParameterSlider.ChangedValue).Value;
    }
    private void KinectImporter_z_max_ValueChanged(object sender, EventArgs e)
    {
        this.m_front = (e as ParameterSlider.ChangedValue).Value;
    }

    public override Mat getCvMat()
    {
        return this.m_mat;
    }

    public override MatType getMatType()
    {
        return this.m_matType;
    }
}