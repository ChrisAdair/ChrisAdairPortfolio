using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;


public struct GeomData : IEquatable<GeomData>, IFormattable
{
    public double Distance { get; set; }
    public double Ksi { get; set; }
    public double Eta { get; set; }
    public double Phi { get; set; }
    public int Idx { get; set; }

    public bool Equals(GeomData other)
    {
        return (Distance == other.Distance && Ksi == other.Ksi && Phi == other.Phi && Eta == other.Eta);
    }
    public override int GetHashCode()
    {

        int hashDistance = Distance.GetHashCode();
        int hashKsi = Ksi.GetHashCode();
        int hashEta = Eta.GetHashCode();
        int hashPhi = Phi.GetHashCode();

        return hashDistance ^ hashKsi ^ hashEta ^ hashPhi;
    }
    public string ToString(string format, IFormatProvider formatProvider)
    {
        return Distance.ToString(format, formatProvider);
    }
}
public struct QuaternionD: IFormattable
{
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
    public double w { get; set; }

    public QuaternionD(double x, double y, double z, double w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return "w: " + w + " x: "+x + " y: " + y + " z: " + z;
    }

    public static implicit operator QuaternionD(Quaternion qD)
    {
        return new QuaternionD(qD.x, qD.y, qD.z, qD.w);
    }
}

public static class GBEnergy
{

    /// <summary>
    /// Returns the grain boundary energy of a given misorientation
    /// </summary>
    /// <param name="P">Rotation matrix of the first grain in lab frame</param>
    /// <param name="Q">Rotation matrix of the second grain in the lab frame</param>
    /// <param name="N">The grain boundary normal in lab frame</param>
    /// <param name="AlCuParameter">A string representing the metal to simulate eg: "Ni" for Nickle</param>
    /// <param name="eRGB"></param>
    /// <returns></returns>
    public static double GB5DOF(Quaternion disorientation, Vector<double> N, string AlCuParameter, float eRGB = -1f)
    {
        double energy = 0;
        Matrix<double> P = Matrix<double>.Build.DenseIdentity(3, 3);
        Matrix<double> Q = Quat2Mat(disorientation);
        GetPandQMatrix(N, ref P, ref Q);
        List<GeomData> geom100 = DistancesToSet(P, Q, "100");
        List<GeomData> geom110 = DistancesToSet(P, Q, "110");
        List<GeomData> geom111 = DistancesToSet(P, Q, "111");

        List<double> parvec = new List<double>();
        if (eRGB == -1)
            parvec = MakeParVec(AlCuParameter);
        else
            parvec = MakeParVec(AlCuParameter, eRGB);
        energy = WeigtedMeanEnergy(geom100, geom110, geom111, parvec);
        return energy;
    }

    private static void GetPandQMatrix(Vector<double> N, ref Matrix<double> p, ref Matrix<double> q)
    {

        if (N[2] == 1)
        {
            p = Matrix<double>.Build.Dense(3, 3, new double[] { 0, 0, 1,
                                                                0, 1, 0,
                                                                -1, 0, 0 });
        }
        else
        {
            var sinb_p = N[2];
            var cosb_p = Math.Sqrt(1 - Math.Pow(N[2], 2));
            var sinc_p = N[1] / (-cosb_p);
            var cosc_p = N[0] / cosb_p;


            p = Matrix<double>.Build.DenseOfColumnMajor(3, 3, new double[] { cosb_p*cosc_p, sinc_p, -sinb_p*cosc_p,
                                                                -cosb_p*sinc_p, cosc_p, sinb_p*sinc_p,
                                                                sinb_p, 0, cosb_p});
        }
        
        q = p * q;
    }
    private static List<GeomData> DistancesToSet(Matrix<double> P, Matrix<double> Q, string whichAxes, float disMax = 0.999999f)
    {
        List<GeomData> output;

        Matrix<double> axes;
        Matrix<double> dirs;
        switch (whichAxes)
        {
            case "110":
                axes = Matrix<double>.Build.Dense(3, 6);
                axes.SetRow(0, new double[] { 1, 1, 1, 1, 0, 0 });
                axes.SetRow(1, new double[] { 1, -1, 0, 0, 1, 1 });
                axes.SetRow(2, new double[] { 0, 0, 1, -1, 1, -1 });

                axes /= Math.Sqrt(2);

                dirs = Matrix<double>.Build.Dense(3, 6);
                dirs.SetRow(0, new double[] { 0, 0, 0, 0, 1, 1 });
                dirs.SetRow(1, new double[] { 0, 0, 1, 1, 0, 0 });
                dirs.SetRow(2, new double[] { 1, 1, 0, 0, 0, 0 });

                break;
            case "111":

                axes = Matrix<double>.Build.Dense(3, 4);

                axes.SetRow(0, new double[] { 1, 1, -1, -1 });
                axes.SetRow(1, new double[] { 1, -1, 1, -1 });
                axes.SetRow(2, new double[] { 1, -1, -1, 1 });
                axes /= Math.Sqrt(3);

                dirs = Matrix<double>.Build.Dense(3, 4);

                dirs.SetRow(0, new double[] { 1, 1, 1, 1 });
                dirs.SetRow(1, new double[] { -1, 1, 1, -1 });
                dirs.SetRow(2, new double[] { 0, 0, 0, 0 });
                dirs /= Math.Sqrt(2);

                break;
            case "100":
                axes = Matrix<double>.Build.Dense(3, 3);

                axes.SetRow(0, new double[] { 1, 0, 0 });
                axes.SetRow(1, new double[] { 0, 1, 0 });
                axes.SetRow(2, new double[] { 0, 0, 1 });

                dirs = Matrix<double>.Build.Dense(3, 3);

                dirs.SetRow(0, new double[] { 0, 0, 1 });
                dirs.SetRow(1, new double[] { 1, 0, 0 });
                dirs.SetRow(2, new double[] { 0, 1, 0 });

                break;
            default:
                throw (new System.Exception("Undefined axis set"));
        }

        int naxes = axes.ColumnCount;
        double period = Mathf.PI * naxes / 6.0;

        Matrix<double> rotX90 = Matrix<double>.Build.Dense(3, 3, new double[] { 1, 0, 0, 0, 0, 1, 0, -1, 0 });
        Matrix<double> rotY90 = Matrix<double>.Build.Dense(3, 3, new double[] { 0, 0, -1, 0, 1, 0, 1, 0, 0 });
        Matrix<double> rotZ90 = Matrix<double>.Build.Dense(3, 3, new double[] { 0, 1, 0, -1, 0, 0, 0, 0, 1 });
        Matrix<double> rotZ90m = Matrix<double>.Build.Dense(3, 3, new double[] { 0, -1, 0, 1, 0, 0, 0, 0, 1 });

        List<Matrix<double>> V = new List<Matrix<double>>();

        V.Add(Q);
        V.Add(V[0] * rotX90);
        V.Add(V[1] * rotX90);
        V.Add(V[2] * rotX90);

        for (int j = 0; j < 12; j++)
        {
            V.Add( V[j] * rotY90);
        }

        for (int j = 0; j < 4; j++)
        {
            V.Add( V[j] * rotZ90);
        }
        for (int j = 0; j < 4; j++)
        {
            V.Add( V[j] * rotZ90m);
        }
        double[] distances = new double[24 * naxes];
        double[] phis = new double[24 * naxes];
        double[] ksis = new double[24 * naxes];
        double[] etas = new double[24 * naxes];

        int thisindex = 0;

        for (int i = 0; i < naxes; i++)
        {
            Vector<double> ax = axes.Column(i);
            Vector<double> dir = dirs.Column(i);
            Vector<double> dir2 = Vector<double>.Build.Dense(new double[] { ax[1] * dir[2] - ax[2] * dir[1], -(ax[0] * dir[2] - ax[2] * dir[0]), ax[0] * dir[1] - ax[1] * dir[0] });


            for (int j = 0; j < 24; j++)
            {
                Q = V[j];
                Matrix<double> R = Q.Transpose() * P;

                QuaternionD q = Mat2Quat(R);

                double normalize = Math.Sqrt(System.Math.Pow(q.x, 2) + System.Math.Pow(q.y, 2) + System.Math.Pow(q.z, 2));
                Vector<double> axi = Vector<double>.Build.Dense(new double[] { q.x / normalize, q.y / normalize, q.z / normalize });
                double psi = 2 * System.Math.Acos(q.w);

                double dotp = axi * ax;

                double dis = 2 * System.Math.Sqrt(System.Math.Abs(1 - dotp * dotp)) * System.Math.Sin(psi / 2);

                if (dis < disMax)
                {
                    double theta = 2 * System.Math.Atan(dotp * System.Math.Tan(psi / 2));

                    Vector<double> n1 = P.Row(0);
                    Vector<double> n2 = Q.Row(0);

                    Matrix<double> RA = Quat2Mat(new QuaternionD(ax[0] * Math.Sin(theta / 2.0), ax[1] * Math.Sin(theta / 2.0), ax[2] * Math.Sin(theta / 2.0), Math.Cos(theta / 2.0)));

                    Vector<double> m1 = n1 + RA.TransposeThisAndMultiply(n2);

                    if (m1.L2Norm() < 0.000001)
                    {
                        Debug.LogWarning("m1 is singular!!");
                    }

                    m1 = m1 / m1.L2Norm();
                    Vector<double> m2 = RA * m1;

                    //Don't have the case where the complex real part is calculated in case of parallel vectors
                    double phi = Math.Acos(Math.Abs(m1 * ax));
                    if (double.IsNaN(phi))
                        phi = 0;
                    double theta1;
                    double theta2;
                    if (Math.Abs(ax * m1) > 0.9999)
                    {
                        theta1 = -theta / 2.0;
                        theta2 = theta / 2.0;
                    }
                    else
                    {
                        theta1 = System.Math.Atan2(dir2 * m1, dir * m1);
                        theta2 = System.Math.Atan2(dir2 * m2, dir * m2);
                    }

                    theta2 -= System.Math.Round(theta2 / period) * period;
                    theta1 -= System.Math.Round(theta1 / period) * period;

                    if (System.Math.Abs(theta2 + period / 2.0) < 0.000001)
                        theta2 += period;
                    if (System.Math.Abs(theta1 + period / 2.0) < 0.000001)
                        theta1 += period;

                    double ksi = System.Math.Abs(theta2 - theta1);
                    double eta = System.Math.Abs(theta2 + theta1);

                    distances[thisindex] = dis;
                    ksis[thisindex] = ksi;
                    etas[thisindex] = eta;
                    phis[thisindex] = phi;

                    thisindex++;
                }
            }
        }

        output = new List<GeomData>(thisindex);

        for(int i = 0; i < thisindex; i++)
        {
            output.Add( new GeomData { Distance = distances[i], Ksi = ksis[i], Eta =etas[i], Phi=phis[i], Idx=i});
        }

        for (int i = 0; i < thisindex; i++)
        {
            output[i] = new GeomData
            {
                Distance = Math.Round(output[i].Distance, 7),
                Ksi = Math.Round(output[i].Ksi, 7),
                Eta = Math.Round(output[i].Eta, 7),
                Phi = Math.Round(output[i].Phi, 7),
                Idx = i
            };

        }
        //Sorts by ascending distances
        var sort = from data in output
                   where data.Idx < thisindex
                   orderby data.Distance ascending
                   select data;
        //Removes repeated values
        output = new List<GeomData>(sort.Distinct());

        return output;
    }

    //Helper data structure for DistancesToSet
    


    public static QuaternionD Mat2Quat(Matrix<double> m)
    {
        QuaternionD output = new QuaternionD();

        double t = (m[0, 0] + m[1, 1] + m[2, 2]);
        double e0 = Math.Sqrt(1 + t) / 2.0;
        if (t > -0.99999999999999)
        {
            output.x = (m[1, 2] - m[2, 1]) / (4 * e0);
            output.y = (m[2, 0] - m[0, 2]) / (4 * e0);
            output.z = (m[0, 1] - m[1, 0]) / (4 * e0);
        }
        else
        {
            e0 = 0;
            output.z = Math.Sqrt(-(m[0, 0] + m[1, 1]) / 2.0);
            if (Math.Abs(output.z) > 2.0e-8)
            {
                output.x = (m[0, 2] / (2 * output.z));
                output.y = (m[1, 2] / (2 * output.z));
            }
            else
            {
                output.x = Math.Sqrt((m[0, 0] + 1) / 2.0);
                if (output.x != 0)
                {
                    output.y = (m[1, 0] / (2 * output.x));
                    output.z = 0;
                }
                else
                {
                    output.x = 0;
                    output.y = 1;
                    output.z = 0;
                }
            }
        }
        output.w = e0;
        output.x = -output.x;
        output.y = -output.y;
        output.z = -output.z;
        return output;
    }
    public static Matrix<double> Quat2Mat(QuaternionD q)
    {
        Matrix<double> m = Matrix<double>.Build.Dense(3, 3);
        m[0, 0] = Math.Pow(q.w,2) + Math.Pow(q.x,2) - Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        m[0, 1] = 2 * (q.x * q.y - q.z * q.w);
        m[0, 2] = 2 * (q.x * q.z + q.y * q.w);
        m[1, 0] = 2 * (q.x * q.y + q.z * q.w);
        m[1, 1] = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) + Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        m[1, 2] = 2 * (q.y * q.z - q.x * q.w);
        m[2, 0] = 2 * (q.x * q.z - q.y * q.w);
        m[2, 1] = 2 * (q.y * q.z + q.x * q.w);
        m[2, 2] = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) - Math.Pow(q.y, 2) + Math.Pow(q.z, 2);

        m = m / (Math.Pow(q.w, 2) + Math.Pow(q.x, 2) + Math.Pow(q.y, 2) + Math.Pow(q.z, 2));
        return m;
    }

    private static List<double> par42AlDefault = new List<double>() { 0.405204179289160,0.738862004021890,0.351631012630026,2.40065811939667,1.34694439281655,0.352260396651516,
        0.602137375062785,1.58082498976078,0.596442399566661,1.30981422643602,3.21443408257354,0.893016409093743,0.835332505166333,0.933176738717594,0.896076948651935,
        0.775053293192055,0.391719619979054,0.782601780600192,0.678572601273508,1.14716256515278,0.529386201144101,0.909044736601838,0.664018011430602,0.597206897283586,
        0.200371750006251,0.826325891814124,0.111228512469435,0.664039563157148,0.241537262980083,0.736315075146365,0.514591177241156,1.73804335876546,3.04687038671309,
        1.48989831680317,0.664965104218438,0.495035051289975,0.495402996460658,0.468878130180681,0.836548944799803,0.619285521065571,0.844685390948170,1.02295427618256 };
    private static List<double> par42CuDefault = new List<double>() { 0.405204179289160,0.738862004021890,0.351631012630026,2.40065811939667,1.34694439281655,3.37892632736175,
        0.602137375062785,1.58082498976078,0.710489498577995,0.737834049784765,3.21443408257354,0.893016409093743,0.835332505166333,0.933176738717594,0.896076948651935,
        0.775053293192055,0.509781056492307,0.782601780600192,0.762160812499734,1.10473084066580,0.529386201144101,0.909044736601838,0.664018011430602,0.597206897283586,
        0.200371750006251,0.826325891814124,0.0226010533470218,0.664039563157148,0.297920289861751,0.666383447163744,0.514591177241156,1.73804335876546,2.69805148576400,
        1.95956771207484,0.948894352912787,0.495035051289975,0.301975031994664,0.574050577702240,0.836548944799803,0.619285521065571,0.844685390948170,0.0491040633104212};
    private static List<double> MakeParVec(string AlCuParameter, double eRGB = 1.0366943122742, List<double> par42Al = null, List<double> par42Cu = null)
    {
        List<double> parVec = new List<double>();
        par42Al = par42Al ?? par42AlDefault;
        par42Cu = par42Cu ?? par42CuDefault;
        double AlCuParameterN = 1;
        switch (AlCuParameter)
        {
            case "Ni":
                eRGB = 1.44532834613925;
                AlCuParameterN = 0.767911805073948;
                break;
            case "Al":
                eRGB = 0.547128733614891;
                AlCuParameterN = 0;
                break;
            case "Au":
                eRGB = 0.529912885175204;
                AlCuParameterN = 0.784289766313152;
                break;
            case "Cu":
                eRGB = 1.03669431227427;
                AlCuParameterN = 1;
                break;
            default:
                throw (new System.Exception("Undefined Element"));
        }

        for (int i = 0; i < par42Al.Count; i++)
        {
            par42Al[i] = par42Al[i] + AlCuParameterN * (par42Cu[i] - par42Al[i]);
        }

        parVec.Add(eRGB);
        parVec.AddRange(par42Al);
        //parVec.Add(AlCuParameterN);
        return parVec;
    }


    private static double WeigtedMeanEnergy(List<GeomData> geom100, List<GeomData> geom110, List<GeomData> geom111, List<double> pars)
    {
        double energy = 0;

        double eRGB = pars[0];
        double d0100 = pars[1];
        double d0110 = pars[2];
        double d0111 = pars[3];
        double weight100 = pars[4];
        double weight110 = pars[5];
        double weight111 = pars[6];

        double offset = 0.00001;

        var e100 = Set100(geom100, pars);
        var e110 = Set110(geom110, pars);
        var e111 = Set111(geom111, pars);

        var d100 = from data in geom100 let num = data.Distance select num;
        var d110 = from data in geom110 let num = data.Distance select num;
        var d111 = from data in geom111 let num = data.Distance select num;

        List<double> s100 = new List<double>(from data in d100
                                             let num =  Math.Sin(Math.PI/2.0*data/d0100)
                                             select num);
        var d100L = d100.ToList();
        for(int i = 0; i < s100.Count; i++)
        {
            if (d100L[i] > d0100)
                s100[i] = 1;
            if (d100L[i] < offset * d0100)
                s100[i] = offset * Math.PI / 2.0;
        }

        List<double> w100 = new List<double>(from data in s100
                                             let num = (1 / (data * (1 - 0.5 * Math.Log(data))) - 1) * weight100
                                             select num);

        List<double> s110 = new List<double>(from data
                                             in d110
                                             let num = Math.Sin(Math.PI / 2.0 * data / d0110)
                                             select num);
        var d110L = d110.ToList();
        for (int i = 0; i < s110.Count; i++)
        {
            if (d110L[i] > d0110)
                s110[i] = 1;
            if (d110L[i] < offset * d0110)
                s110[i] = offset * Math.PI / 2.0;
        }

        List<double> w110 = new List<double>(from data in s110
                                             let num = (1 / (data * (1 - 0.5 * Math.Log(data))) - 1) * weight110
                                             select num);



        List<double> s111 = new List<double>(from data
                                             in d111
                                             let num = Math.Sin(Math.PI / 2.0 * data / d0111)
                                             select num);
        var d111L = d111.ToList();
        for (int i = 0; i < s111.Count; i++)
        {
            if (d111L[i] > d0111)
                s111[i] = 1;
            if (d111L[i] < offset * d0111)
                s111[i] = offset * Math.PI / 2.0;
        }

        List<double> w111 = new List<double>(from data in s111
                                             let num = (1 / (data * (1 - 0.5 * Math.Log(data))) - 1) * weight111
                                             select num);

        double ew100 = 0;
        double ew110 = 0;
        double ew111 = 0;
        double sum100 = w100.Sum();
        double sum110 = w110.Sum();
        double sum111 = w111.Sum();
        for(int i = 0; i < w100.Count; i++)
        {
            ew100 += e100[i] * w100[i];
        }
        for(int i = 0; i < w110.Count; i++)
        {
            ew110 += e110[i] * w110[i];
        }
        for (int i = 0; i < w111.Count; i++)
        {
            ew111 += e111[i] * w111[i];
        }

        energy = eRGB * (ew100 + ew110 + ew111 + 1) / (sum100 + sum110 + sum111 + 1);
        return energy;
    }


    private static List<double> Set100(List<GeomData> geom100, List<double> pars)
    {
        List<double> en = new List<double>();
        var pwr1 = pars[7];
        var pwr2 = pars[8];

        var ksi = from ksiData in geom100 let num = ksiData.Ksi select num;
        var eta = from etaData in geom100 let num = etaData.Eta select num;
        var phi = from phiData in geom100 let num = phiData.Phi select num;

        var entwist = Twist100(ksi, pars);
        var entilt = Atgb100(eta, ksi, pars);

        List<double> x = new List<double>();
        foreach (double num in phi)
            x.Add(num / (Math.PI / 2.0));
        for(int i = 0; i < x.Count; i++)
        {
            en.Add(entwist[i] * Math.Pow(1 - x[i], pwr1) + entilt[i] * Math.Pow(x[i], pwr2));
        }
        return en;
    }

    private static List<double> Twist100(IEnumerable<double> ksi, List<double> pars)
    {
        double a = pars[9];
        double b = pars[9] * pars[10];

        double perio = System.Math.PI / 2;
        List<double> en = new List<double>();
        foreach(double num in ksi)
        {
            double tempNum = System.Math.Abs(num) % perio;
            if (tempNum > perio / 2)
                tempNum = perio - tempNum;
            double sin = System.Math.Sin(2 * tempNum);
            double xlogx = sin * System.Math.Log(sin);
            if (double.IsNaN(xlogx) || double.IsInfinity(xlogx))
                xlogx = 0;

           en.Add( a*sin-b*xlogx);
        }


        return en;
    }

    private static List<double> Atgb100(IEnumerable<double> eta, IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();
        double pwr = pars[11];

        double period = System.Math.PI / 2;

        var en1 = Stgb100(ksi, pars);
        var en2 = Stgb100(from data in ksi let num = (period - data) select num, pars);
        List<double> etaL = eta.ToList<double>();
        for(int i = 0; i < en1.Count; i++)
        {
            if (en1[i] >= en2[i])
                en.Add(en1[i] - (en1[i] - en2[i]) * Math.Pow((etaL[i] / period), pwr));
            else
                en.Add(en2[i] - (en2[i] - en1[i]) * Math.Pow((1 - etaL[i] / period), pwr));
        }


        return en;
    }

    private static List<double> Stgb100(IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();
        double en2 = pars[12];
        double en3 = pars[13];
        double en4 = pars[14];
        double en5 = pars[15];
        double en6 = pars[16];

        double th2 = pars[17];
        double th4 = pars[18];

        double th6 = 2 * System.Math.Acos(5.0 / System.Math.Sqrt(34.0));
        double a12, a23, a34, a45, a56, a67;
        a12 = a23= a34= a45= a56= a67 = 0.5;

        double en1 = 0;
        double en7 = 0;

        double th1 = 0;
        double th3 = System.Math.Acos(4.0 / 5.0);
        double th5 = System.Math.Acos(3.0 / 5.0);
        double th7 = System.Math.PI / 2.0;

        foreach(double data in ksi)
        {
            double temp = 0;

            if (data <= th2)
                temp = en1 + (en2 - en1) * Rsw(data, th1, th2, a12);
            else if (data < th3 && data >= th2)
                temp = en3 + (en2 - en3) * Rsw(data, th3, th2, a23);
            else if (data < th4 && data >= th3)
                temp = en3 + (en4 - en3) * Rsw(data, th3, th4, a34);
            else if (data < th5 && data >= th4)
                temp = en5 + (en4 - en5) * Rsw(data, th5, th4, a45);
            else if (data < th6 && data >= th5)
                temp = en6 + (en5 - en6) * Rsw(data, th6, th5, a56);
            else if (data <= th7 && data >= th6)
                temp = en7 + (en6 - en7) * Rsw(data, th7, th6, a67);

            en.Add(temp);
        }


        return en;
    }

    private static double Rsw(double theta, double theta1, double theta2, double a)
    {
        double en = 0;

        double dtheta = theta2 - theta1;
        theta = (theta - theta1) / (dtheta * Math.PI / 2.0);

        double sin = Math.Sin(theta);
        if (sin < 0.000001)
            return 0;
        double xlogx = sin * Math.Log(sin);
        en = sin - a * xlogx;


        return en;
    }

    private static List<double> Set110(List<GeomData> geom110, List<double> pars)
    {
        List<double> en = new List<double>();

        var pwr1 = pars[19];
        var pwr2 = pars[20];

        var ksi = from data in geom110 let num = data.Ksi select num;
        var eta = from data in geom110 let num = data.Eta select num;
        var phi = from data in geom110 let num = data.Phi select num;

        var entwist = Twists110(ksi, pars);
        var entilt = Atgbs110(eta, ksi, pars);

        List<double> x = new List<double>(from data in phi let num =  data / (Math.PI / 2.0) select num);

        for(int i = 0; i < x.Count; i++)
        {
            en.Add(entwist[i] * Math.Pow(1 - x[i], pwr1) + entilt[i] * Math.Pow(x[i], pwr2));
        }


        return en;
    }

    private static List<double> Atgbs110(IEnumerable<double> eta, IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();

        var a = pars[25];

        var period = Math.PI;

        var en1 = Stgbs110(ksi, pars);
        var en2 = Stgbs110(from data in ksi let num =  period - data select num, pars);

        List<double> etaL = eta.ToList<double>();
        for (int i = 0; i < en1.Count; i++)
        {
            if (en1[i] >= en2[i])
                en.Add(en2[i] + (en1[i] - en2[i]) * Rsw(etaL[i], Math.PI, 0, a));
            else
                en.Add(en1[i] + (en2[i] - en1[i]) * Rsw(etaL[i], 0, Math.PI, a));
        }

        return en;
    }

    private static List<double> Stgbs110(IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();

        var en2 = pars[26];
        var en3 = pars[27];
        var en4 = pars[28];
        var en5 = pars[29];
        var en6 = pars[30];

        var th2 = pars[31];
        var th4 = pars[32];
        var th6 = pars[33];

        double a12, a23, a34, a45, a56, a67;
        a12 = a23 = a34 = a45 = a56 = a67 = 0.5;

        double en1 = 0;
        double en7 = 0;

        double th1 = 0;
        double th3 = Math.Acos(1.0 / 3.0);
        double th5 = Math.Acos(-7.0 / 11.0);
        double th7 = Math.PI;

        var th = from data in ksi let num =  Math.PI - data select num;

        foreach(double theta in th)
        {
            if (theta < th2)
                en.Add(en1 + (en2 - en1) * Rsw(theta, th1, th2, a12));
            else if (theta < th3)
                en.Add(en3 + (en2 - en3) * Rsw(theta, th3, th2, a23));
            else if (theta < th4)
                en.Add(en3 + (en3 - en2) * Rsw(theta, th3, th4, a34));
            else if (theta < th5)
                en.Add(en5 + (en4 - en5) * Rsw(theta, th5, th4, a45));
            else if (theta < th6)
                en.Add(en5 + (en6 - en5) * Rsw(theta, th5, th6, a56));
            else if (theta <= th7)
                en.Add(en7 + (en6 - en7) * Rsw(theta, th7, th6, a67));
        }


        return en;
    }

    private static List<double> Twists110(IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();

        var th1 = pars[21];

        var en1 = pars[22];
        var en2 = pars[23];
        var en3 = pars[24];

        double a01, a12, a23;
        a01 = a12 = a23 = 0.5;

        double th2 = Math.Acos(1.0 / 3.0);
        double th3 = Math.PI / 2;

        double perio = Math.PI;

        List<double> temp = new List<double>();

        foreach(double th in ksi)
        {
            double tmp = Math.Abs(th) % perio;
            tmp = tmp >= 0 ? tmp : tmp + perio;
            if (tmp > perio / 2)
                tmp = perio - tmp;
            temp.Add(tmp);
        }
        foreach(double th in temp)
        {
            if (th <= th1)
                en.Add(en1 * Rsw(th, 0, th1, a01));
            else if (th <= th2)
                en.Add(en2 + (en1 - en2) * Rsw(th, th2, th1, a12));
            else
                en.Add(en3 + (en2 - en3) * Rsw(th, th3, th2, a23));
        }


        return en;
    }

    private static List<double> Set111(List<GeomData> geom111, List<double> pars)
    {
        List<double> en = new List<double>();

        var a = pars[34];
        var b = a - 1;

        var ksi = from data in geom111 let num = data.Ksi select num;
        var eta = from data in geom111 let num = data.Eta select num;
        var phi = from data in geom111 let num = data.Phi select num;

        var entwist = Twists111(ksi, pars);
        var entilt = Atgbs111(eta, ksi, pars);
        var x = new List<double>(from data in phi let num =  data / (Math.PI / 2.0) select num);

        for(int i = 0; i < x.Count; i++)
        {
            en.Add(entwist[i] + (entilt[i] - entwist[i]) * (a * x[i] - b * Math.Pow(x[i], 2)));
        }

        return en;
    }

    private static List<double> Twists111(IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();

        var thd = pars[36];
        var enm = pars[37];
        var en2 = pars[27];

        var a1 = pars[35];
        var a2 = a1;

        foreach(double th in ksi)
        {
            double temp = 0;
            if (th > Math.PI / 3.0)
                temp = 2.0 * Math.PI / 3.0 - th;
            else
                temp = th;

            if (temp <= thd)
                en.Add(enm * Rsw(temp, 0, thd, a1));
            else
                en.Add(en2 + (enm - en2) * Rsw(temp, Math.PI / 3.0, thd, a2));
        }

        return en;
    }

    private static List<double> Atgbs111(IEnumerable<double> eta, IEnumerable<double> ksi, List<double> pars)
    {
        List<double> en = new List<double>();

        var ksim = pars[38];

        var enmax = pars[39];
        var enmin = pars[40];
        var encnt = pars[41];

        double a1 = 0.5, a2 = 0.5;

        var etascale = pars[42];
        List<double> etaL = new List<double>(eta);
        List<double> ksiL = new List<double>(ksi);

        for(int i = 0; i < etaL.Count; i++)
        {
            if (ksiL[i] > Math.PI / 3.0)
                ksiL[i] = 2.0 * Math.PI / 3.0 - ksiL[i];
            if (etaL[i] > Math.PI / 3.0)
                etaL[i] = 2.0 * Math.PI / 3.0 - etaL[i];

            if (ksiL[i] <= ksim)
                en.Add(enmax * Rsw(ksiL[i], 0, ksim, a1));
            else
            {
                double chi = enmin + (encnt - enmin) * Rsw(etaL[i], 0, Math.PI / (2.0 * etascale), 0.5);
                en.Add(chi + (enmax - chi) * Rsw(ksiL[i], Math.PI / 3.0, ksim, a2));
            }
        }

        return en;
    }
}
