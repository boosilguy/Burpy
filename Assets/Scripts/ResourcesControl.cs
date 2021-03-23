using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourcesControl : MonoBehaviour {

	//top3아템정보 넣기
    public void resultproductimagecontrol(int ranknumber)
    {
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
        GameObject.Find("resultproductimage").GetComponent<Image>().sprite = Resources.Load<Sprite>("information/" + ranknumber);
		GameObject findImage = GameObject.Find ("/UserDefinedTarget-"+ueh.TargetCounterTemp+"/informresult/ResultCanvas/Panel (1)/Image/resultproductimage");
		findImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("information/" + ranknumber);
    }
    public void resultproductcategorycontrol(string rankcategory)
    {
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
		GameObject findcategory = GameObject.Find ("/UserDefinedTarget-"+ueh.TargetCounterTemp+"/informresult/ResultCanvas/Panel (1)/productcategory");
		findcategory.GetComponent<Text>().text = "분류 : "+rankcategory;
    }
    public void resultproductnamecontrol(string rankname)
    {
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
		GameObject findname = GameObject.Find ("/UserDefinedTarget-"+ueh.TargetCounterTemp+"/informresult/ResultCanvas/Panel (1)/productname");
		findname.GetComponent<Text>().text = rankname;
    }
    public void resultproductscorecontrol(string rankscore)
    {
		UDTEventHandler ueh = GameObject.Find ("UserDefinedTargetBuilder").GetComponent<UDTEventHandler> ();
		GameObject findscore = GameObject.Find ("/UserDefinedTarget-"+ueh.TargetCounterTemp+"/informresult/ResultCanvas/Panel (1)/productscore");
		findscore.GetComponent<Text>().text = "평점 : "+rankscore;
    }
}
