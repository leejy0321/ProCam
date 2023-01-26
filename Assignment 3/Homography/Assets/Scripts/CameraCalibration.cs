using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgcodecsModule;
using System;
using OpenCVForUnity.ImgprocModule;
using Unity.VisualScripting;

public class CameraCalibration : MonoBehaviour
{
    Mat captureMatrix;
    Mat checkerMatrix;

    Mat homoMat1;
    Mat homoMat2;
    Mat homoMat3;

    Mat V;
    Mat B;
    Mat A;
    Mat K;

    Mat U, Sigma, Vt;
    Mat dstImage;

    public Camera cameraObject;
    public int resWidth = 1920;
    public int resHeight = 1080;

    Point[] checkerPoints = { new Point(1280, 960), new Point(1280, 640), new Point(1600, 640), new Point(1600, 960) };
    Point[] capturePoints1 = { new Point(1418, 672), new Point(1417, 637), new Point(1455, 634), new Point(1456, 669) };
    Point[] capturePoints2 = { new Point(1668, 714), new Point(1669, 673), new Point(1699, 687), new Point(1698, 731) };
    Point[] capturePoints3 = { new Point(1469, 825), new Point(1451, 807), new Point(1492, 805), new Point(1511, 823) };
    Point[] capturePoints = { new Point(551, 540), new Point(535, 447), new Point(639, 441), new Point(656, 539) };

    Point3[] checker3DPoints1 = { new Point3(0, 0, 0), new Point3(0.0001, 0.1665, 0), new Point3(0.1252, 0.1665, 0), new Point3(0.1252, -0.0003, 0) };
    Point3[] checker3DPoints2 = { new Point3(0, 0, 0), new Point3(0, 0.1662, 0), new Point3(0.1252, 0.1662, 0), new Point3(0.1252, -0.0003, 0) };
    Point3[] checker3DPoints3 = { new Point3(0, 0, 0), new Point3(0, 0.1668, 0), new Point3(0.1252, 0.1668, 0), new Point3(0.1252, -0.0002, 0) };
    Point3[] checker3DPoints = { new Point3(0, 0, 0), new Point3(0, 0.1665, 0), new Point3(0.1252, 0.1665, 0), new Point3(0.1252, 0, 0) };
    Point3[] checker3D = { new Point3(1280, 960, 0), new Point3(1280, 640, 0), new Point3(1600, 640, 0), new Point3(1600, 960, 0) };

    Point3[] projector3DPoints1 = { new Point3(25.66, 15.49, 0), new Point3(25.688, 22.244, 0), new Point3(33.42, 21.96, 0), new Point3(33.58, 14.81, 0) };
    Point3[] projector3DPoints2 = { new Point3(15.178, 11.539, 0), new Point3(15.187, 14.967, 0), new Point3(18.55, 14.958, 0), new Point3(18.6, 11.54, 0) };
    Point3[] projector3DPoints3 = { new Point3(9.06, 4.41, 0), new Point3(9.079, 6.742, 0), new Point3(12.17, 6.6, 0), new Point3(12.24, 4.027, 0) };

    MatOfPoint2f checkerPointMat;
    MatOfPoint2f capturePointMat1;
    MatOfPoint2f capturePointMat2;
    MatOfPoint2f capturePointMat3;

    void MatrixOperation()
    {
        // image 1
        Mat img1vi1 = Mat.zeros(1, 3, CvType.CV_64F);
        Mat img1vi2 = Mat.zeros(1, 3, CvType.CV_64F);
        img1vi1.put(0, 0, homoMat1.get(0, 0));
        img1vi1.put(0, 1, homoMat1.get(1, 0));
        img1vi1.put(0, 2, homoMat1.get(2, 0));
        img1vi2.put(0, 0, homoMat1.get(0, 1));
        img1vi2.put(0, 1, homoMat1.get(1, 1));
        img1vi2.put(0, 2, homoMat1.get(2, 1));
        Mat img1vj1 = Mat.zeros(3, 6, CvType.CV_64F);
        Mat img1vj2 = Mat.zeros(3, 6, CvType.CV_64F);
        img1vj1.put(0, 0, homoMat1.get(0, 0));
        img1vj1.put(0, 1, homoMat1.get(1, 0));
        img1vj1.put(0, 2, homoMat1.get(2, 0));
        img1vj1.put(1, 1, homoMat1.get(0, 0));
        img1vj1.put(1, 3, homoMat1.get(1, 0));
        img1vj1.put(1, 4, homoMat1.get(2, 0));
        img1vj1.put(2, 2, homoMat1.get(0, 0));
        img1vj1.put(2, 4, homoMat1.get(1, 0));
        img1vj1.put(2, 5, homoMat1.get(2, 0));
        img1vj2.put(0, 0, homoMat1.get(0, 1));
        img1vj2.put(0, 1, homoMat1.get(1, 1));
        img1vj2.put(0, 2, homoMat1.get(2, 1));
        img1vj2.put(1, 1, homoMat1.get(0, 1));
        img1vj2.put(1, 3, homoMat1.get(1, 1));
        img1vj2.put(1, 4, homoMat1.get(2, 1));
        img1vj2.put(2, 2, homoMat1.get(0, 1));
        img1vj2.put(2, 4, homoMat1.get(1, 1));
        img1vj2.put(2, 5, homoMat1.get(2, 1));
        Mat img1v11 = img1vi1.matMul(img1vj1);
        Mat img1v12 = img1vi1.matMul(img1vj2);
        Mat img1v22 = img1vi2.matMul(img1vj2);

        // image 2
        Mat img2vi1 = Mat.zeros(1, 3, CvType.CV_64F);
        Mat img2vi2 = Mat.zeros(1, 3, CvType.CV_64F);
        img2vi1.put(0, 0, homoMat2.get(0, 0));
        img2vi1.put(0, 1, homoMat2.get(1, 0));
        img2vi1.put(0, 2, homoMat2.get(2, 0));
        img2vi2.put(0, 0, homoMat2.get(0, 1));
        img2vi2.put(0, 1, homoMat2.get(1, 1));
        img2vi2.put(0, 2, homoMat2.get(2, 1));
        Mat img2vj1 = Mat.zeros(3, 6, CvType.CV_64F);
        Mat img2vj2 = Mat.zeros(3, 6, CvType.CV_64F);
        img2vj1.put(0, 0, homoMat2.get(0, 0));
        img2vj1.put(0, 1, homoMat2.get(1, 0));
        img2vj1.put(0, 2, homoMat2.get(2, 0));
        img2vj1.put(1, 1, homoMat2.get(0, 0));
        img2vj1.put(1, 3, homoMat2.get(1, 0));
        img2vj1.put(1, 4, homoMat2.get(2, 0));
        img2vj1.put(2, 2, homoMat2.get(0, 0));
        img2vj1.put(2, 4, homoMat2.get(1, 0));
        img2vj1.put(2, 5, homoMat2.get(2, 0));
        img2vj2.put(0, 0, homoMat2.get(0, 1));
        img2vj2.put(0, 1, homoMat2.get(1, 1));
        img2vj2.put(0, 2, homoMat2.get(2, 1));
        img2vj2.put(1, 1, homoMat2.get(0, 1));
        img2vj2.put(1, 3, homoMat2.get(1, 1));
        img2vj2.put(1, 4, homoMat2.get(2, 1));
        img2vj2.put(2, 2, homoMat2.get(0, 1));
        img2vj2.put(2, 4, homoMat2.get(1, 1));
        img2vj2.put(2, 5, homoMat2.get(2, 1));
        Mat img2v11 = img2vi1.matMul(img2vj1);
        Mat img2v12 = img2vi1.matMul(img2vj2);
        Mat img2v22 = img2vi2.matMul(img2vj2);

        // image 3
        Mat img3vi1 = Mat.zeros(1, 3, CvType.CV_64F);
        Mat img3vi2 = Mat.zeros(1, 3, CvType.CV_64F);
        img3vi1.put(0, 0, homoMat3.get(0, 0));
        img3vi1.put(0, 1, homoMat3.get(1, 0));
        img3vi1.put(0, 2, homoMat3.get(2, 0));
        img3vi2.put(0, 0, homoMat3.get(0, 1));
        img3vi2.put(0, 1, homoMat3.get(1, 1));
        img3vi2.put(0, 2, homoMat3.get(2, 1));
        Mat img3vj1 = Mat.zeros(3, 6, CvType.CV_64F);
        Mat img3vj2 = Mat.zeros(3, 6, CvType.CV_64F);
        img3vj1.put(0, 0, homoMat3.get(0, 0));
        img3vj1.put(0, 1, homoMat3.get(1, 0));
        img3vj1.put(0, 2, homoMat3.get(2, 0));
        img3vj1.put(1, 1, homoMat3.get(0, 0));
        img3vj1.put(1, 3, homoMat3.get(1, 0));
        img3vj1.put(1, 4, homoMat3.get(2, 0));
        img3vj1.put(2, 2, homoMat3.get(0, 0));
        img3vj1.put(2, 4, homoMat3.get(1, 0));
        img3vj1.put(2, 5, homoMat3.get(2, 0));
        img3vj2.put(0, 0, homoMat3.get(0, 1));
        img3vj2.put(0, 1, homoMat3.get(1, 1));
        img3vj2.put(0, 2, homoMat3.get(2, 1));
        img3vj2.put(1, 1, homoMat3.get(0, 1));
        img3vj2.put(1, 3, homoMat3.get(1, 1));
        img3vj2.put(1, 4, homoMat3.get(2, 1));
        img3vj2.put(2, 2, homoMat3.get(0, 1));
        img3vj2.put(2, 4, homoMat3.get(1, 1));
        img3vj2.put(2, 5, homoMat3.get(2, 1));
        Mat img3v11 = img3vi1.matMul(img3vj1);
        Mat img3v12 = img3vi1.matMul(img3vj2);
        Mat img3v22 = img3vi2.matMul(img3vj2);

        Mat img1vsub = img1v11 - img1v22;
        Mat img2vsub = img2v11 - img2v22;
        Mat img3vsub = img3v11 - img3v22;

        V = Mat.eye(6, 6, CvType.CV_64F);
        for (int i = 0; i < 6; i++)
        {
            V.put(0, i, img1v12.get(0, i));
            V.put(1, i, img1vsub.get(0, i));
            V.put(2, i, img2v12.get(0, i));
            V.put(3, i, img2vsub.get(0, i));
            V.put(4, i, img3v12.get(0, i));
            V.put(5, i, img3vsub.get(0, i));
        }
    }

    Mat Cholesky(Mat A)
    {
        Mat R;
        Size sizeA = A.size();
        int n = (int)Math.Sqrt(sizeA.area());
        R = Mat.zeros(sizeA, CvType.CV_64F);

        for (int k = 0; k < n; k++)
        {
            double tmp_val = Math.Sqrt(Math.Abs(A.get(k, k)[0])); // Abs
            //double tmp_val = Math.Sqrt(A.get(k, k)[0]);
            double[] val = { tmp_val };
            R.put(k, k, val);
            for (int i = k + 1; i < n; i++)
            {
                double tmp_val2 = A.get(k, i)[0] / R.get(k, k)[0];
                double[] val2 = { tmp_val2 };
                R.put(k, i, val2);
            }
            for (int j = k + 1; j < n; j++)
            {
                for (int l = j; l < n; l++)
                {
                    double tmp_val3 = A.get(j, l)[0] - R.get(k, j)[0] * R.get(k, l)[0];
                    double[] val3 = { tmp_val3 };
                    A.put(j, l, val3);
                }
            }
        }
        return R.t();
    }

    void Question2()
    {

        checkerPointMat = new MatOfPoint2f(checkerPoints);
        capturePointMat1 = new MatOfPoint2f(capturePoints1);
        capturePointMat2 = new MatOfPoint2f(capturePoints2);
        capturePointMat3 = new MatOfPoint2f(capturePoints3);

        homoMat1 = Calib3d.findHomography(checkerPointMat, capturePointMat1);
        homoMat2 = Calib3d.findHomography(checkerPointMat, capturePointMat2);
        homoMat3 = Calib3d.findHomography(checkerPointMat, capturePointMat3);

        MatrixOperation();

        //Sigma = Mat.eye(6, 6, CvType.CV_64F);
        U = new Mat();
        Sigma = new Mat();
        Vt = new Mat();
        Core.SVDecomp(V, Sigma, U, Vt, Core.SVD_FULL_UV);

        B = Mat.zeros(3, 3, CvType.CV_64F);
        B.put(0, 0, Sigma.get(0, 0));
        B.put(0, 1, Sigma.get(1, 0));
        B.put(0, 2, Sigma.get(2, 0));
        B.put(1, 0, Sigma.get(1, 0));
        B.put(1, 1, Sigma.get(3, 0));
        B.put(1, 2, Sigma.get(4, 0));
        B.put(2, 0, Sigma.get(2, 0));
        B.put(2, 1, Sigma.get(4, 0));
        B.put(2, 2, Sigma.get(5, 0));

        //Imgproc.warpPerspective(checkerMatrix, dstImage, homoMat3, new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/homoImage.png", dstImage);

        // B의 값
        // [0.05807825408518712, 0.02638392378415696, 0.008378798823006205;
        // 0.02638392378415696, 2.505546293677046e-05, 7.842437442305525e-06;
        // 0.008378798823006205, 7.842437442305525e-06, 2.740431220951377e-09]

        A = Cholesky(B);
        K = A.inv().t();

        // K의 값
        // [4.149475682593416, -4.1538196043775, -5.871317416200403;
        // -0, 9.14369645624628, 6.462287528799679;
        // 0, 0, 20.34842543463063]

        MatOfPoint3f checker3DPointsMat1 = new MatOfPoint3f(checker3DPoints1);
        MatOfPoint3f checker3DPointsMat2 = new MatOfPoint3f(checker3DPoints2);
        MatOfPoint3f checker3DPointsMat3 = new MatOfPoint3f(checker3DPoints3);
        MatOfPoint3f checker3DMat = new MatOfPoint3f(checker3D);

        List<Mat> objectPoints = new List<Mat>();
        //objectPoints.Add(checker3DPointsMat1);
        //objectPoints.Add(checker3DPointsMat2);
        //objectPoints.Add(checker3DPointsMat3);
        objectPoints.Add(checker3DMat);
        objectPoints.Add(checker3DMat);
        objectPoints.Add(checker3DMat);

        List<MatOfPoint3f> objectPoints3f = new List<MatOfPoint3f>();
        objectPoints3f.Add(checker3DPointsMat1);
        objectPoints3f.Add(checker3DPointsMat2);
        objectPoints3f.Add(checker3DPointsMat3);

        List<Mat> imagePoints = new List<Mat>();
        imagePoints.Add(capturePointMat1);
        imagePoints.Add(capturePointMat2);
        imagePoints.Add(capturePointMat3);

        List<MatOfPoint2f> imagePoints2f = new List<MatOfPoint2f>();
        imagePoints2f.Add(capturePointMat1);
        imagePoints2f.Add(capturePointMat2);
        imagePoints2f.Add(capturePointMat3);

        Mat calK = new Mat();
        Mat distC = new Mat();
        List<Mat> rvecs = new List<Mat>();
        List<Mat> tvecs = new List<Mat>();

        Calib3d.calibrateCamera(objectPoints, imagePoints, new Size(1920, 1080), calK, distC, rvecs, tvecs);
        Mat calKf = Calib3d.initCameraMatrix2D(objectPoints3f, imagePoints2f, new Size(1920, 1080));

        Mat rotationMat1 = new Mat();
        Mat rotationMat2 = new Mat();
        Mat rotationMat3 = new Mat();
        Calib3d.Rodrigues(rvecs[0], rotationMat1);
        Calib3d.Rodrigues(rvecs[1], rotationMat2);
        Calib3d.Rodrigues(rvecs[2], rotationMat3);
        rotationMat1.put(0, 2, tvecs[0].get(0, 0));
        rotationMat1.put(1, 2, tvecs[0].get(1, 0));
        rotationMat1.put(2, 2, tvecs[0].get(2, 0));
        rotationMat2.put(0, 2, tvecs[1].get(0, 0));
        rotationMat2.put(1, 2, tvecs[1].get(1, 0));
        rotationMat2.put(2, 2, tvecs[1].get(2, 0));
        rotationMat3.put(0, 2, tvecs[2].get(0, 0));
        rotationMat3.put(1, 2, tvecs[2].get(1, 0));
        rotationMat3.put(2, 2, tvecs[2].get(2, 0));
        Mat calH1 = calK.matMul(rotationMat1);
        Mat calH2 = calK.matMul(rotationMat2);
        Mat calH3 = calK.matMul(rotationMat3);
        Mat calImage = new Mat();
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH1, new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/calH1.png", calImage);
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH2, new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/calH2.png", calImage);
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH3, new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/calH3.png", calImage);
        //Imgproc.warpPerspective(captureMatrix, calImage, calH1.inv(), new Size(2560, 1920));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/calInvH1.png", calImage);
    }

    void Question3()
    {
        List<Mat> objectPoints = new List<Mat>();
        List<Mat> imagePoints = new List<Mat>();

        MatOfPoint3f projector3DPointsMat1 = new MatOfPoint3f(projector3DPoints1);
        MatOfPoint3f projector3DPointsMat2 = new MatOfPoint3f(projector3DPoints2);
        MatOfPoint3f projector3DPointsMat3 = new MatOfPoint3f(projector3DPoints3);

        MatOfPoint2f checkerPointsMat = new MatOfPoint2f(checkerPoints);

        objectPoints.Add(projector3DPointsMat1 * 30);
        objectPoints.Add(projector3DPointsMat2 * 30);
        objectPoints.Add(projector3DPointsMat3 * 30);

        imagePoints.Add(checkerPointsMat);
        imagePoints.Add(checkerPointsMat);
        imagePoints.Add(checkerPointsMat);

        Mat calK = new Mat();
        Mat distC = new Mat();
        List<Mat> rvecs = new List<Mat>();
        List<Mat> tvecs = new List<Mat>();

        Calib3d.calibrateCamera(objectPoints, imagePoints, new Size(2560, 1920), calK, distC, rvecs, tvecs);

        Mat rotationMat1 = new Mat();
        Mat rotationMat2 = new Mat();
        Mat rotationMat3 = new Mat();
        Calib3d.Rodrigues(rvecs[0], rotationMat1);
        Calib3d.Rodrigues(rvecs[1], rotationMat2);
        Calib3d.Rodrigues(rvecs[2], rotationMat3);
        rotationMat1.put(0, 2, tvecs[0].get(0, 0));
        rotationMat1.put(1, 2, tvecs[0].get(1, 0));
        rotationMat1.put(2, 2, tvecs[0].get(2, 0));
        rotationMat2.put(0, 2, tvecs[1].get(0, 0));
        rotationMat2.put(1, 2, tvecs[1].get(1, 0));
        rotationMat2.put(2, 2, tvecs[1].get(2, 0));
        rotationMat3.put(0, 2, tvecs[2].get(0, 0));
        rotationMat3.put(1, 2, tvecs[2].get(1, 0));
        rotationMat3.put(2, 2, tvecs[2].get(2, 0));
        Mat calH1 = calK.matMul(rotationMat1);
        Mat calH2 = calK.matMul(rotationMat2);
        Mat calH3 = calK.matMul(rotationMat3);
        Mat calImage = new Mat();
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH1.inv(), new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/proCalH1.png", calImage);
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH2.inv(), new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/proCalH2.png", calImage);
        //Imgproc.warpPerspective(checkerMatrix, calImage, calH3.inv(), new Size(1920, 1080));
        //Imgcodecs.imwrite(Application.dataPath + "/Outputs/proCalH3.png", calImage);
    }

    void Question4()
    {
        //checkerPointMat = new MatOfPoint2f(checkerPoints);
        //MatOfPoint2f capturePointMat = new MatOfPoint2f(capturePoints);
        //Mat homoMat = Calib3d.findHomography(capturePointMat, checkerPointMat);

        //Mat dstImage2 = new Mat();

        //Imgproc.warpPerspective(captureMatrix, dstImage2, homoMat, new Size(2560, 1920));
        //Imgcodecs.imwrite(Application.dataPath + "/homoImage2.png", dstImage2);

        Point[] checkerBigPoints = { new Point(320, 1600), new Point(320, 320), new Point(2240, 320), new Point(2240, 1600) };
        Point[] captureBigPoints = { new Point(330, 696), new Point(283, 388), new Point(875, 311), new Point(969, 777) };
        MatOfPoint2f checkerBigPointMat = new MatOfPoint2f(checkerBigPoints);
        MatOfPoint2f captureBigPointMat = new MatOfPoint2f(captureBigPoints);
        Mat homoMatBig = Calib3d.findHomography(captureBigPointMat, checkerBigPointMat);
        
        //Mat dstImage3 = new Mat();
        //Imgproc.warpPerspective(captureMatrix, dstImage3, homoMatBig, new Size(2560, 1920));
        //Imgcodecs.imwrite(Application.dataPath + "/homoImage3.png", dstImage3);

        //Mat captureRectMat = Imgcodecs.imread(Application.dataPath + "/CameraCapture2.png");
        //Mat dstImage4 = new Mat();
        //Imgproc.warpPerspective(captureRectMat, dstImage4, homoMatBig, new Size(2560, 1920));
        //Imgcodecs.imwrite(Application.dataPath + "/WarpedImage.png", dstImage4);

        //Mat dstImage5 = new Mat();
        //Imgproc.warpPerspective(checkerMatrix, dstImage5, homoMatBig, new Size(5120, 3840));
        //Imgcodecs.imwrite(Application.dataPath + "/WarpedOriginalImage.png", dstImage5);

        Point[] screenBigPoints = { new Point(1.336271, 28.2375), new Point(2.191269, 4.868491), new Point(51.33, 29.02), new Point(54.33, -4.83) };
        for (int i = 0; i < screenBigPoints.Length; i++)
        {
            screenBigPoints[i] *= 50;
        }
        MatOfPoint2f screenBigPointMat = new MatOfPoint2f(screenBigPoints);
        Mat camScreenH = Calib3d.findHomography(captureBigPointMat, screenBigPointMat);
        Mat proScreenH = Calib3d.findHomography(checkerBigPointMat, screenBigPointMat);
        Mat correspondenceH = camScreenH.matMul(proScreenH.inv());

        Mat captureRectMat = Imgcodecs.imread(Application.dataPath + "/CameraCapture2.png");
        Mat dstImage6 = new Mat();
        Imgproc.warpPerspective(captureRectMat, dstImage6, correspondenceH, new Size(1920, 1080));
        Imgcodecs.imwrite(Application.dataPath + "/Outputs/WarpedImage.png", dstImage6);

        Mat dstImage7 = new Mat();
        Imgproc.warpPerspective(checkerMatrix, dstImage7, correspondenceH, new Size(1920, 1080));
        Imgcodecs.imwrite(Application.dataPath + "/Outputs/WarpedOriginalImage.png", dstImage7);
    }

    void Start()
    {
        captureMatrix = Imgcodecs.imread(Application.dataPath + "/CameraCapture.png");
        checkerMatrix = Imgcodecs.imread(Application.dataPath + "/CheckerBoard.png");

        //Question2();
        //Question3();
        //Question4();
    }

    void cameraCapture()
    {
        RenderTexture renderTexture = new RenderTexture(resWidth, resHeight, 32);
        cameraObject.targetTexture = renderTexture;
        Texture2D capturedImage = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
        UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, capturedImage.width, capturedImage.height);
        cameraObject.Render();
        RenderTexture.active = renderTexture;
        capturedImage.ReadPixels(new UnityEngine.Rect(0, 0, resWidth, resHeight), 0, 0);
        capturedImage.Apply();

        byte[] bytes = capturedImage.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/CameraCapture2.png", bytes);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            cameraCapture();
        
    }
}