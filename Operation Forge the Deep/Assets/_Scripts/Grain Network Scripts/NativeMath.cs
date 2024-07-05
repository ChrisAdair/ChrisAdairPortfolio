using System.Collections;
using System.Numerics;
using System.Security;
using System.Runtime.InteropServices;
public enum Layout
{
    Row_Major = 101,
    Col_Major = 102,
}
public enum UpperLower : byte
{
    upper = (byte)'U',
    lower = (byte)'L',
}
internal sealed unsafe class NativeMath
{
    [SuppressUnmanagedCodeSecurity]
    [DllImport("mkl_lite", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int LAPACKE_dgesv(Layout matrix_layout, int n,
                          int nrhs, [In,Out] double[] a, int lda,
                          [In,Out] int[] ipiv, [In,Out] double[] b, int ldb);
    [SuppressUnmanagedCodeSecurity]
    [DllImport("mkl_lite", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, SetLastError = false)]
    internal static extern void cblas_dsymv(  Layout lay, UpperLower uplo,  int n,  double alpha, [In] double[] a,  int lda, [In] double[] x,  int incx,  double beta, [In,Out] double[] y,  int incy);
    [SuppressUnmanagedCodeSecurity]
    [DllImport("mkl_lite", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, SetLastError = false)]
    internal static extern double cblas_ddot( int n, [In,Out] double[] x, int incx, [In,Out] double[] y, int incy);
}
public sealed unsafe class MatrixNative
{
    public static int dgesv(Layout matrix_layout, ref int n, ref int nrhs, [In,Out] double[] a, ref int lda, [In, Out] int[] ipiv, [In, Out] double[] b, ref int ldb)
    {
        return NativeMath.LAPACKE_dgesv(matrix_layout, n, nrhs, a , lda, ipiv, b, ldb);
    }
    public static void cblas_dsymv( Layout Layout, UpperLower uplo,  int n,  double alpha, [In] double[] a,  int lda, [In] double[] x,  int incx,  double beta, [In, Out] double[] y,  int incy)
    {
        NativeMath.cblas_dsymv( Layout,  uplo,  n,  alpha, a,  lda, x,  incx,  beta, y,  incy);
    }
    public static double cblas_ddot( ref int n, [In,Out] double[] x,  ref int incx, [In, Out] double[] y, ref int incy)
    {
        return NativeMath.cblas_ddot( n, x, incx, y, incy);
    }
}
