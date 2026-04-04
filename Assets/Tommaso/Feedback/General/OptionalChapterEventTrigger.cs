using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using VRBuilder.Core;

public class OptionalChapterEventTrigger : MonoBehaviour
{
    [System.Serializable]
    public class ChapterEventLink
    {
        public string chapterName;
        public List<UnityEvent> onChapterActive;
    }

    public List<ChapterEventLink> chapterEvents = new List<ChapterEventLink>();

    private ChaptersOrderManager co_mgr;

    private void Start()
    {
        co_mgr = GetComponent<ChaptersOrderManager>();
        if (co_mgr == null)
            co_mgr = FindFirstObjectByType<ChaptersOrderManager>();

        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        IProcess process = ProcessRunner.Current;

        // Aspetta che ChaptersOrderManager sia inizializzato
        if (!co_mgr.EditorChaptersReady)
        {
            StartCoroutine(WaitAndCheck(process));
            return;
        }

        CheckActiveChapters(process);
    }

    private System.Collections.IEnumerator WaitAndCheck(IProcess process)
    {
        yield return new WaitUntil(() => co_mgr.EditorChaptersReady);
        CheckActiveChapters(process);
    }

    private void CheckActiveChapters(IProcess process)
    {
        // Scorre la sequenza dei nodi per trovare i capitoli opzionali attivi
        Node current = co_mgr.head;
        HashSet<string> activeOptionals = new HashSet<string>();

        while (current != null)
        {
            string name = process.Data.Chapters[current.chapterId].Data.Name;
            if (name.Contains("Optional"))
                activeOptionals.Add(name);

            current = current.OptionalNext ?? current.Next;
        }

        // Scatena gli eventi per i capitoli opzionali attivi
        foreach (var link in chapterEvents)
        {
            if (activeOptionals.Contains(link.chapterName))
            {
                Debug.Log($"[OptionalChapterEventTrigger] Capitolo '{link.chapterName}' attivo, scateno eventi.");
                foreach (var unityEvent in link.onChapterActive)
                {
                    unityEvent?.Invoke();
                }
            }
        }
    }
}