using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UtilsModule;
using System;
using UnityEngine.UI;

public class Calibration : MonoBehaviour
{

    Mat downsampledRegisteredColorFrame;
    int downsampleSize = 2;
    int depthFrameWidth = 512;

    List<Mat> collectionOfCalibrationPatterns = new List<Mat>();
    List<MatOfPoint2f> generatedCalibrationPatternPoints = new List<MatOfPoint2f>();

    // Measurements
    float rmsLength;
    float meanLengthX;
    float meanLengthY;
    float stdDev;
    float stdDevX;
    float stdDevY;


    // Projector display parameters
    Mat undistortedProjectorFrame;
    Mat rectifiedProjectorFrame;
    Mat projectorFrame;


    // Calibration pattern variables
    Size calibrationPatternSize;
    public Size minOffset = new Size(1, 1); // width 0, height 1
    public Size maxOffset;
    public int chessboardSquareLengthInPixels;
    public int minChessboardSquareLengthInPixels = 40; // 체스보드 생성 시 픽셀 수, 환경에 따라 변경 가능
    public float chessboardSquareLengthInMeters;


    // Add Parametor
    public Size chessboardSize = new Size(8, 5); // 체스보드 코너의 숫자, 환경에 따라 변경 가능
    public Size projectorResolution = new Size(1024, 768); // 프로젝터 해상도를 1024 x 768, 1920 x 1080 사용 이후 다양한 해상도 적용 필요

    Size projectorAspectRatio = new Size(4, 3);

    // Calibration process vairables
    List<MatOfPoint2f> detectedCalibrationPatternPointsInCameraFrame = new List<MatOfPoint2f>();
    List<MatOfPoint2f> detectedCalibrationPatternPointsInProjectorFrame = new List<MatOfPoint2f>();



    // Calibration result variables
    public Mat homography = Mat.eye(3, 3, CvType.CV_64F);
    public Mat inverseHomography = Mat.eye(3, 3, CvType.CV_64F);


    // Calibration helper variables
    int maxCaptureAttempts = 5;
    int delay = 200;
    bool downsampleFrame, fastCheck, manualMode, showResults, showLogs;


    List<bool> worldPointsToBeConsidered = new List<bool>();



    public GameObject projectorObj;

    List<Mat> rgbaMat = new List<Mat>();


    // Guide line
    Texture2D guideLine;


    Point[] startPoints = {
        new Point(4, 4), new Point(4, 4),
        new Point(1020, 4), new Point(1020, 4),
        new Point(492, 384), new Point(512, 364),
        new Point(1020, 760), new Point(1020, 760),
        new Point(4, 760), new Point(4, 760)



    };
    Point[] endPoints = {
        new Point(4, 24), new Point(24, 4),
        new Point(1020, 24), new Point(1000, 4),
        new Point(532, 384),new Point(512, 404),
        new Point(1020, 740), new Point(1000, 760),
        new Point(4, 740), new Point(24, 760)

    };

    // Test
    int tmp = 0;
    public GameObject displayObj;

    public GameObject CenterMarker;


    /*
     public Size webCamResolution = new Size(1920, 1080);
     public Texture webCamTexture;

     */

    // WebCam

    public string webCamName = "HD Pro Webcam C920";
    Size webCamResolution = new Size(1920, 1080);
    Texture webCamTexture;

    public GameObject warpObj;

    public Texture renderTexture;
    bool check;


    double imgX, imgY;
    int count = 0;


    //WebCam
    public Renderer display;
    WebCamTexture camTexture;
    private int currentIndex = 1;

    // Start is called before the first frame update
    void Start()
    {
        // 멀티 디스플레이
        //Debug.Log("displays connected: " + Display.displays.Length);
        // Display.displays[0] is the primary, default display and is always ON.
        // Check if additional displays are available and activate each.
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
        if (Display.displays.Length > 2)
            Display.displays[2].Activate();

        CreateGuideLine();

        CalculateAndSetChessboardSquareEdgeLengthInpixels();

        ConnectWebCam();

        

    }

    private void ConnectWebCam()
    {
        // 웹캠 연결 확인
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log(i + ")" + devices[i].name + " connect");

            if (devices[i].name.Equals(webCamName))
            {
                currentIndex = i;
            }
        }

        if (camTexture != null)
        {
            display.material.mainTexture = null;
            camTexture.Stop();
            camTexture = null;
        }
        WebCamDevice device = WebCamTexture.devices[currentIndex];
        camTexture = new WebCamTexture(device.name);
        display.material.mainTexture = camTexture;
        camTexture.Play();

    }

    private void CreateGuideLine()
    {
        Renderer projectorRender = projectorObj.GetComponent<Renderer>();
        Texture projectorTex = projectorRender.material.GetTexture("_MainTex");

        Mat img = Mat.zeros(projectorTex.height, projectorTex.width, CvType.CV_8UC4);


        for (int s = 0; s < startPoints.Length; s++)
        {
            Imgproc.line(img, startPoints[s], endPoints[s], new Scalar(255, 0, 0, 255), 4, Imgproc.LINE_AA);

        }

        guideLine = new Texture2D(projectorTex.width, projectorTex.height, TextureFormat.RGBA32, false);

        Utils.matToTexture2D(img, guideLine);

        projectorObj.GetComponent<Renderer>().material.mainTexture = guideLine;
    }

    private void CalculateAndSetChessboardSquareEdgeLengthInpixels()
    {
        if (projectorAspectRatio == new Size(4, 3))
        {
            // 4:3 비율의 프로젝터인 경우
            // chessboardSquareLengthInPixels 은 256가 되며 scaleBy 루프를 통과해 64픽셀로 변환됨
            Debug.Log("Projector aspect ratio : 4:3");
            chessboardSquareLengthInPixels = (int)projectorResolution.width / 4;
        }
        else if (projectorAspectRatio == new Size(16, 9))
        {
            // 16:9 비율의 프로젝터인 경우
            Debug.Log("Projector aspect ratio : 16:9");
            chessboardSquareLengthInPixels = (int)projectorResolution.width / 16;
        }

        int scaleBy = (int)(chessboardSquareLengthInPixels / (float)minChessboardSquareLengthInPixels);
        while (scaleBy >= 2)
        {
            chessboardSquareLengthInPixels >>= 1;
            scaleBy = (int)(chessboardSquareLengthInPixels / (float)(float)minChessboardSquareLengthInPixels);
        }
        return;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 체커보드 패턴 이미지 생성
            StartCoroutine("CreateChessBoardPatternImages");

        }


        if (Input.GetKeyDown(KeyCode.W))
        {

            // 캘리브레이션
            StartCoroutine("CollectCalibrationPatternPointsFromProjector");

        }



        if (Input.GetKeyDown(KeyCode.E))
        {

            // warp
         
             Texture2D renderTexture2D = new Texture2D((int)webCamResolution.width, (int)webCamResolution.height, TextureFormat.RGBA32, false);

            Utils.textureToTexture2D(renderTexture, renderTexture2D);
            Mat renderMat = new Mat((int)webCamResolution.height, (int)webCamResolution.width, CvType.CV_8UC4);
            Utils.texture2DToMat(renderTexture2D, renderMat, false);

            Mat warpedImgMat = new Mat((int)projectorResolution.height, (int)projectorResolution.width, CvType.CV_8UC4);

            Imgproc.warpPerspective(renderMat, warpedImgMat, homography, new Size((int)projectorResolution.width, (int)projectorResolution.height));
            Texture2D texture2DWarp = new Texture2D((int)projectorResolution.width, (int)projectorResolution.height, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(warpedImgMat, texture2DWarp, false);
            Rendering(projectorObj, texture2DWarp);
            Rendering(warpObj, texture2DWarp);
            


            //check = true;

        }


        if (check)
        {
            // 왜곡
            /*
            Mat warpedImgMat = new Mat((int)projectorResolution.height, (int)projectorResolution.width, CvType.CV_8UC4);
            Imgproc.warpPerspective(collectionOfCalibrationPatterns[0], warpedImgMat, inverseHomography, new Size((int)projectorResolution.width, (int)projectorResolution.height));
            Texture2D texture2D = new Texture2D((int)projectorResolution.width, (int)projectorResolution.height, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(warpedImgMat, texture2D, false);
            Rendering(projectorObj, texture2D);
            */

            //


            /// 왜곡

            //renderTexture

            Texture2D renderTexture2D = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            Utils.textureToTexture2D(renderTexture, renderTexture2D);
            Mat renderMat = new Mat(1080, 1920, CvType.CV_8UC4);
            Utils.texture2DToMat(renderTexture2D, renderMat, false);



            Mat warpedImgMat = new Mat(1080, 1920, CvType.CV_8UC4);

            //Imgproc.warpPerspective(rgbaMat[0], warpedImgMat, homography, new Size(1024, 768));

            Imgproc.warpPerspective(renderMat, warpedImgMat, homography, new Size(1024, 768));
            Texture2D texture2DWarp = new Texture2D(1024, 768, TextureFormat.RGBA32, false);
            /*
            Debug.Log("->" + collectionOfCalibrationPatterns[0].width() + "/" + collectionOfCalibrationPatterns[0].height());
            Debug.Log("->" + warpedImgMat.width() + "/" + warpedImgMat.height());
            Debug.Log("->" + texture2DWarp.width + "/" + texture2DWarp.height);
            */
            Utils.matToTexture2D(warpedImgMat, texture2DWarp, false);
            Rendering(projectorObj, texture2DWarp);
            Rendering(warpObj, texture2DWarp);

        }





        if (Input.GetKeyDown(KeyCode.R))
        {
            homography = Mat.eye(3, 3, CvType.CV_64F);
            inverseHomography = Mat.eye(3, 3, CvType.CV_64F);

            imgX = 0;
            imgY = 0;
            count = 0;



            CreateGuideLine();
        }



        if (Input.GetKeyDown(KeyCode.Space))
        {


        }



        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log(rgbaMat.Count +"/" + tmp);


            if (tmp == -1)
            {
                tmp = 29;
            }

            Texture2D texture2D = new Texture2D((int)webCamResolution.width, (int)webCamResolution.height, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat[tmp], texture2D, false);
            Rendering(displayObj, texture2D);


            tmp = tmp -= 1;

            
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log(rgbaMat.Count + "/" + tmp);

            if (tmp == 30)
            {
                tmp = 0;
            }

            Texture2D texture2D = new Texture2D((int)webCamResolution.width, (int)webCamResolution.height, TextureFormat.RGBA32, false);

            print("$" + rgbaMat[tmp].width() + "/" + texture2D.width + "," + rgbaMat[tmp].height() + "/" + texture2D.height);

            Utils.matToTexture2D(rgbaMat[tmp], texture2D, false);
            Rendering(displayObj, texture2D);
                        

            tmp = tmp += 1;

            
        }



        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 캘리브레이션
            StartCoroutine("Test");
        }




    }



    IEnumerator CollectCalibrationPatternPointsFromProjector()
    {
        int countTotalFrames = 0;       // 총 프레임 수
        int key = -1;                   // 키
        bool firstAttempt = true;       // 첫번째 시도
        bool patternCaptured = false;   // 패턴 캡쳐
        //bool gotLatestData = false;     // 최신 데이터 얻기
        int currentAttempt = 0;     // 현재 시도

        for (int i = 0; i < collectionOfCalibrationPatterns.Count;)
        {
            Debug.Log("Frame count : " + countTotalFrames);
                       
            patternCaptured = GetCalibrationPatternPointsInCurrentFrame(rgbaMat[i], countTotalFrames);

            List<MatOfPoint2f> sourcePoints = new List<MatOfPoint2f>();
            List<MatOfPoint2f> destinationPoints = new List<MatOfPoint2f>();

            if (patternCaptured && detectedCalibrationPatternPointsInCameraFrame.Count > 0)
            {
                Debug.Log("# of points detected = " + detectedCalibrationPatternPointsInCameraFrame.Count + " x " + detectedCalibrationPatternPointsInCameraFrame[detectedCalibrationPatternPointsInCameraFrame.Count - 1].size() + ", # of points projected = " + generatedCalibrationPatternPoints[i].size());
                if (detectedCalibrationPatternPointsInCameraFrame[detectedCalibrationPatternPointsInCameraFrame.Count - 1].total() > 0 && generatedCalibrationPatternPoints[i].total() != 0 &&
                    detectedCalibrationPatternPointsInCameraFrame[detectedCalibrationPatternPointsInCameraFrame.Count - 1].size() == generatedCalibrationPatternPoints[i].size())
                {
                    // 카메라 프레임에서 검출된 보정 패턴 포인트를 sourcePoints에 insert 한다.  - begin이 height인지 width인지 확인 필요.
                    MatOfPoint2f sourcePoint = new MatOfPoint2f(detectedCalibrationPatternPointsInCameraFrame[detectedCalibrationPatternPointsInCameraFrame.Count - 1]);
                    sourcePoints.Insert(sourcePoints.Count, sourcePoint);
                    // 생성된 보정 패턴 포인트를 destinationPoints에 insert 한다. - begin이 height인지 width인지 확인 필요.
                    MatOfPoint2f destinationPoint = new MatOfPoint2f(generatedCalibrationPatternPoints[i]);
                    destinationPoints.Insert(destinationPoints.Count, destinationPoint);

                    // 메시지 출력
                    for (int j = 0; j < sourcePoints.Count; j++)
                    {
                        Debug.Log("#" + j + " (x, y) = " + sourcePoints[j].width() + ", " + sourcePoints[j].height() + " --> " + destinationPoints[j].width() + ", " + destinationPoints[j].height());
                    }

                    // insert된 sourcePoints, destinationPoints를 이용하여 호모그래피를 계산한다.(이때 RANSAC 알고리즘를 적용함)
                    homography = Calib3d.findHomography(sourcePoint, destinationPoint);
                    Core.invert(homography, inverseHomography);

                    Debug.Log("$ findHomography" + homography.dump());


                    // 220113 수정
                    List<String> sourcePointList = new List<String>();
                    List<String> destinationPointList = new List<String>();

                    for (int s = 0; s < sourcePoint.total(); s++)
                    {
                        sourcePointList.Add(sourcePoint.toArray()[s].ToString());
                        destinationPointList.Add(destinationPoint.toArray()[s].ToString());
                    }




                    //projectorFrame = new Mat(collectionOfCalibrationPatterns[i].height(), collectionOfCalibrationPatterns[i].width(), CvType.CV_8UC4);
                    projectorFrame = collectionOfCalibrationPatterns[i];


                    //ApplyInverseHomographyAndWarpImage(rgbaMat[i], projectorFrame);
                    ApplyHomographyAndWarpPoints(detectedCalibrationPatternPointsInCameraFrame, detectedCalibrationPatternPointsInProjectorFrame);

                    // 캡쳐된 실제 체커보드 코너 이미지 표시
                    int radius = 5, thickness = -1;
                    Mat realCorner = collectionOfCalibrationPatterns[i];

                    Point[] debugCorner = generatedCalibrationPatternPoints[i].toArray();


                    /*
                    for (int j = 0; j < detectedCalibrationPatternPointsInProjectorFrame[detectedCalibrationPatternPointsInProjectorFrame.Count - 1].total(); j++)
                    {
                        int c = (int)detectedCalibrationPatternPointsInProjectorFrame[detectedCalibrationPatternPointsInProjectorFrame.Count - 1].total() - 1;
                        //circle 를 사용해 검출된 코너를 표시함
                        Point[] cornerCenter = detectedCalibrationPatternPointsInProjectorFrame[detectedCalibrationPatternPointsInProjectorFrame.Count - 1].toArray();
                        Imgproc.circle(realCorner, cornerCenter[c - j], radius, new Scalar(255, 0, 255, 255), thickness, Imgproc.LINE_8, 0);

                    }

                    */

                    for (int j = 0; j < detectedCalibrationPatternPointsInProjectorFrame[detectedCalibrationPatternPointsInProjectorFrame.Count - 1].total(); j++)
                    {
                        //circle 를 사용해 검출된 코너를 표시함
                        Point[] cornerCenter = detectedCalibrationPatternPointsInProjectorFrame[detectedCalibrationPatternPointsInProjectorFrame.Count - 1].toArray();
                        Imgproc.circle(realCorner, cornerCenter[j], radius, new Scalar(255, 0, 255, 255), thickness, Imgproc.LINE_8, 0);

                    }
                                     

                    yield return new WaitForSeconds(0.001f);

                    // 화면에 표시
                    Texture2D texture2D = new Texture2D((int)projectorResolution.width, (int)projectorResolution.height, TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(realCorner, texture2D, false);

                    Rendering(projectorObj, texture2D);







                    if (showLogs)
                    {
                        Debug.Log("points generated (destination)            = " + destinationPoints.Count);
                        Debug.Log("points found (source)                     = " + sourcePoints.Count);
                    }

                }
            }

            chessboardSquareLengthInMeters = rmsLength;

            countTotalFrames++;

            if (patternCaptured)
            {
                worldPointsToBeConsidered.Add(true);
                i++;
                currentAttempt = 0;
            }
            else
                currentAttempt++;

            if (currentAttempt > maxCaptureAttempts)
            {
                worldPointsToBeConsidered.Add(false);
                i++;
                currentAttempt = 0;
            }


        }
        print("&&&&&" + (int)(imgX / count) + " / " + (int)(imgY / count));

        // 중앙점

        float centerX = (float)((imgX / count) - 960);
        float centerY = (float)((imgY / count) - 540);

        CenterMarker.transform.localPosition = new Vector3(centerX, centerY, 0);


    }



    private void ApplyInverseHomographyAndWarpImage(Mat in_source, Mat out_destination)
    {
        double denominator = 1;
        int newX = 0, newY = 0;


        for (int y = 0; y < out_destination.width(); y++)
        {
            for (int x = 0; x < out_destination.height(); x++)
            {
                denominator = (double)inverseHomography.get(2, 0)[0] * x + (double)inverseHomography.get(2, 1)[0] * y + (double)inverseHomography.get(2, 2)[0];
                newX = (int)(((double)inverseHomography.get(0, 0)[0] * x + (double)inverseHomography.get(0, 1)[0] * y + (double)inverseHomography.get(0, 2)[0]) / denominator);
                newY = (int)(((double)inverseHomography.get(1, 0)[0] * x + (double)inverseHomography.get(1, 1)[0] * y + (double)inverseHomography.get(1, 2)[0]) / denominator);
                
                if (newX >= 0 && newX < in_source.height() && newY >= 0 && newY < in_source.width())
                {
                    //out_destination.put(y, x, in_source.get(newY, newX)[0]);

                    //out_destination.get(y, x)[0] = in_source.get(newY, newX)[0];
                }
            }
        }


        /* 220105
        denominator = inverseHomography.get(2, 0)[0] * x + inverseHomography.get(2, 1)[0] * y + inverseHomography.get(2, 2)[0];
        newX = (int)((inverseHomography.get(0, 0)[0] * x + inverseHomography.get(0, 1)[0] * y + inverseHomography.get(0, 2)[0]) / denominator);
        newY = (int)((inverseHomography.get(1, 0)[0] * x + inverseHomography.get(1, 1)[0] * y + inverseHomography.get(1, 2)[0]) / denominator);
         */

        return;
    }



    // 카메라 프레임에서 검출된 보정 패턴 포인트를 프로젝트 프레임에서 검출된 보정 패턴으로 적용시킨다.
    private void ApplyHomographyAndWarpPoints(List<MatOfPoint2f> in_collectionOfSourcePoints, List<MatOfPoint2f> out_collectionOfDestinationPoints)
    {
        double x, y, denominator = 1.0, newX = 0, newY = 0;

        List<Point> latestSourcePoints = new List<Point>(in_collectionOfSourcePoints[in_collectionOfSourcePoints.Count - 1].toArray());
        List<Point> latestDestinationPoints = new List<Point>();

        for (int i = 0; i < latestSourcePoints.Count; i++)
        {
            x = latestSourcePoints[i].x;
            y = latestSourcePoints[i].y;
            denominator = ((double)homography.get(2, 0)[0] * x) + ((double)homography.get(2, 1)[0] * y) + (double)homography.get(2, 2)[0];
            newX = (((double)homography.get(0, 0)[0] * x) + ((double)homography.get(0, 1)[0] * y) + (double)homography.get(0, 2)[0]) / denominator;
            newY = (((double)homography.get(1, 0)[0] * x) + ((double)homography.get(1, 1)[0] * y) + (double)homography.get(1, 2)[0]) / denominator;

            Point newDestinationPoint = new Point((float)newX, (float)newY);
            latestDestinationPoints.Insert(0, newDestinationPoint);


            /* 220105
            denominator = (homography.get(2, 0)[0] * x) + (homography.get(2, 1)[0] * y) + homography.get(2, 2)[0];
            newX = ((homography.get(0, 0)[0] * x) + (homography.get(0, 1)[0] * y) + homography.get(0, 2)[0]) / denominator;
            newY = ((homography.get(1, 0)[0] * x) + (homography.get(1, 1)[0] * y) + homography.get(1, 2)[0]) / denominator;

            Point newDestinationPoint = new Point(newX, newY);
            latestDestinationPoints.Insert(0, newDestinationPoint);
             */

        }

        //Debug.Log("$$$$$$" + homography.dump());

        out_collectionOfDestinationPoints.Add(new MatOfPoint2f(latestDestinationPoints.ToArray()));

        Debug.Log("size of latest destination points collection = " + out_collectionOfDestinationPoints.Count + " x " + latestDestinationPoints.Count);

        return;
    }

    private bool GetCalibrationPatternPointsInCurrentFrame(Mat rgbaMat, int frameNumber)
    {
        MatOfPoint2f foundPoint = new MatOfPoint2f();
        Point[] foundPoints = new Point[] { };

        bool found = false; // if pattern is detected by opencv function
        Mat grayMat = new Mat();

        if (downsampleFrame)
        {
            Imgproc.resize(rgbaMat, downsampledRegisteredColorFrame, new Size(), 1 / (float)downsampleSize, 1 / (float)downsampleSize, Imgproc.INTER_AREA);
            Imgproc.cvtColor(downsampledRegisteredColorFrame, grayMat, Imgproc.COLOR_BGR2GRAY);
        }
        else
        {
            Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_BGR2GRAY);
        }

        try
        {
            if (fastCheck)
            {
                found = Calib3d.findChessboardCorners(grayMat, calibrationPatternSize, foundPoint, Calib3d.CALIB_CB_ADAPTIVE_THRESH | Calib3d.CALIB_CB_FAST_CHECK | Calib3d.CALIB_CB_NORMALIZE_IMAGE | Calib3d.CALIB_CB_FILTER_QUADS);
                foundPoints = foundPoint.toArray();

                if (foundPoints.Length > 0)
                {
                    found = Calib3d.find4QuadCornerSubpix(grayMat, new MatOfPoint2f(foundPoints[foundPoints.Length]), new Size(50, 50));
                    //foundPoints.Insert(foundPoints.Length, foundPoints[foundPoints.Length]); 나중에 수정
                }
            }
            else
            {
                found = Calib3d.findChessboardCorners(grayMat, calibrationPatternSize, foundPoint, Calib3d.CALIB_CB_ADAPTIVE_THRESH | Calib3d.CALIB_CB_NORMALIZE_IMAGE | Calib3d.CALIB_CB_FILTER_QUADS);
                foundPoints = foundPoint.toArray();
            }

            

            if (found)
            {
                int pointIndex = 0;
                float sumX = 0;
                float sumY = 0;
                float squareSumX = 0;
                float squareSumY = 0;

                // 이미지 중앙값 찾기
                foreach (var p in foundPoints)
                {
                    imgX = imgX + p.x;
                    imgY = imgY + p.y;
                    count = count + 1;

                    //print("&&&" + p);
                }

                //print("&&&---------------------------");
                ////////



                for (int i = 0; i < calibrationPatternSize.height; i++)
                {
                    for (int j = 0; j < calibrationPatternSize.width; j++)
                    {
                        float foundPointX = (float)foundPoints[pointIndex].x;
                        float foundPointY = (float)foundPoints[pointIndex].y;
                        if (downsampleFrame)
                        {
                            foundPointX = ((float)(foundPoints[pointIndex].x * (float)downsampleSize));
                            foundPointY = ((float)(foundPoints[pointIndex].y * (float)downsampleSize));
                        }

                        int foundPointX_int = (int)foundPoints[pointIndex].x;
                        int foundPointY_int = (int)foundPoints[pointIndex].y;
                        int depthSpaceIndexForCurrentFoundPoint = foundPointY_int * depthFrameWidth + foundPointX_int;
                        float differenceX = 0, differenceY = 0;

                        if (j != 0)
                        {
                            int previousFoundPointX_int = (int)foundPoints[pointIndex - 1].x;
                            int depthSpaceIndexForPreviousFoundPointInX = foundPointY_int * depthFrameWidth + previousFoundPointX_int;
                            sumX += differenceX;
                            squareSumX += differenceX * differenceX;
                        }

                        if (i != 0)
                        {
                            int previousFoundPointY_int = (int)foundPoints[pointIndex - (int)calibrationPatternSize.width].y;
                            int depthSpaceIndexForPreviousFoundPointInY = previousFoundPointY_int * depthFrameWidth + foundPointX_int;
                            sumY += differenceY;
                            squareSumY += differenceY * differenceY;
                        }


                        //showLogs = true;
                        showLogs = false;
                        if (showLogs)
                        {
                            Debug.Log(
                                "##" + pointIndex + " (" + j + ", " + i + ") : "
                                + " foundPointX = " + foundPointX_int
                                + " foundPointY = " + foundPointY_int
                                + " depthSpaceIndex = " + depthSpaceIndexForCurrentFoundPoint
                                + " difference X = " + differenceX
                                + " difference Y = " + differenceY
                                );
                        }
                        pointIndex++;
                    }
                }

                meanLengthX = sumX / (((float)calibrationPatternSize.width - 1) * (float)calibrationPatternSize.height);
                meanLengthY = sumY / ((float)calibrationPatternSize.width * ((float)calibrationPatternSize.height - 1));
                float expectationOfSquareOfDifferenceX = squareSumX / (((float)calibrationPatternSize.width - 1) * (float)calibrationPatternSize.height);
                float expectationOfSquareOfDifferenceY = squareSumY / ((float)calibrationPatternSize.width * ((float)calibrationPatternSize.height - 1));
                float squareMeanMeasureX = meanLengthX * meanLengthX;
                float squareMeanMeasureY = meanLengthY * meanLengthY;
                float varianceOfDifferenceX = expectationOfSquareOfDifferenceX - squareMeanMeasureX;
                float varianceOfDifferenceY = expectationOfSquareOfDifferenceY - squareMeanMeasureY;
                stdDevX = (float)Math.Sqrt(varianceOfDifferenceX);
                stdDevY = (float)Math.Sqrt(varianceOfDifferenceY);
                rmsLength = (float)Math.Sqrt(squareMeanMeasureX + squareMeanMeasureY) / (float)Math.Sqrt(2);
                stdDev = (float)Math.Sqrt(stdDevX * stdDevX + stdDevY * stdDevY) / (float)Math.Sqrt(2);


                ///////
                showResults = false;
                //////

                if (showResults)
                {
                    Calib3d.drawChessboardCorners(rgbaMat, calibrationPatternSize, foundPoint, found);
                    Debug.Log(
                        "sumX = " + sumX
                        + " sumY = " + sumY
                        + " squareSumX = " + squareSumX
                        + " squareSumY = " + squareSumY
                        + " # of points = " + foundPoints.Length
                        + " meanMeasureX = " + meanLengthX
                        + " meanMeasureY = " + meanLengthY
                        + " meanMeasure = " + rmsLength
                        + " standardDeviationX = " + stdDevX
                        + " standardDeviationY = " + stdDevY
                        + " standardDeviation = " + stdDev);
                }

                if (manualMode)
                {
                    Debug.Log("Pattern detected... press 'c' to capture or 'r' to reject, frame # " + frameNumber);

                    if (Input.GetKeyDown(KeyCode.R))
                        return false;
                    else if (Input.GetKeyDown(KeyCode.C))
                    {
                        detectedCalibrationPatternPointsInCameraFrame.Add(foundPoint);
                        return true;
                    }
                    return false;
                }
                else
                {
                    Debug.Log("Pattern detected ... standard deviation = " + stdDev + ", frame # " + frameNumber);
                    if (stdDev < 0.01)
                    {
                        detectedCalibrationPatternPointsInCameraFrame.Add(foundPoint);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.Log("Exception: " + e);

            return false;
        }
    }


    IEnumerator CreateChessBoardPatternImages()
    {

        // 체스보드와 포인트 초기화
        collectionOfCalibrationPatterns.Clear();
        generatedCalibrationPatternPoints.Clear();

        rgbaMat.Clear();

        calibrationPatternSize = chessboardSize;

        // 체커보드 색상 및 치수 설정
        Scalar colorBlack = new Scalar(0, 0, 0, 255);       // 블랙
        Scalar colorWhite = new Scalar(255, 255, 255, 255); // 화이트
        Scalar colorRed = new Scalar(255, 0, 0, 255);       // 레드
        Scalar colorBlue = new Scalar(0, 0, 255, 255);      // 블루

        Size maxOffset = new Size(projectorResolution.width / chessboardSquareLengthInPixels - chessboardSize.width - 1, projectorResolution.height / chessboardSquareLengthInPixels - chessboardSize.height - 1);

        Debug.Log("# Start - Create ChessBoard Pattern Images");
        Debug.Log("# Corner width : " + chessboardSize.width + ", Corner height : " + chessboardSize.height + ", Chessboard Square Pixels : " + chessboardSquareLengthInPixels);
        Debug.Log("# minOffset : " + (int)minOffset.height + ", minOffset : " + (int)minOffset.height + ", maxOffset : " + (int)maxOffset.width + ", maxOffset : " + (int)maxOffset.width);

        // 코너수에 따른 체스보드 크기 설정
        double chessboardWidth = (calibrationPatternSize.width + 1) * chessboardSquareLengthInPixels;   // (5+1) * 64 = 384
        double chessboardHeight = (calibrationPatternSize.height + 1) * chessboardSquareLengthInPixels; // (8*1) * 64 = 576

        int cornerCount = 1;


        //for (int yOffset = (int)minOffset.height; yOffset < (int)maxOffset.height; yOffset++)
        for (int yOffset = (int)maxOffset.height - 1; yOffset >= (int)minOffset.height; yOffset--)
        {
            for (int xOffset = (int)minOffset.width; xOffset < (int)maxOffset.width; xOffset++)
            {
                Mat searchPattern = new Mat((int)projectorResolution.height, (int)projectorResolution.width, CvType.CV_8UC4, colorWhite);
                Mat testPattern = new Mat((int)chessboardHeight, (int)chessboardWidth, CvType.CV_8UC4, colorWhite);

                bool storeInReverseOrder = false;
                if ((xOffset + yOffset) % 2 != 0)
                    storeInReverseOrder = true;

                List<Point> newCalibrationPatternPoints = new List<Point>();

                for (int y = yOffset, y_searchPattern = 0; y <= yOffset + calibrationPatternSize.height; y++, y_searchPattern += chessboardSquareLengthInPixels)
                {
                    for (int x = xOffset, x_searchPattern = 0; x <= xOffset + calibrationPatternSize.width; x++, x_searchPattern += chessboardSquareLengthInPixels)
                    {
                        int xPosition = x * chessboardSquareLengthInPixels;
                        int yPosition = y * chessboardSquareLengthInPixels;

                        if (x != xOffset && y != yOffset)
                        {
                            Point chessboardPoint = new Point(xPosition, yPosition);

                            if (!storeInReverseOrder)
                            {
                                newCalibrationPatternPoints.Add(chessboardPoint);
                            }
                            else
                            {
                                newCalibrationPatternPoints.Insert(0, chessboardPoint);
                            }
                        }

                        OpenCVForUnity.CoreModule.Rect chessboardBlock = new OpenCVForUnity.CoreModule.Rect(xPosition, yPosition, chessboardSquareLengthInPixels, chessboardSquareLengthInPixels);
                        OpenCVForUnity.CoreModule.Rect chessboardBlock_searchPattern = new OpenCVForUnity.CoreModule.Rect(x_searchPattern, y_searchPattern, chessboardSquareLengthInPixels, chessboardSquareLengthInPixels);
                        // 프로젝터 높이와 너비에 따라 생성된 searchPattern에 xOffset + yOffset이 짝수일 경우 검정, 홀수일 경우 흰색을 넣음

                        if ((x + y) % 2 == 0)
                        {
                            Imgproc.rectangle(searchPattern, chessboardBlock, colorBlack, -1, 8);
                            Imgproc.rectangle(testPattern, chessboardBlock_searchPattern, colorBlue, -1, 8);
                        }
                        else
                        {
                            Imgproc.rectangle(searchPattern, chessboardBlock, colorWhite, -1, 8);
                            Imgproc.rectangle(testPattern, chessboardBlock_searchPattern, colorRed, -1, 8);
                        }
                    }
                }

                collectionOfCalibrationPatterns.Add(searchPattern);
                generatedCalibrationPatternPoints.Add(new MatOfPoint2f(newCalibrationPatternPoints.ToArray()));

                Texture2D texture2D = new Texture2D((int)projectorResolution.width, (int)projectorResolution.height, TextureFormat.RGBA32, false);
                Utils.matToTexture2D(searchPattern, texture2D, false);


                // 화면에 표시
                Rendering(projectorObj, texture2D);

                yield return new WaitForSeconds(0.18f);

                // 체스보드 캡쳐
                /*
                Texture2D webCamTexture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                Utils.textureToTexture2D(webCamTexture, webCamTexture2D);
                 */


                webCamTexture = camTexture;
                
                Texture2D webCamTexture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                Utils.textureToTexture2D(webCamTexture, webCamTexture2D);

                webCamTexture2D = Resize(webCamTexture2D, (int)webCamResolution.width, (int)webCamResolution.height);


                //
                Mat capture = new Mat(webCamTexture2D.height, webCamTexture2D.width, CvType.CV_8UC4);
                Utils.texture2DToMat(webCamTexture2D, capture, false);
                rgbaMat.Add(capture);



            }
        }

        Debug.Log("# End - Create ChessBoard Pattern Images");
    }

    public static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Point;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new UnityEngine.Rect(0, 0, newWidth, newHeight), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

    private void Rendering(GameObject Obj, Texture2D texture2D)
    {
        Renderer rend = Obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.mainTexture = texture2D;
        }
        else
        {
            print("-Rendering Fail-");
        }
    }




    IEnumerator Test()
    {
        // 체스보드와 포인트 초기화
        collectionOfCalibrationPatterns.Clear();
        generatedCalibrationPatternPoints.Clear();

        calibrationPatternSize = chessboardSize;

        // 체커보드 색상 및 치수 설정
        Scalar colorBlack = new Scalar(0, 0, 0, 255);       // 블랙
        Scalar colorWhite = new Scalar(255, 255, 255, 255); // 화이트
        Scalar colorRed = new Scalar(255, 0, 0, 255);       // 레드
        Scalar colorBlue = new Scalar(0, 0, 255, 255);      // 블루

        Size maxOffset = new Size(projectorResolution.width / chessboardSquareLengthInPixels - chessboardSize.width - 1, projectorResolution.height / chessboardSquareLengthInPixels - chessboardSize.height - 1);

        Debug.Log("# Start - Create ChessBoard Pattern Images");
        Debug.Log("# Corner width : " + chessboardSize.width + ", Corner height : " + chessboardSize.height + ", Chessboard Square Pixels : " + chessboardSquareLengthInPixels);
        Debug.Log("# minOffset : " + (int)minOffset.height + ", minOffset : " + (int)minOffset.height + ", maxOffset : " + (int)maxOffset.width + ", maxOffset : " + (int)maxOffset.width);

        // 코너수에 따른 체스보드 크기 설정
        double chessboardWidth = (calibrationPatternSize.width + 1) * chessboardSquareLengthInPixels;   // (5+1) * 64 = 384
        double chessboardHeight = (calibrationPatternSize.height + 1) * chessboardSquareLengthInPixels; // (8*1) * 64 = 576

        int cornerCount = 1;


        /*
         

        for (int yOffset = (int)minOffset.height; yOffset < (int)maxOffset.height; yOffset++)
        {
            for (int xOffset = (int)minOffset.width; xOffset < (int)maxOffset.width; xOffset++)
            {
         
         */


        for (int yOffset = (int)maxOffset.height - 1; yOffset >= (int)minOffset.height; yOffset--)
        {
            for (int xOffset = (int)minOffset.width; xOffset < (int)maxOffset.width; xOffset++)
            {
                Debug.Log("#" + yOffset + "," + xOffset);

                Mat searchPattern = new Mat((int)projectorResolution.height, (int)projectorResolution.width, CvType.CV_8UC4, colorWhite);
                Mat testPattern = new Mat((int)chessboardHeight, (int)chessboardWidth, CvType.CV_8UC4, colorWhite);

                bool storeInReverseOrder = false;
                if ((xOffset + yOffset) % 2 != 0)
                    storeInReverseOrder = true;

                List<Point> newCalibrationPatternPoints = new List<Point>();

                for (int y = yOffset, y_searchPattern = 0; y <= yOffset + calibrationPatternSize.height; y++, y_searchPattern += chessboardSquareLengthInPixels)
                {
                    for (int x = xOffset, x_searchPattern = 0; x <= xOffset + calibrationPatternSize.width; x++, x_searchPattern += chessboardSquareLengthInPixels)
                    {
                        int xPosition = x * chessboardSquareLengthInPixels;
                        int yPosition = y * chessboardSquareLengthInPixels;

                        if (x != xOffset && y != yOffset)
                        {
                            Point chessboardPoint = new Point(xPosition, yPosition);

                            if (!storeInReverseOrder)
                            {
                                newCalibrationPatternPoints.Add(chessboardPoint);
                            }
                            else
                            {
                                newCalibrationPatternPoints.Insert(0, chessboardPoint);
                            }
                        }

                        OpenCVForUnity.CoreModule.Rect chessboardBlock = new OpenCVForUnity.CoreModule.Rect(xPosition, yPosition, chessboardSquareLengthInPixels, chessboardSquareLengthInPixels);
                        OpenCVForUnity.CoreModule.Rect chessboardBlock_searchPattern = new OpenCVForUnity.CoreModule.Rect(x_searchPattern, y_searchPattern, chessboardSquareLengthInPixels, chessboardSquareLengthInPixels);
                        // 프로젝터 높이와 너비에 따라 생성된 searchPattern에 xOffset + yOffset이 짝수일 경우 검정, 홀수일 경우 흰색을 넣음

                        if ((x + y) % 2 == 0)
                        {
                            Imgproc.rectangle(searchPattern, chessboardBlock, colorBlack, -1, 8);
                            Imgproc.rectangle(testPattern, chessboardBlock_searchPattern, colorBlue, -1, 8);
                        }
                        else
                        {
                            Imgproc.rectangle(searchPattern, chessboardBlock, colorWhite, -1, 8);
                            Imgproc.rectangle(testPattern, chessboardBlock_searchPattern, colorRed, -1, 8);
                        }
                    }
                }

                collectionOfCalibrationPatterns.Add(searchPattern);
                generatedCalibrationPatternPoints.Add(new MatOfPoint2f(newCalibrationPatternPoints.ToArray()));

                Texture2D texture2D = new Texture2D((int)projectorResolution.width, (int)projectorResolution.height, TextureFormat.RGBA32, false);
                Utils.matToTexture2D(searchPattern, texture2D, false);


                // 화면에 표시
                Rendering(projectorObj, texture2D);

                yield return new WaitForSeconds(0.001f);




                // 체스보드 캡쳐
                Texture2D webCamTexture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                Utils.textureToTexture2D(webCamTexture, webCamTexture2D);
                Mat capture = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                Utils.texture2DToMat(webCamTexture2D, capture, false);
                rgbaMat.Add(capture);


            }
        }

        Debug.Log("# End - Create ChessBoard Pattern Images");
    }
}
