using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisorientationColoring : MonoBehaviour {

	public static Color DisorientationColor(Quaternion disorientation)
    {
        Vector3 axis = Vector3.zero;
        float angle = 0;

        disorientation.ToAngleAxis(out angle, out axis);

        Vector3 xyz = new Vector3(axis.x * Mathf.Tan(angle * Mathf.Deg2Rad / 2.0f), axis.y * Mathf.Tan(angle * Mathf.Deg2Rad/ 2.0f), axis.z * Mathf.Tan(angle*Mathf.Deg2Rad / 2.0f));
        Vector3 x1y1z1 = xyz;
        float theta = Mathf.Atan2(xyz.z, xyz.y);
        if (xyz.x>=1.0f/3.0f && Mathf.Tan(theta) >= (1 - 2.0f * xyz.x) / xyz.x)
        {
            x1y1z1 = new Vector3(x1y1z1.x, x1y1z1.x * (x1y1z1.y + x1y1z1.z) / (1 - x1y1z1.x), x1y1z1.x * x1y1z1.z * (x1y1z1.y + x1y1z1.z) / (x1y1z1.y * (1 - x1y1z1.x)));
        }

        if (float.IsNaN(x1y1z1.y))
            x1y1z1.y = 0;
        if (float.IsNaN(x1y1z1.z))
            x1y1z1.z = 0;
        Quaternion g = Quaternion.AngleAxis(3.0f * Mathf.PI / 8.0f *Mathf.Rad2Deg, new Vector3(1, 0, 0));
        Vector3 x2y2z2 = new Vector3(x1y1z1.x - Mathf.Tan(Mathf.PI / 8.0f), x1y1z1.y, x1y1z1.z);
        x2y2z2 = g * x2y2z2;

        Vector3 x3y3z3 = new Vector3(x2y2z2.x, x2y2z2.y * (1 + (x2y2z2.y / x2y2z2.z) * Mathf.Tan(Mathf.PI / 8.0f)), x2y2z2.z + x2y2z2.y * Mathf.Tan(Mathf.PI / 8.0f));

        if (float.IsNaN(x3y3z3.y))
            x3y3z3.y = 0;

        Vector3 x4y4z4 = new Vector3(x3y3z3.x, (x3y3z3.y * Mathf.Cos(Mathf.PI / 8.0f)) / Mathf.Tan(Mathf.PI / 8.0f), x3y3z3.z - x3y3z3.x / Mathf.Cos(Mathf.PI / 8.0f));

        float phi = Mathf.Atan2(-x4y4z4.x, x4y4z4.y);

        Vector3 x5y5z5 = new Vector3(x4y4z4.x * (Mathf.Sin(phi) + Mathf.Abs(Mathf.Cos(phi))), x4y4z4.y * (Mathf.Sin(phi) + Mathf.Abs(Mathf.Cos(phi))), x4y4z4.z);

        float phi1 = Mathf.Atan2(-x5y5z5.x, x5y5z5.y);

        Vector3 x6y6z6 = new Vector3(-Mathf.Sqrt(Mathf.Pow(x5y5z5.x, 2) + Mathf.Pow(x5y5z5.y, 2)) * Mathf.Sin(2 * phi1), Mathf.Sqrt(Mathf.Pow(x5y5z5.x, 2) + Mathf.Pow(x5y5z5.y, 2)) * Mathf.Cos(2 * phi1), x5y5z5.z);

        g = Quaternion.AngleAxis(Mathf.PI / 6.0f * Mathf.Rad2Deg, new Vector3(0, 0, 1));

        Vector3 hsv = new Vector3(x6y6z6.x / Mathf.Tan(Mathf.PI / 8.0f), x6y6z6.y / Mathf.Tan(Mathf.PI / 8.0f), x6y6z6.z * Mathf.Cos(Mathf.PI / 8.0f) / Mathf.Tan(Mathf.PI / 8.0f));

        hsv = g * hsv;

        //Hsvregcone section
        float rho = Mathf.Atan2(hsv.y, hsv.x);
        float maxrho = Mathf.PI * 2.0f;
        rho = (rho % maxrho);
        rho = rho >= 0 ? rho / maxrho : (rho + maxrho) / maxrho;
        float yFinal = Mathf.Sqrt(Mathf.Pow(hsv.x, 2) + Mathf.Pow(hsv.y, 2)) / (hsv.z);
        if (float.IsNaN(yFinal))
            yFinal = 0;
        Color rgb = Color.HSVToRGB(rho, yFinal, hsv.z);
        Vector3 rgbVec = new Vector3(1 - rgb.r, 1 - rgb.g, 1 - rgb.b);

        //Back to colormap function
        g = Quaternion.AngleAxis(Mathf.PI*Mathf.Rad2Deg, new Vector3(0, 1, 0));

        Vector3 hsvChange = g * rgbVec;
        hsvChange = new Vector3(hsvChange.y, hsvChange.z + 1, hsvChange.x + 1);

        

        return new Color(hsvChange.x, hsvChange.y, hsvChange.z, 1.0f);
    }

}
