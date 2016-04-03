﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miwalab.ShadowGroup.ImageProcesser
{
    public enum ImageProcesserType
    {
        /// <summary>
        /// 通常影　自影
        /// </summary>
        Normal,
        /// <summary>
        /// CA
        /// </summary>
        CellAutomaton,
        /// <summary>
        /// ポリゴン
        /// </summary>
        Polygon,
        /// <summary>
        /// 二重残像
        /// </summary>
        DoubleAfterImage,
        /// <summary>
        /// 時間遅れ影
        /// </summary>
        TimeDelay,
        /// <summary>
        /// 個数を数えるためのもの．消したら処刑
        /// </summary>
        Count,
    }
}