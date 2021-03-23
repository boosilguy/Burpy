using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;

using IC = ImageClassification;
using ST = SearchText;          // 연관어 검색

public class ButtonsOfOtherResults : MonoBehaviour {
	GameObject[] otherResults; // 기본 3개의 결과를 제외한 나머지 결과를 출력할 GameObject.
	public List<IC.InfoElement> infoList = new List<IC.InfoElement>(); // 기본 3개의 결과를 제외한 나머지 결과의 정보를 담는 List.
	public List<byte[]> otherItemImgs = new List<byte[]>(); // 다른 결과 이미지
	public List<ST.InfoElement> searchinfoList = new List<ST.InfoElement>(); // 기본 3개의 결과를 제외한 나머지 결과의 정보를 담는 List.
	[Header("OtherRankPanel")]
	[Tooltip("inactive 상태 Object는 스크립트로 접근하기 까다로움. 따라서 숨겨진 otherRankPanel을 찾기 위한 GameObject 변수.")]
	public GameObject otherRankPanel; // OnOtherResultButtonDown 함수를 통해 출력될 Panel로써, inactive 상태 Object는 스크립트로 접근하기 까다로움.

	//[HideInInspector]
	public GameObject Top3buttonUI;//상위 3개상품 UI
	//[HideInInspector]
	public GameObject SearchResultRankUI;//맥주모양 판낼
	//[HideInInspector]
	public GameObject textSearchResultButtonGroup;//연관어인식결과 버튼
	//[HideInInspector]
	public GameObject gotoInputInformFieldButton;//카테코리 입력 UI로이동 버튼

	private bool listSelect;//true : 인식 false : 등록
	private int productcount;
		

	public ButtonsOfOtherResults(){}

	void Awake(){
		
	}
	//true:other검색 false:연관어 검색
	void AssignGameObjects(bool mode){
		if (mode) {
			// otherResults GameObject를 할당하는 함수.
			// count개의 다른 결과를 제시해줌. 따라서 오브젝트 할당도 count번 진행되어야함.
			// 이 때 count 변수는 predicting 후 UI에 확인되는 결과 3개를 제외한 나머지 결과(Other results)
			productcount = this.infoList.Count;
			this.otherResults = new GameObject[productcount];
			int index = 0;
			for (index = 1; index <= productcount; index++) {
				otherResults [index - 1] = GameObject.Find ("Other" + index);
			}
			listSelect = true;
		}
		else 
		{
			productcount = this.searchinfoList.Count;
			this.otherResults = new GameObject[productcount];
			int index = 0;
			for (index = 1; index <= productcount; index++) {
				otherResults [index - 1] = GameObject.Find ("TextSearchResult"+ index);
			}
			listSelect = false;
		}
	}

	public void AddInfoElements(IC.InfoElement i){
		// InfoElement(다른 결과에 대한 정보)를 infoList에 추가하는 함수.
		this.infoList.Add(i);
	}
	public void AddInfoElements2(ST.InfoElement i){
		// InfoElement(연관검색)를 infoList에 추가하는 함수.
		this.searchinfoList.Add(i);
	}

	public void OnOtherResultButtonDown(){
		// 버튼 눌릴 시 Other Results를 보여줄 함수.
		// 기본 UI 구성을 바꿔, 최초 3개 결과를 숨김.
		GameObject.Find("Top3ButtonUI").SetActive(false);
		// Other Results 결과를 출력.
		otherRankPanel.SetActive(true);
		GameObject.Find("OtherResults").SetActive(false);
		AssignGameObjects(true);
		FillOtherResultName();
		RenderOtherResultUI();
	}

	//연관검색관련 컨트롤
	public void UIInputControl(bool existence){
		SearchResultRankUI.SetActive (true);
		if (!existence) {
			textSearchResultButtonGroup.SetActive (false);
			gotoInputInformFieldButton.SetActive (true);
		} else {
			textSearchResultButtonGroup.SetActive (true);
			for (int count = 5; count > searchinfoList.Count; count--) {
				GameObject.Find ("TextSearchResult"+count).SetActive (false);
			}
			gotoInputInformFieldButton.SetActive (true);
		}
		AssignGameObjects(false);
		FillOtherResultName();
		searchinfoList.Clear ();
		//RenderOtherResultUI();
	}

	void RenderOtherResultUI(){
		// otherResults의 자손 GameObject인 'ItemImage' GameObject에 Item 이미지를 할당하는 함수.
		// 반드시, AssignGameObjects가 반드시 선행되어야함.
		int index = 0;
		for(index=1 ; index<=productcount ; index++){
			Transform itemFrame = otherResults[index-1].transform.Find("ItemFrame");
			Transform itemPhoto = itemFrame.transform.Find("ItemPhoto");
			Transform itemImage = itemPhoto.transform.Find("ItemImage");
			itemImage.GetComponent<Image>().sprite = ProcessData.ImageToSprite(otherItemImgs[index-1]);
			//StartCoroutine(AssignItemImage(itemImage, infoList.ElementAt(index - 1).imageUrl));
		}
	}

	void FillOtherResultName(){
		// otherResults의 자식 GameObject인 'Info' GameObject에 Item 이름을 할당하는 함수.
		// 반드시, AssignGameObjects가 반드시 선행되어야함.
		int index = 0;
		for(index=1 ; index<=productcount ; index++){
			Transform itemInfo = otherResults[index-1].transform.Find("Info");
			if (listSelect) {
				itemInfo.GetComponent<Text> ().text = infoList.ElementAt (index - 1).name;
			} else {
				itemInfo.GetComponent<Text> ().text = searchinfoList.ElementAt (index - 1).name;
			}
		}
	}

	public void OnclickOtherRankUI(int rank)
	{
		ResourcesControl rpic = GameObject.Find("UIControl").GetComponent<ResourcesControl>();
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
		UI ui = GameObject.Find("UIControl").GetComponent<UI>();
		otherRankPanel.SetActive(false);
		GameObject.Find("RankPanel").SetActive(false);//상품클릭시 판넬이 사라지지 않아 추가 하였습니다.
		Top3buttonUI.SetActive(true);//9개의 상품을 보여줄때 false를 했기때문에 현 단계에서 다시 true로 변경합니다. 변경하지 않으면 다시 검색시 UI가 등장하지 않는 버그가 생깁니다.
		rpic.resultproductimagecontrol(infoList.ElementAt(rank-4)._id);
		rpic.resultproductcategorycontrol(infoList.ElementAt(rank-4).category);
		rpic.resultproductnamecontrol(infoList.ElementAt(rank-4).name);
		rpic.resultproductscorecontrol(infoList.ElementAt(rank-4).avgScore.ToString());
		ui.urlproductid = infoList.ElementAt(rank-4)._id;
		ueh.ActivateTracking ();
		ui.ISResearchButton = true;
	}


	// 이미지 처리하는 요청입니다아아아아아아
	IEnumerator AssignItemImage(Transform item, string imgUrl){
		string baseUrl = "https://s3.ap-northeast-2.amazonaws.com/burpy-app/";
		BurpyICRequest imageRequest = new BurpyICRequest(baseUrl + imgUrl, "Get");
		yield return imageRequest.SendWebRequest();
		byte[] img = imageRequest.downloadHandler.data;
		item.GetComponent<Image>().sprite = ProcessData.ImageToSprite(img);
	}
}
