//
//MusicStarter.cs
//Mecanimアニメーションイベントを使ったMusicStarter
//2014/07/29 N.Kobayashi
//
using UnityEngine;
using System.Collections;

public class MusicStarter : MonoBehaviour
{

	// オーディオソースへの参照
	public AudioSource[] refAudioSource;


	// Use this for initialization
	void Start()
	{
		foreach (var source in refAudioSource)
			if (source) source.Pause();
	}

	// Mecanimアニメーションイベントとして指定するOnCallMusicPlay
	public void OnCallMusicPlay(string str)
	{
		foreach (var source in refAudioSource)
		{
			// 文字列playを指定で再生開始
			if (str == "play")
				if (source) source.Play();
				// 文字列stopを指定で再生停止
				else if (str == "stop")
					if (source) source.Stop();
					// それ以外はポーズ
					else
					if (source) source.Pause();

		}
	}
}

