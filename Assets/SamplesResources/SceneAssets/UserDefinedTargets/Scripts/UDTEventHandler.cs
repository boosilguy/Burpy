/*============================================================================== 
 Copyright (c) 2016-2017 PTC Inc. All Rights Reserved.
 
 Copyright (c) 2015 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vuforia;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UDTEventHandler : MonoBehaviour, IUserDefinedTargetEventHandler
{
	#region PRIVATEFLASH_MEMBERS
	//private bool Active;
	//private AndroidJavaObject camera1;
	private GameObject SearchButton;
    private enum ImageFilterMode : int
    {
        Nearest = 0,
        Biliner = 1,
        Average = 2
    }
    #endregion//Flash Member_choi

    #region PUBLIC_MEMBERS
    /// <summary>
    /// Can be set in the Unity inspector to reference an ImageTargetBehaviour 
    /// that is instantiated for augmentations of new User-Defined Targets.
    /// </summary>
    public ImageTargetBehaviour ImageTargetTemplate;
    public bool ISStartCoroutineServerThrows = false;
    public byte[] imageByte;//인식 이미지
    public int LastTargetIndex 
    {
        get { return (m_TargetCounter - 1) % MAX_TARGETS; }
    }
    #endregion PUBLIC_MEMBERS


    #region PRIVATE_MEMBERS
    const int MAX_TARGETS = 5;
    UserDefinedTargetBuildingBehaviour m_TargetBuildingBehaviour;
    QualityDialog m_QualityDialog;
    ObjectTracker m_ObjectTracker;
    TrackableSettings m_TrackableSettings;
    FrameQualityMeter m_FrameQualityMeter;

    // DataSet that newly defined targets are added to
    DataSet m_UDT_DataSet;

    // Currently observed frame quality
    ImageTargetBuilder.FrameQuality m_FrameQuality = ImageTargetBuilder.FrameQuality.FRAME_QUALITY_NONE;

    // Counter used to name newly created targets
    int m_TargetCounter;
	public int TargetCounterTemp;//counter save
    #endregion //PRIVATE_MEMBERS


    #region MONOBEHAVIOUR_METHODS
    void Start()
    {
        m_TargetBuildingBehaviour = GetComponent<UserDefinedTargetBuildingBehaviour>();

        if (m_TargetBuildingBehaviour)
        {
            m_TargetBuildingBehaviour.RegisterEventHandler(this);
            Debug.Log("Registering User Defined Target event handler.");
        }

        m_FrameQualityMeter = FindObjectOfType<FrameQualityMeter>();
        m_TrackableSettings = FindObjectOfType<TrackableSettings>();
        m_QualityDialog = FindObjectOfType<QualityDialog>();

        if (m_QualityDialog)
        {
            m_QualityDialog.GetComponent<CanvasGroup>().alpha = 0;
        }
    }
    #endregion //MONOBEHAVIOUR_METHODS


    #region IUserDefinedTargetEventHandler Implementation
    /// <summary>
    /// Called when UserDefinedTargetBuildingBehaviour has been initialized successfully
    /// </summary>
    public void OnInitialized()
    {
        m_ObjectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        if (m_ObjectTracker != null)
        {
            // Create a new dataset
            m_UDT_DataSet = m_ObjectTracker.CreateDataSet();
            m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);
        }
    }

    /// <summary>
    /// Updates the current frame quality
    /// </summary>
    public void OnFrameQualityChanged(ImageTargetBuilder.FrameQuality frameQuality)
    {
        Debug.Log("Frame quality changed: " + frameQuality.ToString());
        m_FrameQuality = frameQuality;
        if (m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_LOW)
        {
            Debug.Log("Low camera image quality");
        }

        m_FrameQualityMeter.SetQuality(frameQuality);
    }

    /// <summary>
    /// Takes a new trackable source and adds it to the dataset
    /// This gets called automatically as soon as you 'BuildNewTarget with UserDefinedTargetBuildingBehaviour
    /// </summary>
    public void OnNewTrackableSource(TrackableSource trackableSource)
    {
        m_TargetCounter++;

        // Deactivates the dataset first
        m_ObjectTracker.DeactivateDataSet(m_UDT_DataSet);

        // Destroy the oldest target if the dataset is full or the dataset 
        // already contains five user-defined targets.
        if (m_UDT_DataSet.HasReachedTrackableLimit() || m_UDT_DataSet.GetTrackables().Count() >= MAX_TARGETS)
        {
            IEnumerable<Trackable> trackables = m_UDT_DataSet.GetTrackables();
            Trackable oldest = null;
            foreach (Trackable trackable in trackables)
            {
                if (oldest == null || trackable.ID < oldest.ID)
                    oldest = trackable;
            }

            if (oldest != null)
            {
                Debug.Log("Destroying oldest trackable in UDT dataset: " + oldest.Name);
                m_UDT_DataSet.Destroy(oldest, true);
            }
        }

        // Get predefined trackable and instantiate it
        ImageTargetBehaviour imageTargetCopy = Instantiate(ImageTargetTemplate);
        imageTargetCopy.gameObject.name = "UserDefinedTarget-" + m_TargetCounter;

        // Add the duplicated trackable to the data set and activate it
        m_UDT_DataSet.CreateTrackable(trackableSource, imageTargetCopy.gameObject);

        // Activate the dataset again
        //m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);

        // Extended Tracking with user defined targets only works with the most recently defined target.
        // If tracking is enabled on previous target, it will not work on newly defined target.
        // Don't need to call this if you don't care about extended tracking.
        StopExtendedTracking();
        m_ObjectTracker.Stop();
        m_ObjectTracker.ResetExtendedTracking();
        m_ObjectTracker.Start();

        // Make sure TargetBuildingBehaviour keeps scanning...
        m_TargetBuildingBehaviour.StartScanning();
    }
    #endregion IUserDefinedTargetEventHandler implementation


    #region PUBLIC_METHODS
    /// <summary>
    /// Instantiates a new user-defined target and is also responsible for dispatching callback to 
    /// IUserDefinedTargetEventHandler::OnNewTrackableSource
    /// </summary>
    public void BuildNewTarget()
    {
        /*if (ui.ISDeleteTargetInformation == true)
        {
            m_ObjectTracker.DestroyAllDataSets(true);
            m_UDT_DataSet = m_ObjectTracker.CreateDataSet();
            m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);
            ui.ISDeleteTargetInformation = false;
        }*/
        if (m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_MEDIUM ||
            m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_HIGH)
		{
			StartCoroutine ("ButtonOut");
            // create the name of the next target.
            // the TrackableName of the original, linked ImageTargetBehaviour is extended with a continuous number to ensure unique names
            string targetName = string.Format("{0}-{1}", ImageTargetTemplate.TrackableName, m_TargetCounter);
			TargetCounterTemp = m_TargetCounter+1;
            // generate a new target:
            m_TargetBuildingBehaviour.BuildNewTarget(targetName, ImageTargetTemplate.GetSize().x);
        }
        else
        {
            Debug.Log("Cannot build new target, due to poor camera image quality");
            if (m_QualityDialog)
            {
                StopAllCoroutines();
                m_QualityDialog.GetComponent<CanvasGroup>().alpha = 1;
                StartCoroutine(FadeOutQualityDialog());
            }
        }
    }

    #endregion //PUBLIC_METHODS


    #region PRIVATE_METHODS

    IEnumerator FadeOutQualityDialog()
    {
        yield return new WaitForSeconds(1f);
        CanvasGroup canvasGroup = m_QualityDialog.GetComponent<CanvasGroup>();

        for (float f = 1f; f >= 0; f -= 0.1f)
        {
            f = (float)Math.Round(f, 1);
            Debug.Log("FadeOut: " + f);
            canvasGroup.alpha = (float)Math.Round(f, 1);
            yield return null;
        }
    }

    /// <summary>
    /// This method only demonstrates how to handle extended tracking feature when you have multiple targets in the scene
    /// So, this method could be removed otherwise
    /// </summary>
    void StopExtendedTracking()
    {
        // If Extended Tracking is enabled, we first disable it for all the trackables
        // and then enable it only for the newly created target
        bool extTrackingEnabled = m_TrackableSettings && m_TrackableSettings.IsExtendedTrackingEnabled();
        if (extTrackingEnabled)
        {
            StateManager stateManager = TrackerManager.Instance.GetStateManager();

            // 1. Stop extended tracking on all the trackables
            foreach (var tb in stateManager.GetTrackableBehaviours())
            {
                var itb = tb as ImageTargetBehaviour;
                if (itb != null)
                {
                    itb.ImageTarget.StopExtendedTracking();
                }
            }

            // 2. Start Extended Tracking on the most recently added target
            List<TrackableBehaviour> trackableList = stateManager.GetTrackableBehaviours().ToList();
            ImageTargetBehaviour lastItb = trackableList[LastTargetIndex] as ImageTargetBehaviour;
            if (lastItb != null)
            {
                if (lastItb.ImageTarget.StartExtendedTracking())
                    Debug.Log("Extended Tracking successfully enabled for " + lastItb.name);
            }
        }
    }

	IEnumerator ButtonOut(){
		UI ui = GameObject.Find("UIControl").GetComponent<UI>();
		SearchButton = GameObject.Find ("BuildButton");
        SearchButton.SetActive (false);
		ui.BuildPosition.SetActive (false);
		yield return new WaitForEndOfFrame ();
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);
        //tex.Resize(800, 1280);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, true);
        tex.Apply();
        tex = ResizeTexture(tex, 0);
        imageByte = tex.EncodeToPNG();
        DestroyImmediate(tex);
		if (ui.selectmode) {//????,???? ???????????? ???????? ???? 
			ISStartCoroutineServerThrows = true;
			ui.ISSearchButton = true;
		} 
		else 
		{
			ui.OnclickTextSearchButton (false);
		}
	}
    public void OnclickResearchButton()//재탐색 버튼
    {
        m_ObjectTracker.DestroyAllDataSets(true);
        m_UDT_DataSet = m_ObjectTracker.CreateDataSet();
        m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);
    }
	public void ActivateTracking ()
	{
		m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);
	}
    private static Texture2D ResizeTexture(Texture2D pSource, ImageFilterMode pFilterMode)
    {

        //*** Variables
        int i;

        //*** Get All the source pixels
        Color[] aSourceColor = pSource.GetPixels(0);
        Vector2 vSourceSize = new Vector2(pSource.width, pSource.height);

        //*** Calculate New Size
        float xWidth = Mathf.RoundToInt(320);
        float xHeight = Mathf.RoundToInt(480);

        //*** Make New
        Texture2D oNewTex = new Texture2D((int)xWidth, (int)xHeight, TextureFormat.RGBA32, false);

        //*** Make destination array
        int xLength = (int)xWidth * (int)xHeight;
        Color[] aColor = new Color[xLength];

        Vector2 vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);

        //*** Loop through destination pixels and process
        Vector2 vCenter = new Vector2();
        for (i = 0; i < xLength; i++)
        {

            //*** Figure out x&y
            float xX = (float)i % xWidth;
            float xY = Mathf.Floor((float)i / xWidth);

            //*** Calculate Center
            vCenter.x = (xX / xWidth) * vSourceSize.x;
            vCenter.y = (xY / xHeight) * vSourceSize.y;

            //*** Do Based on mode
            //*** Nearest neighbour (testing)
            if (pFilterMode == ImageFilterMode.Nearest)
            {

                //*** Nearest neighbour (testing)
                vCenter.x = Mathf.Round(vCenter.x);
                vCenter.y = Mathf.Round(vCenter.y);

                //*** Calculate source index
                int xSourceIndex = (int)((vCenter.y * vSourceSize.x) + vCenter.x);

                //*** Copy Pixel
                aColor[i] = aSourceColor[xSourceIndex];
            }

            //*** Bilinear
            else if (pFilterMode == ImageFilterMode.Biliner)
            {

                //*** Get Ratios
                float xRatioX = vCenter.x - Mathf.Floor(vCenter.x);
                float xRatioY = vCenter.y - Mathf.Floor(vCenter.y);

                //*** Get Pixel index's
                int xIndexTL = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
                int xIndexTR = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));
                int xIndexBL = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
                int xIndexBR = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));

                //*** Calculate Color
                aColor[i] = Color.Lerp(
                    Color.Lerp(aSourceColor[xIndexTL], aSourceColor[xIndexTR], xRatioX),
                    Color.Lerp(aSourceColor[xIndexBL], aSourceColor[xIndexBR], xRatioX),
                    xRatioY
                );
            }

            //*** Average
            else if (pFilterMode == ImageFilterMode.Average)
            {

                //*** Calculate grid around point
                int xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - (vPixelSize.x * 0.5f)), 0);
                int xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + (vPixelSize.x * 0.5f)), vSourceSize.x);
                int xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - (vPixelSize.y * 0.5f)), 0);
                int xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + (vPixelSize.y * 0.5f)), vSourceSize.y);

                //*** Loop and accumulate
                Color oColorTemp = new Color();
                float xGridCount = 0;
                for (int iy = xYFrom; iy < xYTo; iy++)
                {
                    for (int ix = xXFrom; ix < xXTo; ix++)
                    {

                        //*** Get Color
                        oColorTemp += aSourceColor[(int)(((float)iy * vSourceSize.x) + ix)];

                        //*** Sum
                        xGridCount++;
                    }
                }

                //*** Average Color
                aColor[i] = oColorTemp / (float)xGridCount;
            }
        }

        //*** Set Pixels
        oNewTex.SetPixels(aColor);
        oNewTex.Apply();

        //*** Return
        return oNewTex;
    }
    #endregion //PRIVATE_METHODS

    /*#region PUBLIC_FLASHMETHODS
	void FL_Start()
	{
		AndroidJavaClass cameraClass = new AndroidJavaClass("android.hardware.Camera");       
		WebCamDevice[] devices = WebCamTexture.devices;

		int camID = 0;
		camera1 = cameraClass.CallStatic<AndroidJavaObject>("open", camID);

		if (camera1 != null)
		{
			AndroidJavaObject cameraParameters = camera1.Call<AndroidJavaObject>("getParameters");
			cameraParameters.Call("setFlashMode", "torch");
			camera1.Call("setParameters", cameraParameters);
			camera1.Call("startPreview");
			Active = true;
		}
		else
		{
			Debug.LogError("[CameraParametersAndroid] Camera not available");
		}

	}

	void OnDestroy()
	{
		FL_Stop();
	}

	void FL_Stop()
	{

		if (camera1 != null)
		{
			camera1.Call("stopPreview");
			camera1.Call("release");
			Active = false;
		}
		else
		{
			Debug.LogError("[CameraParametersAndroid] Camera not available");
		}

	}

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(Screen.width * 0.1f, Screen.height * 0.1f, Screen.width * 0.3f, Screen.height * 0.1f));
		if (!Active)
		{
			if (GUILayout.Button("ENABLE FLASHLIGHT"))
				FL_Start();
		}
		else
		{
			if (GUILayout.Button("DISABLE FLASHLIGHT"))
				FL_Stop();
		}
		GUILayout.EndArea();
	}
	#endregion//Flash*/
}