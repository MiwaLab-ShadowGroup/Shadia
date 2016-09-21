﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Miwalab.ShadowGroup.Network
{
    /// <summary>
    /// Singleton
    /// </summary>
    public class NetworkHost
    {
        /// <summary>
        /// singleton
        /// </summary>
        private static NetworkHost m_actual;


        /// <summary>
        /// クライアントのリスト
        /// </summary>
        private Dictionary<string, Client> m_clientList;
        private List<int> m_portList;


        /// <summary>
        /// sinleton
        /// </summary>
        private NetworkHost()
        {
            m_clientList = new Dictionary<string, Client>();
            m_portList = new List<int>();
        }

        /// <summary>
        /// 初回呼び出し時に初期化
        /// それ以降は同一の実態を使用
        /// </summary>
        /// <returns></returns>
        public static NetworkHost GetInstance()
        {
            if (m_actual == null)
            {
                m_actual = new NetworkHost();
            }
            return m_actual;
        }

        public void AddClient(int port, string tag)
        {
            if (m_clientList.ContainsKey(tag))
            {
                Debug.Log("tag:" + tag +"は存在します．");
                return;
            }
            if (m_portList.Contains(port))
            {
                ++port;
                //再設定
                AddClient(port, tag);
            }
            Debug.Log("port番号:" + port + "が初期化されます.");
            try
            {
                Client client = new Client(port);
                //タグをつけて記憶
                m_clientList.Add(tag, client);
                m_portList.Add(port);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
            return;
        }

        public void AddClient(NetworkSettings.NetworkSetting setting)
        {
            this.AddClient(setting.PORT, setting.TAG);
        }

        /// <summary>
        /// Response などで用いる
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public void SendRemote(string tag, byte[] data)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                return;
            }
            this.m_clientList[tag].SendRemote(data);
        }

        public void SendRemote(NetworkSettings.NetworkSetting setting, byte[] data)
        {
            this.SendRemote(setting.TAG, data);
        }

        /// <summary>
        /// 送りたい先にデータをぶんなげる
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public void SendTo(string tag, byte[] data, IPEndPoint to)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                return;
            }
            this.m_clientList[tag].SendTo(data,to);
        }
        public void SendTo(NetworkSettings.NetworkSetting setting, byte[] data,IPEndPoint to)
        {
            this.SendTo(setting.TAG, data, to);
        }

        /// <summary>
        /// 複数の送信先にデータを投げる
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <param name="to"></param>
        public void SendTo(string tag, byte[] data, List<IPEndPoint> to)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                return;
            }
            this.m_clientList[tag].SendTo(data, to);
        }
        
        public void SendTo(NetworkSettings.NetworkSetting setting, byte[] data, List<IPEndPoint> to)
        {
            this.SendTo(setting.TAG, data, to);
        }

        public void RemoveClient(string tag)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                return;
            }
            this.m_clientList[tag].Close();
            this.m_clientList.Remove(tag);
        }
        public void RemoveClient(NetworkSettings.NetworkSetting setting)
        {
            this.RemoveClient(setting.TAG);
        }


        public Client GetClient(string tag)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                Debug.Log("No tag as" + tag);
                return null;
            }
            return this.m_clientList[tag];
        }
        public Client GetClient(NetworkSettings.NetworkSetting setting)
        {
            return this.GetClient(setting.TAG);
        }

        public byte[] Receive(string tag)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                Debug.Log("No tag as" + tag);
                return null;
            }
            return this.m_clientList[tag].Receive();
        }
        public byte[] Receive(NetworkSettings.NetworkSetting setting)
        {
            return Receive(setting.TAG);
        }

        public byte[] Receive(string tag, ref int available)
        {
            if (!this.m_clientList.ContainsKey(tag))
            {
                Debug.Log("No tag as" + tag);
                return null;
            }
            return this.m_clientList[tag].Receive(ref available);
        }
        public byte[] Receive(NetworkSettings.NetworkSetting setting, ref int available)
        {
            return Receive(setting.TAG, ref available);
        }

        public void Reset()
        {
            foreach(var p in this.m_clientList)
            {
                p.Value.Close();
            }
            this.m_clientList.Clear();
            this.m_portList.Clear();
        }
    }
}
