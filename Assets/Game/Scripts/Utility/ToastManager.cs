using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastManager : MonoBehaviour
{
    public GameObject toastPrefab;
    public Transform toastParent;
    public float defaultDuration = 2f;

    private Queue<ToastData> toastQueue = new Queue<ToastData>();
    private bool isShowing = false;
    private bool isPaused = false;

    private List<AudioSource> activeAudioSources = new List<AudioSource>();

    private class ToastData
    {
        public string message;
        public float duration;

        public ToastData(string msg, float dur)
        {
            message = msg;
            duration = dur;
        }
    }

    public void ShowToast(string message, float duration = -1f)
    {
        float showDuration = (duration <= 0) ? defaultDuration : duration;
        toastQueue.Enqueue(new ToastData(message, showDuration));
        if (!isShowing)
        {
            StartCoroutine(HandleToastQueue());
        }
    }

    private IEnumerator HandleToastQueue()
    {
        isShowing = true;

        while (toastQueue.Count > 0)
        {
            ToastData data = toastQueue.Dequeue();

            GameObject toast = Instantiate(toastPrefab, toastParent);
            Animator animator = toast.GetComponent<Animator>();
            AudioSource audio = toast.GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.Play();
                activeAudioSources.Add(audio);
            }
            TMP_Text text = toast.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = data.message;

            // Play SlideIn
            animator.SetBool("isOpen", true);

            // Wait for slide-in duration
            yield return WaitWithPause(0.2f);

            // Stay on screen
            yield return WaitWithPause(data.duration);

            // Play SlideOut
            animator.SetBool("isOpen", false);

            // Wait for slide-out duration
            yield return WaitWithPause(0.2f);

            activeAudioSources.Remove(toast.GetComponent<AudioSource>());
            Destroy(toast);
        }

        isShowing = false;
    }

    private IEnumerator WaitWithPause(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            if (!isPaused)
            {
                elapsed += Time.unscaledDeltaTime;
            }
            yield return null;
        }
    }

    public void PauseToasts()
    {
        isPaused = true;

        // Pause all active toast audios
        foreach (var audio in activeAudioSources)
        {
            if (audio != null && audio.isPlaying)
                audio.Pause();
        }
    }

    public void ResumeToasts()
    {
        isPaused = false;

        // Resume all active toast audios
        foreach (var audio in activeAudioSources)
        {
            if (audio != null)
                audio.UnPause();
        }
    }
}
