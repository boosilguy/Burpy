using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class BurpyICRequest : UnityWebRequest {
	byte[] boundary = null;

	// Google Drive Api의 Authorization info.
	private static string client_id = "128151809305-g518rut0ap5nq9935vsvcbu7kfgbbj33.apps.googleusercontent.com";
	private static string client_secret = "moizTS5E9TQIyLObALu4FUF5";
	private static string refresh_token = "1/8t8GZ8o4Sgkc1Luie-fCSfqBmnbLj-xoNm9Fjs4KEiE";
	private int errorCounter = 0;

	/// <summary>
	/// Burpy의 웹서버(Django, Node, Google Drive) Request.
	/// </summary>
	public BurpyICRequest(string method)
	{
		this.url = "nothing";
        // this.timeout = 0;
		this.method = method;
		this.uploadHandler = (UploadHandler) new UploadHandlerRaw(null);
		this.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
		this.chunkedTransfer = false;
	}
    public BurpyICRequest(string uri, string method)
    {
        this.url = uri;
        // this.timeout = 0;
        this.method = method;
        this.uploadHandler = (UploadHandler)new UploadHandlerRaw(null);
        this.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        this.chunkedTransfer = false;
        SetJsonHeader();
    }
    public BurpyICRequest(string uri, string method, byte[] form)
	{
		this.url = uri;
        // this.timeout = 0;
		this.method = method;
		this.uploadHandler = (UploadHandler) new UploadHandlerRaw(form);
		this.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
		this.chunkedTransfer = false;
		SetJsonHeader();
	}

	/// <summary>
	/// Json Type의 요청 전용 Header.
	/// </summary>
	public void SetJsonHeader()
	{
		SetRequestHeader("Content-Type", "application/json");
		SetRequestHeader("enctype", "multipart/form-data");
	}

	/// <summary>
	/// 웹서버(Django, Node)로부터 리스폰스를 가져온다.
	/// 오퍼레이터나 메소드 존재 유무 등의 오류로 where 연산을 통한 제네릭 통제.
	/// GenericObject : ResultObject를 상속받는 DjangoObject, NodeObject 들을 사용하길 바람.
	/// </summary>
	public GenericObject GetResponse<GenericObject>() where GenericObject : ResultObject
	{
		GenericObject result = null;
		result = ProcessData.DecodeJsonForm<GenericObject>(downloadHandler.data);
		if(result == null)
		{
			throw new BurpyException("요청에 따른 응답의 객체화 실패.");
		}
		else
		{
			return result;
		}
	}

	/// <summary>
	/// 구글 Api 토큰 호출 Request Header 설정.
	/// </summary>
	void SetTokenRequestHeader()
	{
		SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
	}

	/// <summary>
	/// 구글 드라이브 전용 이미지 등록 Request Header 설정.
	/// </summary>
	/// <typeparam name="auth">access_token 값.</typeparam>
	/// <typeparam name="textBoundary">multipart form의 바운더리 값.</typeparam>
	public void SetFileRegistHeader(string auth, string textBoundary)
	{
		SetRequestHeader("Authorization", auth);
		SetRequestHeader("Content-Type", "multipart/related; boundary=" + textBoundary);
	}

	/// <summary>
	/// 구글 드라이브 전용 파일 검색 Request Header 설정.
	/// </summary>
	/// <typeparam name="auth">access_token 값.</typeparam>
	public void SetFileSearchHeader(string auth)
	{
		SetRequestHeader("Authorization", auth);
	}

	/// <summary>
	/// Google OAuth로부터 새로운 토큰을 받기 위해 폼을 구성. WWWForm을 사용.
	/// </summary>
	public byte[] ImplementTokenForm()
	{
		WWWForm formBody = new WWWForm();
		formBody.AddField("client_id", client_id);
		formBody.AddField("client_secret", client_secret);
		formBody.AddField("refresh_token", refresh_token);
		formBody.AddField("grant_type", "refresh_token");

		return formBody.data;
	}

	/// <summary>
	/// Google OAuth로부터 새로운 토큰을 받기 위해 요청을 구성. 객체 url은 자동 할당됨.
	/// </summary>
	public void ConfigureTokenRequst()
	{
		this.url = "https://www.googleapis.com/oauth2/v4/token";
		this.uploadHandler = new UploadHandlerRaw(ImplementTokenForm());
		SetTokenRequestHeader();
	}

	/// <summary>
	/// 학습데이터 폴더 내 제품 폴더 검색 URI 설정. 객체 url은 자동 할당됨.
	/// </summary>
	/// <typeparam name="service">서비스할 access_token.</typeparam>
	/// <typeparam name="fileName">검색할 폴더 이름.</typeparam>
	public void ConfigureFolderSearch(string service, string fileName)
	{
		string fileNameQuery = WWW.EscapeURL("name = \'") + fileName + WWW.EscapeURL("\'");
		string mimeTypeQuery = WWW.EscapeURL("mimeType = \'") + "application/vnd.google-apps.folder" + WWW.EscapeURL("\'");
		string parentsQuery = WWW.EscapeURL("\'") + "1bsXXNCY-rkcT7aLYsqDo6gwK3lJcxY15" + WWW.EscapeURL("\' in parents");
		this.url = "https://www.googleapis.com/drive/v3/files?q=" + fileNameQuery + "&" + mimeTypeQuery + "&" + parentsQuery;
		this.method = "GET";

		SetFileSearchHeader(service);
	}

	/// <summary>
	/// 학습데이터 폴더 내 제품 폴더 검색 URI 설정. 객체 url은 자동 할당됨.
	/// </summary>
	/// <typeparam name="service">서비스할 access_token.</typeparam>
	/// <typeparam name="fileName">검색할 폴더 이름.</typeparam>
	public void ConfigureFilesCounting(string service, string parentFolderId)
	{
		string parentsQuery = WWW.EscapeURL("\'") + parentFolderId + WWW.EscapeURL("\' in parents");
		this.url = "https://www.googleapis.com/drive/v3/files?q=" + parentsQuery;
		this.method = "GET";

		SetFileSearchHeader(service);
	}

	/// <summary>
	/// 이미지 등록용 Multipart/related Body 구성용 메소드.
	/// </summary>
	/// <typeparam name="parent">등록될 이미지의 Directory id.</typeparam>
	/// <typeparam name="regName">등록될 이미지의 파일명.</typeparam>
	/// <typeparam name="imageFile">등록될 이미지.</typeparam>
	public List<IMultipartFormSection> ImplementMultipartForm(string parent, string regName, byte[] imageFile)
	{
		// Image 정보 및 설정
		string nameSubQuery = "\"name\":\""+ regName +"\"";
		string parentsSubQuery = "\"parents\": [\""+ parent +"\"]";
		string jsonQuery = "{"+ nameSubQuery + ", " + parentsSubQuery +"}";
		byte[] jsonbody = System.Text.Encoding.UTF8.GetBytes(jsonQuery);

		// Multipart form 구성요소 설정(1st Section : Json 기반 이미지 정보, 2nd Section : 이미지)
		List<IMultipartFormSection> formBody = new List<IMultipartFormSection>();
		MultipartFormDataSection infoSection = new MultipartFormDataSection("data", jsonbody, "application/json; charset=UTF-8");
		MultipartFormDataSection imageSection = new MultipartFormDataSection("file", imageFile, "image/jpeg");
		formBody.Add(infoSection);
		formBody.Add(imageSection);

		return formBody;
	}

	/// <summary>
	/// 폴더 등록용 Multipart/related Body 구성용 메소드.
	/// </summary>
	/// <typeparam name="parent">등록될 폴더의 Directory id.</typeparam>
	/// <typeparam name="regName">등록될 폴더명.</typeparam>
	public List<IMultipartFormSection> ImplementMultipartForm(string parent, string regName)
	{
		// 폴더 정보 및 설정
		string nameSubQuery = "\"name\":\""+ regName +"\"";
		string mimeTypeQuery = "\"mimeType\":\"application/vnd.google-apps.folder\"";
		string parentsSubQuery = "\"parents\": [\""+ parent +"\"]";
		string jsonQuery = "{"+ nameSubQuery + ", " + mimeTypeQuery + ", " + parentsSubQuery +"}";
		byte[] jsonbody = System.Text.Encoding.UTF8.GetBytes(jsonQuery);

		// Multipart form 구성요소 설정(1st Section : Json 기반 폴더 정보, 2nd Section : 폴더)
		List<IMultipartFormSection> formBody = new List<IMultipartFormSection>();
		MultipartFormDataSection infoSection = new MultipartFormDataSection("data", jsonbody, "application/json; charset=UTF-8");
		formBody.Add(infoSection);

		return formBody;
	}

	/// <summary>
	/// 이미지 파일 업로드 URI 설정 및 업로드 폼 구성. 객체 url은 자동 할당됨. 임시로 Text/plain.
	/// </summary>
	/// <typeparam name="service">서비스할 access_token.</typeparam>
	/// <typeparam name="parent">등록될 폴더의 id.</typeparam>
	/// <typeparam name="regName">등록될 이미지의 파일명.</typeparam>
	/// <typeparam name="imageFile">등록될 이미지.</typeparam>
	public void ConfigureImgRegist(string service, string parent, string regName, byte[] imageFile)
	{
		// parent id를 갖는 폴더에 regName으로 imageFile을 등록하는 Multipart/related 폼 구성.
		List<IMultipartFormSection> form = ImplementMultipartForm(parent, regName, imageFile);
		// url 및 method 설정.
		this.url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
		this.method = "POST";

		// Boundary요소 생성.
		this.boundary = GenerateBoundary();
		string textBoundary = System.Text.Encoding.UTF8.GetString(this.boundary);
		byte[] terminate = System.Text.Encoding.UTF8.GetBytes(string.Concat("\r\n--", textBoundary, "--"));

		// Multipart form 구현을 위한 인코딩.
		byte[] formSections = SerializeFormSections(form, this.boundary);
		byte[] body = new byte[formSections.Length + terminate.Length];
		System.Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
		System.Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

		// uploadHandler 설정 및 Token 할당과 Header 설정.
		this.uploadHandler = new UploadHandlerRaw(body);
		SetFileRegistHeader(service, textBoundary);
	}

	/// <summary>
	/// 드라이브 폴더 생성 URI 설정 및 폴더 생성 폼 구성. 객체 url은 자동 할당됨.
	/// </summary>
	/// <typeparam name="service">서비스할 access_token.</typeparam>
	/// <typeparam name="regName">생성될 폴더명.</typeparam>
	public void ConfigureCreateFolder(string service, string regName)
	{
		// parent id를 갖는 폴더에 regName으로 폴더를 등록하는 Multipart/related 폼 구성.
		string parent = "1bsXXNCY-rkcT7aLYsqDo6gwK3lJcxY15";
		List<IMultipartFormSection> form = ImplementMultipartForm(parent, regName);
		// url 및 method 설정.
		this.url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
		this.method = "POST";

		// Boundary요소 생성.
		this.boundary = GenerateBoundary();
		string textBoundary = System.Text.Encoding.UTF8.GetString(this.boundary);
		byte[] terminate = System.Text.Encoding.UTF8.GetBytes(string.Concat("\r\n--", textBoundary, "--"));

		// Multipart form 구현을 위한 인코딩.
		byte[] formSections = SerializeFormSections(form, this.boundary);
		byte[] body = new byte[formSections.Length + terminate.Length];
		System.Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
		System.Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

		// uploadHandler 설정 및 Token 할당과 Header 설정.
		this.uploadHandler = new UploadHandlerRaw(body);
		SetFileRegistHeader(service, textBoundary);
	}

	/// <summary>
	/// 해당 요청 객체의 에러 횟수를 증가.
	/// </summary>
	public void AddErrorCount(){
		this.errorCounter++;
	}

	/// <summary>
	/// 해당 요청 객체의 에러 횟수를 반환.
	/// </summary>
	public int GetErrorCount(){
		return this.errorCounter;
	}

	/// <summary>
	/// 해당 요청 객체의 에러 횟수를 0으로 회귀.
	/// </summary>
	public void RollbackErrorCount(){
		this.errorCounter = 0;
	}
}
