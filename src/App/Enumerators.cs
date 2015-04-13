﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lenticulis_gui.src.App
{
    /// <summary>
    /// Type of transformation, that could be done to canvas object
    /// </summary>
    public enum TransformType
    {
        Translation,
        Rotate,
        Scale
    };

    /// <summary>
    /// Interpolation type
    /// For every interpolation, Y and T value should be from range 0 to 1
    /// </summary>
    public enum InterpolationType
    {
        Linear,         // y(t) = t
        Quadratic,      // y(t) = t^2
        Goniometric,    // y(t) = sin(t*pi/2)
        Cubic           // y(t) = 4*(t-1/2)^3 + 1/2
    };
}
