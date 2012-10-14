using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour {
	
	public bool isSpawning;
	public int waves;
	public int[] miniwaves;
	public Vector2[] minMaxEnemiesPerMiniWave;
	public Transform[] enemies;
	public Transform player;
	public bool haveCompanions;
	public int bossNumber=1;
	
	private int actualWave=0;
	private int actualMiniwave=0;
	// Use this for initialization
	void Start () {
		PoolManager.Pools["Players"].Spawn(player, new Vector3(Screen.width/2,Screen.height/2,Screen.height/2),Quaternion.identity);
		StartCoroutine("GameFlow");
	}
	
	IEnumerator GameFlow(){
		while(actualWave != waves && actualMiniwave != miniwaves[actualWave]){
			if(isSpawning){
				if(GetEnemiesAlive() == 0){
					SpawnMiniWave();
					actualMiniwave++;
					if(actualMiniwave == miniwaves[actualWave]){
						actualWave++;
						actualMiniwave=0;
						yield return new WaitForSeconds(5f);
					}
				}
			}
			yield return null;
		}
		yield return new WaitForSeconds(5f);
		SpawnBoss();
	}
	
	void SpawnMiniWave(){
		int er = Random.Range((int)minMaxEnemiesPerMiniWave[actualMiniwave].x,(int)minMaxEnemiesPerMiniWave[actualMiniwave].x);
		for(int i = 0; i<er;i++){
			float r = Random.Range(0f,320f);
			if(i < er/2)
				PoolManager.Pools["Enemies"].Spawn(enemies[Random.Range(0,enemies.Length-bossNumber)], new Vector3(-50f,r,r),Quaternion.identity);
			else
				PoolManager.Pools["Enemies"].Spawn(enemies[Random.Range(0,enemies.Length-bossNumber)], new Vector3(Screen.width+50f,r,r),Quaternion.identity);
		}		
	}
	
	void SpawnBoss(){
		float r = Random.Range(0f,320f);
		PoolManager.Pools["Enemies"].Spawn(enemies[enemies.Length-1], new Vector3(Screen.width+50f,r,r),Quaternion.identity);
	}
	
	int GetEnemiesAlive(){
		return GameObject.FindGameObjectsWithTag("Enemy").Length;
	}
}