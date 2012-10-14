using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	
	private int score=0;
	
	private tk2dTextMesh scoreText;
	private tk2dTextMesh stageText;
	private tk2dTextMesh numberText;
	private tk2dTextMesh clearText;
	
	public void AddScore(int quantity){
		score+=quantity;
		scoreText.text = "Score: " + score.ToString();
		scoreText.Commit();
	}
	
	// Use this for initialization
	void Start () {
		stageText=GameObject.Find("Stage").GetComponent<tk2dTextMesh>();
		numberText=GameObject.Find("Number").GetComponent<tk2dTextMesh>();
		clearText=GameObject.Find("Clear").GetComponent<tk2dTextMesh>();
		scoreText = GameObject.Find("ScoreText").GetComponent<tk2dTextMesh>();
	}
	
	public void StageCleared(){
		StartCoroutine("StageClear");
	}
	
	IEnumerator StageClear(){
		yield return new WaitForSeconds(1f);
		float t = 0f;
		while(t<10f){
			stageText.transform.position = Vector3.Slerp(stageText.transform.position,new Vector3(280f,240f,stageText.transform.position.z),5f*Time.deltaTime);
			numberText.transform.position = Vector3.Slerp(numberText.transform.position,new Vector3(400f,240f,numberText.transform.position.z),5f*Time.deltaTime);
			clearText.transform.position = Vector3.Slerp(clearText.transform.position,new Vector3(520f,240f,clearText.transform.position.z),5f*Time.deltaTime);
			t+=Time.deltaTime;
			yield return null;
		}
	}
}