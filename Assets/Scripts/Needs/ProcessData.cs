using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

using IC = ImageClassification; // 이미지 Prediction namespace
using IR = ImageRegistration;   // 이미지 Registration namespace+
using ST = SearchText;          // 연관어 검색

public static class ProcessData {
	public static string ImageToString(string imagePath){
        // Django 서버로 던지기 위한 이미지 프로세싱 메소드.
        UDTEventHandler ueh = GameObject.Find("UserDefinedTargetBuilder").GetComponent<UDTEventHandler>();
        byte[] image = ueh.imageByte;
        string imageToStr = Convert.ToBase64String(image);
		return imageToStr;
	}

    public static Sprite ImageToSprite(byte[] image){
        // Image(byte array)를 Sprite(Not texture)로 변환하는 메소드.
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(image);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

	public static byte[] ConfigureJsonForm(string str){
        // Django 전용 Json form 구축 함수
        IC.DjangoRequest djangoRequest = new IC.DjangoRequest(str); 
		string jsonForm = JsonUtility.ToJson(djangoRequest);
		byte[] jsonBody = System.Text.Encoding.UTF8.GetBytes(jsonForm);
		return jsonBody;
	}

    public static byte[] ConfigureTextJsonForm(string str)
    {
        // Node 전용 Json form 구축 함수
        ST.NodeRequest nodeRequest = new ST.NodeRequest(str);
        string jsonForm = JsonUtility.ToJson(nodeRequest);
        byte[] jsonBody = System.Text.Encoding.UTF8.GetBytes(jsonForm);
        return jsonBody;
    }

    public static byte[] ConfigureJsonForm(int[] resultArray){
		// Node 전용 Json form 구축 함수
		IC.NodeRequest nodeRequest = new IC.NodeRequest(resultArray); 
		string jsonForm = JsonUtility.ToJson(nodeRequest);
		byte[] jsonBody = System.Text.Encoding.UTF8.GetBytes(jsonForm);
		return jsonBody;
	}

    public static byte[] ConfigureJsonForm(string itemName, string itemCate)
    {
        // Node 전용 Json form 구축 함수
        IR.NodeRequest nodeRequest = new IR.NodeRequest(itemName, itemCate);
        string jsonForm = JsonUtility.ToJson(nodeRequest);
        byte[] jsonBody = System.Text.Encoding.UTF8.GetBytes(jsonForm);
        return jsonBody;
    }

    public static GenericObject DecodeJsonForm<GenericObject>(byte[] handlerData) where GenericObject : ResultObject {
		// 웹서버(Django, Node)로부터 받아온 Json을 디코드할 Json Decoder.
		// 오퍼레이터나 메소드 존재 유무 등의 오류로 where 연산을 통한 제네릭 통제.
		// GenericObject : ResultObject를 상속받는 DjangoObject, NodeObject 들을 사용하길 바람.
		string result = System.Text.Encoding.UTF8.GetString(handlerData);
		GenericObject resultObject;
		resultObject = JsonUtility.FromJson<GenericObject>(result);
		return resultObject;
	}
}