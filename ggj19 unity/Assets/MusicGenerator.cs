using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicGenerator : MonoBehaviour
{
    AudioSource[] audioSources;

    [SerializeField]
    List<AudioClip> audioClips = new List<AudioClip>();

    [SerializeField]
    [Range(0f, 60f)]
    float minInterval, maxInterval;

    [SerializeField]
    int dontRepeatLast = 0;

    // Start is called before the first frame update
    void Start()
    {
        audioSources = GetComponents<AudioSource>();
        StartCoroutine(PlayStingers());
    }

    IEnumerator PlayStingers()
    {
        int audioSourceIndex = 0;

        while (true)
        {
            if (audioSourceIndex >= audioSources.Length)
                audioSourceIndex = 0;

            int audioClipIndex = Random.Range(0, audioClips.Count - 1 - dontRepeatLast);

            audioSources[audioSourceIndex].clip = audioClips[audioClipIndex];
            audioSources[audioSourceIndex].Play();
            audioSourceIndex++;

            AudioClip tempClip = audioClips[audioClipIndex];
            audioClips.RemoveAt(audioClipIndex);
            audioClips.Add(tempClip);

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }
}
