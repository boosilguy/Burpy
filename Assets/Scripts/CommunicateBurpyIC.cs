using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using GoogleDrive;

using IC = ImageClassification; // 이미지 Prediction namespace
using IR = ImageRegistration;   // 이미지 Registration namespace
using ST = SearchText;          // 연관어 검색


public class CommunicateBurpyIC : BurpyMonoBehaviour {
	[Header("Burpy's URI")]
	public string djangoURI = "https://burpyic.herokuapp.com/ic/";
    public string nodeURI = "https://burpyapp.herokuapp.com/api/predict";
	[Space(10)]
    [Header("Local Directory Info")]
	public string dirPath;
	public string imagePath;
	public string imageName;
    [Space(10)]
    [Header("GameObject Info")]
    public int[] productCode = { 0,0,0 };
	public string[] productcategory = new string[3];
	public string[] productname = new string[3];
	public string[] productscore = new string[3];
	public int[] SearchTemp = new int[5];
    public GameObject SearchResultRankUI;
    public GameObject otherResultPanel;
	public ButtonsOfOtherResults buttonsOfOtherResults;
	public GameObject InformMessage;//연관된 이름이 없을때 발생
    private GameObject SearchingImage;
    private GameObject SearchingVolume;
    private IEnumerator coroutine;
    private List<byte[]> itemImgs;

    void Awake(){
        // URL 변경 방지
        nodeURI = "https://burpyapp.herokuapp.com/api/predict";
        djangoURI = "https://burpyic.herokuapp.com/ic/";
        
		dirPath = Application.dataPath + "/Image/";
		imageName = "learning_image.jpg";
		imagePath = Path.Combine(dirPath, imageName);
    }

    void Update()
    {
        UDTEventHandler udteventHandler = GameObject.Find("UserDefinedTargetBuilder").GetComponent<UDTEventHandler>();

        if (udteventHandler.ISStartCoroutineServerThrows)
        {
            udteventHandler.ISStartCoroutineServerThrows = false;
            coroutine = ThrowToDjango();
            StartCoroutine(coroutine);
        }
    }

    IEnumerator ThrowToDjango()
	{
        // Error 카운팅
        int errorCounting = 0;
        itemImgs = new List<byte[]>();

		// Django 서버와 통신하는 코루틴 함수.
		// Django 서버와 통신하기 전에 이미지 처리과정 및 폼 구축.
		string imageData = ProcessData.ImageToString(imagePath);
        byte[] jsonBody = ProcessData.ConfigureJsonForm(imageData);
        BurpyICRequest bicr = new BurpyICRequest(djangoURI, "POST", jsonBody);
		IC.DjangoResult response = null;
        while(true)
        {
            yield return bicr.SendWebRequest();
            try
            {
                response = bicr.GetResponse<IC.DjangoResult>();
                break;
            }
            catch(Exception e)
            {
                if(errorCounting > 4)
                {
                    Debug.Log("4회차 재 요청에 실패했습니다. 조용히 에러좀 내겠습니다.");
                    throw e;
                }
                else
                {
                    Debug.Log("서버에 재 요청 중입니다. (" + (errorCounting + 1) +"회)");
                    errorCounting++;
                    continue;
                }
            }
        }
        // Django로부터 나온 response를 NodeRequest로 만들기 위한 파리미터로 아이디 배열 준비.
        int[] responIDArray = response.GetAllId();
        StartCoroutine(ThrowToNode(responIDArray));
    }
    
    IEnumerator ThrowToNode(int[] resultArray){
        int index = 0;//foreach문에서 1~3위의 이름을 받아올때 사용.
        // Nodejs 서버와 통신하는 코루틴 함수.
        // DjangoResult 객체의 제품 ID들만 받아오는 int형 배열을 파라미터로 갖음.
        // 해당 파라미터는 NodeRequest 객체의 인스턴스 변수.
        byte[] jsonBody = ProcessData.ConfigureJsonForm(resultArray);
        BurpyICRequest nodeRequest = new BurpyICRequest(nodeURI, "POST", jsonBody);
        IC.NodeResult response = null;
        yield return nodeRequest.SendWebRequest();
        try
        {
            response = nodeRequest.GetResponse<IC.NodeResult>();
        }
        catch(Exception e)
        {
            throw e;
            //
            // Do Something!
            //
        }

        foreach (IC.InfoElement i in response.result)
        {
            yield return AddImageList(i.imageUrl, itemImgs);
        }

        // Predict 후, 최초 UI 갱신.
        SearchingImage = GameObject.Find("searching");
        SearchingVolume = GameObject.Find("Volume");
        SearchingImage.SetActive(false);
        SearchingVolume.SetActive(false);
        SearchResultRankUI.SetActive(true);
        buttonsOfOtherResults.infoList.Clear();//리스트 초기화
        foreach (IC.InfoElement i in response.result)
        {
            if (index < 3){
                GameObject.Find("Top"+(index+1)+"Button").GetComponentInChildren<Text>().text = i.name;
                productCode[index] = i._id;
                productname[index] = i.name;
                productcategory[index] = i.category;
                productscore[index] = i.avgScore.ToString();
                GameObject.Find("Imagecast" + index).GetComponent<Image>().sprite = ProcessData.ImageToSprite(itemImgs[index]);
            }//이미지 검색결과 상위 3개의 품목이 나올때 이미지와 버튼 텍스트 출력 부분 2018.05.09

            else{
                // InfoElements를 다른 스크립트로 넘기는 로직.
                GameObject.Find("OtherResults").GetComponent<ButtonsOfOtherResults>().otherItemImgs.Add(itemImgs[index]);
                GameObject.Find("OtherResults").GetComponent<ButtonsOfOtherResults>().AddInfoElements(i);
            }
            index++;
        }
    }

    /// <summary>
	/// 이미지 등록 메소드. Node와 통신하여, 제품 id를 얻은 후에 구글 드라이브와 연동하여 training data를 등록.
	/// </summary>
	/// <typeparam name="resultArray">입력값(제품명, 카테고리)를 포함한 string형 배열</typeparam>
	public IEnumerator RegistrationThrowToNode(string[] resultArray , bool mode)
    {
        Coroutine<string> searchFolderRequest = null;
        Coroutine<string> createFolderRequest = null;
        Coroutine<bool> registImageRequest = null;

		string productId;
		if(mode)
        {
           // true:새로운 상품 등록 false:기존상품에 이미지 등록
            IR.NodeResult response = null;
            byte[] jsonBody = ProcessData.ConfigureJsonForm(resultArray[0],resultArray[1]);
            BurpyICRequest nodeRequest = new BurpyICRequest("https://burpyapp.herokuapp.com/api/product/ic", "POST", jsonBody);
            yield return nodeRequest.SendWebRequest();
            try
            {
                response = nodeRequest.GetResponse<IR.NodeResult>();
            }
            catch(Exception e)
            {
                throw e;
                //
                // Do Something!
                //
            }
            
            // Node로부터 받은 상품 id.
            productId = response.result.id.ToString();
        
		}
        else
        {
			productId = resultArray [0];
		}

        // 코루틴 반환으로 할당될 생성 폴더의 google drive id.
        string googleFolderId = null;

        GoModifyPage(productId);
        
        // 폴더의 유무를 검색.
        searchFolderRequest = StartCoroutine<string>(SearchFolderRequest(productId));
        yield return searchFolderRequest.coroutine;
        try
        {
            googleFolderId = searchFolderRequest.Value;
        }
        catch(Exception e)
        {
            throw e;
            //
            // Do Something!
            //
        }
        
        // 할당된 googleFolderId값이 null이라면 제품 폴더 생성 요청을 전송.
        // 그에 대한 응답으로 생성된 폴더 ID를 얻어, googleFolderId값으로 할당.
        if(googleFolderId == null)
        {
            createFolderRequest = StartCoroutine<string>(CreateFolderRequest(productId));
            yield return createFolderRequest.coroutine;
            try
            {
                googleFolderId = createFolderRequest.Value;
            }
            catch(Exception e)
            {
                throw e;
                //
                // Do Something!
                //
            }
        }

        // 최종적으로 이미지 등록.
        registImageRequest = StartCoroutine<bool>(RegistImageRequest(googleFolderId, GetProductImage()));
        yield return registImageRequest.coroutine;
        try
        {
            if(registImageRequest.Value)
                Debug.Log("이미지 등록 완료");
        }
        catch(Exception e)
        {
            throw e;
            //
            // Do Something!
            //
        }
    }

    public IEnumerator SearchTextThrowToNode(string result)
    {
        string GetURL = "https://burpyapp.herokuapp.com/api/suggest?q=" + result;
        BurpyICRequest nodeRequest = new BurpyICRequest(GetURL, "GET");
        ST.NodeResult response = null;
        yield return nodeRequest.SendWebRequest();
        try
        {
            response = nodeRequest.GetResponse<ST.NodeResult>();
        }
        catch(Exception e)
        {
            throw e;
            //
            // Do Something!
            //
        }
		Debug.Log (response.result.Length);
		if (response.result.Length == 0) {
			GameObject.Find("UIControl").GetComponent<UI>().SearchingImage.SetActive(false);
            GameObject.Find("UIControl").GetComponent<UI>().SearchingVolume.SetActive(false);
            buttonsOfOtherResults.UIInputControl(false);
		} else {
			InformMessage.SetActive (false);
			int count = 0;
			foreach (ST.InfoElement i in response.result)
			{
				SearchTemp [count] = response.result [count]._id;
				count++;
				buttonsOfOtherResults.AddInfoElements2 (i);
			}
			GameObject.Find("UIControl").GetComponent<UI>().SearchingImage.SetActive(false);
            GameObject.Find("UIControl").GetComponent<UI>().SearchingVolume.SetActive(false);
            buttonsOfOtherResults.UIInputControl(true);
		}
    }

    /// <summary>
	/// 이미지파일 등록 메소드.
	/// </summary>
    private byte[] GetProductImage()
    {
        UDTEventHandler ueh = GameObject.Find("UserDefinedTargetBuilder").GetComponent<UDTEventHandler>();
        byte[] content = ueh.imageByte;
        return content;
    }

    /// <summary>
	/// 제품 세부정보 등록을 위한 홈페이지 url로 이동.
	/// </summary>
	/// <typeparam name="productId">등록된 제품 Id</typeparam>
    private void GoModifyPage(string productId)
    {
		Application.OpenURL("https://burpyapp.herokuapp.com/product/" + productId);
    }

    /// <summary>
	/// 토큰 발급 요청 섹션. BurpyMonoBehaviour의 반환값을 갖는 Coroutine을 이용.
	/// </summary>
    IEnumerator GetAccessToken()
    {
        // 새로운 토큰 발급을 위한 요청 구성.
        RefreshTokenResult refreshToken = null;
        BurpyICRequest tokenRequest = new BurpyICRequest("POST");
		tokenRequest.ConfigureTokenRequst();
        
        // 요청 오류에 따라, 5회 가량 요청 재시도를 위한 반복문.
        // 5회째 요청마저 실패시, 발생한 Exception을 재차 throw.
        while(true){
            yield return tokenRequest.SendWebRequest();
            try
            {
                refreshToken = tokenRequest.GetResponse<RefreshTokenResult>();
                break;
            }
            catch(System.Exception e)
            {
                if(tokenRequest.GetErrorCount() > 4)
                {
                    tokenRequest.RollbackErrorCount();
                    throw e;
                }
                else
                {
                    tokenRequest.AddErrorCount();
                    Debug.LogError(tokenRequest.GetErrorCount() + "회차 저장소(Drive) 접근 요청 실패!");
                    continue;
                }
            }
        }
        yield return refreshToken.GetRefreshToken();
    }
    
    /// <summary>
	/// 폴더 검색 요청 섹션. BurpyMonoBehaviour의 반환값을 갖는 Coroutine을 이용.
	/// </summary>
    /// <typeparam name="folderName">검색할 폴더 이름</typeparam>
    IEnumerator SearchFolderRequest(string folderName)
    {
        // 작업을 위한 토큰 발급 요청.
        string token = null;
        Coroutine<string> tokenRequest = null;
        tokenRequest = StartCoroutine<string>(GetAccessToken());
        yield return tokenRequest.coroutine;
        try
        {
            token = tokenRequest.Value;
        }
        catch(Exception e)
        {
            throw e;
            //
            // AccessToken 요청에 따른 예외처리구간. Do Something!
            //
        }
        
        // 입력받은 이름의 폴더 검색을 위한 요청 및 응답(검색 결과) 객체화.
        GoogleDriveResult results = null;
        BurpyICRequest folderSearchRequest = new BurpyICRequest("POST");
        folderSearchRequest.ConfigureFolderSearch(token, folderName);
        yield return folderSearchRequest.SendWebRequest();
        try
        {
            results = folderSearchRequest.GetResponse<GoogleDriveResult>();
        }
        catch(Exception e)
        {
            throw e;
            //
            // SearchFolder 요청에 따른 예외처리구간. Do Something!
            //
        }

        // 옳바른 결과는 폴더의 id를 반환하지만 그렇지 않다면 null 반환.
        if(results.GetFilesListSize() != 0){
            yield return results.GetFolderId();
        } else {
            yield return null;
        }
    }
    
    /// <summary>
	/// 폴더 생성 요청 섹션. BurpyMonoBehaviour의 반환값을 갖는 Coroutine을 이용.
	/// </summary>
    /// <typeparam name="folderName">등록될 폴더 이름</typeparam>
    IEnumerator CreateFolderRequest(string folderName)
    {
        // 작업을 위한 토큰 발급 요청.
        string token = null;
        Coroutine<string> tokenRequest = null;
        tokenRequest = StartCoroutine<string>(GetAccessToken());
        yield return tokenRequest.coroutine;
        try
        {
            token = tokenRequest.Value;
        }
        catch(Exception e)
        {
            throw e;
            //
            // AccessToken 요청에 따른 예외처리구간. Do Something!
            //
        }
        
        // 입력받은 이름의 폴더 생성을 위한 요청 및 응답(해당 폴더의 GoogleDrive ID) 객체화.
        GoogleFiles folder = null;
        BurpyICRequest folderCreateRequest = new BurpyICRequest("POST");
        folderCreateRequest.ConfigureCreateFolder(token, folderName);
        yield return folderCreateRequest.SendWebRequest();
        try
        {
            folder = folderCreateRequest.GetResponse<GoogleFiles>();
        }
        catch(Exception e)
        {
            throw e;
            //
            // CreateFolder 요청에 따른 예외처리구간. Do Something!
            //
        }
        
        // 생성된 폴더 아이디 반환.
        yield return folder.id;
    }

    /// <summary>
	/// 이미지 등록 요청 섹션. BurpyMonoBehaviour의 반환값을 갖는 Coroutine을 이용.
	/// </summary>
    /// <typeparam name="registDirectory">등록될 구글 드라이브 폴더의 Id</typeparam>
	/// <typeparam name="img">등록될 이미지</typeparam>
    IEnumerator RegistImageRequest(string registDirectory, byte[] img)
    {
        // 작업을 위한 토큰 발급 요청.
        string token = null;
        Coroutine<string> tokenRequest = null;
        tokenRequest = StartCoroutine<string>(GetAccessToken());
        yield return tokenRequest.coroutine;
        try
        {
            token = tokenRequest.Value;
        }
        catch(Exception e)
        {
            throw e;
            //
            // AccessToken 요청에 따른 예외처리구간. Do Something!
            //
        }

        // 입력받은 구글 드라이브 폴더 내의 파일 갯수를 카운트하기 위한 요청 및 응답 객체화.
        GoogleDriveResult filesCounting = null;
        BurpyICRequest fileCountiongRequest = new BurpyICRequest("POST");
        fileCountiongRequest.ConfigureFilesCounting(token, registDirectory);
        yield return fileCountiongRequest.SendWebRequest();
        try
        {
            filesCounting = fileCountiongRequest.GetResponse<GoogleDriveResult>();
        }
        catch(Exception e)
        {
            throw e;
            //
            // FilesCounting 요청에 따른 예외처리구간. Do Something!
            //
        }

        int fileNumbering = filesCounting.GetFilesListSize();

        // 실제 이미지 등록.
        // JPEG 형식으로 등록한다는 것을 명시하였으니, 형식 변환시 유의해야함.
        BurpyICRequest registImageRequest = new BurpyICRequest("POST");
        registImageRequest.ConfigureImgRegist(token, registDirectory, (fileNumbering + 1).ToString() + ".jpg", img);
        yield return registImageRequest.SendWebRequest();

        // 완료되었다면 true 반환.
        yield return true;
    }

    IEnumerator AddImageList(string imgUrl, List<byte[]> imgList){
		string baseUrl = "https://s3.ap-northeast-2.amazonaws.com/burpy-app/";
		BurpyICRequest imageRequest = new BurpyICRequest(baseUrl + imgUrl, "Get");
        yield return imageRequest.SendWebRequest();
		byte[] img = imageRequest.downloadHandler.data;
        imgList.Add(img);
	}
}