using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;

using IC = ImageClassification; // 이미지 Prediction namespace

public class UI : MonoBehaviour {

    public GameObject SearchingImage;//탐색UI
    public GameObject SearchingVolume;//탐색게이지
    public GameObject ResearchButton;//재탐색UI
    public GameObject otherResultsButton;// 다른 결과 Button
    public GameObject otherRankPanel;// 다른 결과 UI
    public GameObject Top3buttonUI;//상위 3개상품 UI
    public GameObject otherproductbutton;//9개상품 UI
    public GameObject SearchResultRankUI;//맥주모양 판낼
	public GameObject InputInformationGroup;//상품명과 카고리 입력 UI
    public GameObject InputSearchTextGroup;//연관어검색 UI
	public GameObject InitialScreenButtonGroup;//초기 이미지인식,이미지등록버튼그룹
	public GameObject buildButton;//인식버튼
	public GameObject textSearchResultGroup;//연관 검색 결과
    public GameObject InformMessage;//연관어 검색결과가 없을때 등장하는 UI
	public GameObject BuildPosition;//이미지 검색시 상품 가이드 UI
    //이미지 검색 버튼과 다시 검색버튼 UI false,true 동작을 위한 bool값
    public bool ISSearchButton = false;
    public bool ISResearchButton = false;
	public int urlproductid = 0;
	public bool selectmode;//인식 or 등록

	[Header("TextSearchResult Settings")]
	public TextSearchResultObjectGroup tsrog;

	[Serializable]
	public class TextSearchResultObjectGroup
	{
		public GameObject TextSearchResult1;
		public GameObject TextSearchResult2;
		public GameObject TextSearchResult3;
		public GameObject TextSearchResult4;
		public GameObject TextSearchResult5;
	}

    private Texture TrakingImageTexture;
    private string[] registrationProductInform = new string[2];

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        if (ISSearchButton == true) {
            SearchingImage.SetActive(true);
            SearchingVolume.SetActive(true);
            otherResultsButton.SetActive(true);
            ISSearchButton = false;
        }
        if (ISResearchButton == true)
        {
            ResearchButton.SetActive(true);
            ISResearchButton = false;
        }
    }

    public void OnclickRankUI(int rank)//상위 3개 품목 버튼 클릭 
    {
        CommunicateBurpyIC cbi = GameObject.Find("UserDefinedTargetBuilder").GetComponent<CommunicateBurpyIC>();
        ResourcesControl rpic = GameObject.Find("UIControl").GetComponent<ResourcesControl>();
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
            SearchResultRankUI.SetActive(false);
			rpic.resultproductimagecontrol(cbi.productCode[rank-1]);
			rpic.resultproductcategorycontrol(cbi.productcategory[rank-1]);
			rpic.resultproductnamecontrol(cbi.productname[rank-1]);
			rpic.resultproductscorecontrol(cbi.productscore[rank-1]);
			urlproductid = cbi.productCode[rank-1];
			ueh.ActivateTracking ();
            ISResearchButton = true;
    }

    public void OnclickCancelButton(){//취소 버튼 동작
        //otherResultsButton.SetActive(false);
        // Buttonsofotherresults.cs에서 숨긴 UI를 취소 버튼이 눌리면 다시 true로 변경
		InformMessage.SetActive (true);
        otherRankPanel.SetActive(false);
        SearchResultRankUI.SetActive(false);
        Top3buttonUI.SetActive(true);
		//buildButton.SetActive(true);
        otherproductbutton.SetActive(true);
		InitialScreenButtonGroup.SetActive (true);
		InputSearchTextGroup.SetActive(false);
		InputInformationGroup.SetActive(false);
		textSearchResultGroup.SetActive(false);
        UDTEventHandler ueh = GameObject.Find("UserDefinedTargetBuilder").GetComponent<UDTEventHandler>();
        ueh.OnclickResearchButton();
		tsrog.TextSearchResult1.SetActive (true);
		tsrog.TextSearchResult2.SetActive (true);
		tsrog.TextSearchResult3.SetActive (true);
		tsrog.TextSearchResult4.SetActive (true);
		tsrog.TextSearchResult5.SetActive (true);
    }

    public void OnclickResearchButton()//재탐색 버튼
    {
        ResearchButton.SetActive(false);
		InitialScreenButtonGroup.SetActive(true);
    }

	public void OnclickGoWebpageButton()
	{
		Application.OpenURL("https://burpyapp.herokuapp.com/product/"+ urlproductid);
	}

	public void OnclickTextSearchButton(bool mode)//연관어검색 페이지 이동 버튼 true:이미지 인식 false:이지 등록
    {
		if (mode) {
			otherRankPanel.SetActive (false);
			InputSearchTextGroup.SetActive (true);
		} 
		else 
		{
			otherResultsButton.SetActive (false);
			Top3buttonUI.SetActive (false);
			SearchResultRankUI.SetActive(true);
			InputSearchTextGroup.SetActive (true);
		}
		InputField resetField = GameObject.Find ("InputSearchText").GetComponent<InputField>();
		resetField.text = null;//InputField초기화
    }

	public void OnclickSendInputTextButton()//연관어 전송 버튼
	{
		CommunicateBurpyIC cbi = GameObject.Find("UserDefinedTargetBuilder").GetComponent<CommunicateBurpyIC>();
		StartCoroutine(cbi.SearchTextThrowToNode(registrationProductInform[0]));
		SearchResultRankUI.SetActive(false);
		InputSearchTextGroup.SetActive (false);
		textSearchResultGroup.SetActive (true);
		SearchingImage.SetActive(true);
        SearchingVolume.SetActive(true);
    }

    public void OnclickGotoInputInformFieldButton()//상품명 입력 UI로 이동
    {
		tsrog.TextSearchResult1.SetActive (true);
		tsrog.TextSearchResult2.SetActive (true);
		tsrog.TextSearchResult3.SetActive (true);
		tsrog.TextSearchResult4.SetActive (true);
		tsrog.TextSearchResult5.SetActive (true);
        InputSearchTextGroup.SetActive(false);
		textSearchResultGroup.SetActive (false);
		InformMessage.SetActive (true);
        InputInformationGroup.SetActive(true);
		InputField resetField = GameObject.Find ("Productname").GetComponent<InputField>();
		resetField.text = null;//InputField초기화
		GameObject.Find("ProductCategoryDropboxText").GetComponent<Text> ().text = "카테고리 선택";//Dropboxtext초기화
    }

    //연관검색입력
    public void InputProductnameSearchField(InputField iproductname)
    {
        registrationProductInform[0] = iproductname.text;
    }

    //InputInformationGroup에서 받은 값을 저장하는 매소드
    public void InputProductnameField(InputField iproductname)
    {
        registrationProductInform[0] = iproductname.text;
    }
    /*public void InputProductCategoryField(InputField iproductcategory)
    {
        registrationProductInform[1] = iproductcategory.text;
    }*/
	public void InputProductCategoryButton(string productcategory)
	{
		registrationProductInform[1] = productcategory;
		GameObject.Find("ProductCategoryDropboxText").GetComponent<Text> ().text = registrationProductInform [1];
	}

    public void OnclickSend()
    {
        CommunicateBurpyIC cbi = GameObject.Find("UserDefinedTargetBuilder").GetComponent<CommunicateBurpyIC>();
		StartCoroutine(cbi.RegistrationThrowToNode(registrationProductInform,true));//노드서버로 입력받은 이름과 카테고리를 전송하는 콜루틴 CommunicateBurpyIc스크립트에있음
		GameObject.Find("ProductCategoryDropboxText").GetComponent<Text> ().text = "카테고리 선택";
        InputInformationGroup.SetActive(false);//등록을 시도 했음으로 UI를 끈다
        SearchResultRankUI.SetActive(false);//등록을 시도 했음으로 UI를 끈다
		Top3buttonUI.SetActive(true);
		InitialScreenButtonGroup.SetActive(true);//검색버튼을 활성화시킨다.
    }

	public void OnclickInitialButton(bool selectFunction)//초기 모드 선택
	{
		InitialScreenButtonGroup.SetActive(false);
		buildButton.SetActive(true);
		BuildPosition.SetActive (true);
		selectmode = selectFunction;
	}
	//연관어검색으로 나온 결과를 보여주는 UI를 클릭했을때 구글드라이브에 이미지 자동 저장
	public void OnclickSearchTextResultUI(int rank)
	{
		CommunicateBurpyIC cbi = GameObject.Find("UserDefinedTargetBuilder").GetComponent<CommunicateBurpyIC>();
		string[] temp =new string[2];
		temp [0] = cbi.SearchTemp[rank].ToString();
		StartCoroutine(cbi.RegistrationThrowToNode(temp,false));
		Top3buttonUI.SetActive (true);
		SearchResultRankUI.SetActive (false);
		textSearchResultGroup.SetActive (false);
		InitialScreenButtonGroup.SetActive (true);
	}
	//sku.burpy
	//qwe1200213!
}
