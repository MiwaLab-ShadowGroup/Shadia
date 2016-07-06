﻿using Miwalab.ShadowGroup.ImageProcesser.Particle2D;
using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miwalab.ShadowGroup.ImageProcesser
{
    public class ShadowParticle2D : AShadowImageProcesser
    {
        public List<Particle2D.AParticle2D> m_particleList = new List<Particle2D.AParticle2D>();
        public ShadowParticle2D()
            : base()
        {
            for (int i = 0; i < 1000; ++i)
            {
                var particle = new CircleParticle();
                particle.Size = 5;
                particle.Color = new Scalar(255, 255, 255);
                particle.Position = new UnityEngine.Vector2(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
                this.m_particleList.Add(particle);
            }
        }


        public override ImageProcesserType getImageProcesserType()
        {
            return ImageProcesserType.Particle2D;
        }
        Mat m_dst;
        public override void ImageProcess(ref Mat src, ref Mat dst)
        {
                var size = src.Size();
            m_dst = new Mat(size,MatType.CV_8UC3,new Scalar(0,0,0));
            unsafe
            {
                byte* data = src.DataPointer;
                for (int i = 0; i < this.m_particleList.Count; ++i)
                {

                    this.m_particleList[i].AddForce(new UnityEngine.Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f)));
                    this.m_particleList[i].Update();
                    this.m_particleList[i].DeadCheck(size.Width, size.Height);
                    this.m_particleList[i].Revirth(size.Width, size.Height);
                    int index = ((int)this.m_particleList[i].Position.x + size.Width * (int)this.m_particleList[i].Position.y) * 3;
                    if(index > size.Height*size.Width*3 -1 || index < 0)
                    {
                        continue;
                    }
                    int _size =(int)( data[index] / 255f * 2 + 1);
                    this.m_particleList[i].Size = _size;

                    this.m_particleList[i].DrawShape(ref m_dst);
                }
            }
            m_dst.CopyTo(dst);
        }
        public override string ToString()
        {
            return ImageProcesserType.Normal.ToString();
        }
    }
}