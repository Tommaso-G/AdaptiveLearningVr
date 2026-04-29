using UnityEngine;
using System.Collections.Generic;
using VRBuilder.Core;
using System;
using System.Linq;
using System.Collections;
using VRBuilder.Core.Behaviors;
using static UnityEngine.UI.Image;

[System.Serializable]
public struct ChapterLink
{
    [Tooltip("Nome del nuovo capitolo da inserire.")]
    public string newChapter;
    [Tooltip("Il nuovo capitolo verrà inserito DOPO questo.")]
    public string previousChapter;
}

public class Node
{
    public int Id { get; set; }

    public int chapterId;
    public Node Next { get; set; }
    public Node OptionalNext { get; set; }

    public Node(int id)
    {
        Id = id;
        chapterId = id;
        Next = null;
        OptionalNext = null;
    }
}
public class ChaptersOrderManager : MonoBehaviour
{

    private IProcess process;
    private bool waitForChange;
    private bool nextChapter;
    private int currentNode;
    private Dictionary<string, int> chapterNameToIndex = new Dictionary<string, int>();
    private Dictionary<string, ExecuteChaptersBehavior> subchapterNameToIndex = new Dictionary<string, ExecuteChaptersBehavior>();
    private Dictionary<string, ExecuteChaptersBehavior> chapterWithExecuteBehavior = new Dictionary<string, ExecuteChaptersBehavior>();
    public Dictionary<string, ExecuteChaptersBehavior> ChapterWithExecuteBehavior => chapterWithExecuteBehavior;

    [Tooltip("Ordine con cui sono inseriti i capitoli opzionali.")]
    public List<ChapterLink> OptionalDefaultPosition;

    public List<ChapterLink> ChaptersToAddOrRemove; // (nome_nuovo_cap, nome_cap_precedente)
    [Tooltip("Inseriemento immediato dopo il capitolo corrente. Richiede si specificare solo il nome del nuovo capitolo.")]
    public bool addNow;
    [Tooltip("Rimozione immediata dopo il capitolo corrente. Non è necessario specificare nulla, rimuove il primo opzionale che trova.")]
    public bool removeNow;
    public int prevId;

    public event Action OnListChanged;
    public event Action OnRemoveCurrent;
    public event Action<string, IChapter> OnSubChapterAdded;

    public List<string> OptionalChapters { get; private set; } = new List<string>();
    public List<Node> nodes { get; private set; } = new List<Node>();
    public Node head { get; private set; }
    public bool empty { get; private set; }
    public int lastNodeId { get; private set; }

    public bool EditorChaptersReady { get; private set; } = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentNode = 0;
        nextChapter = false;
        empty = false;
        addNow = false;
        removeNow = false;
        prevId = -1;

        ProcessRunner.Events.ChapterStarted += (sender, args) =>
        {
            nextChapter = true;
        };
    }
    public void initialize(IProcess process)
    {
        this.process = process;
        Node previous = null;

        foreach (IChapter chapter in process.Data.Chapters)
        {
            int index = process.Data.Chapters.IndexOf(chapter);
            Node node = new Node(index);
            nodes.Add(node);
            chapterNameToIndex.Add(chapter.Data.Name, index);


            IList<IStep> steps = chapter.Data.Steps;

            foreach (IStep step in steps)
            {
                if (step.Data.Behaviors?.Data.Behaviors.FirstOrDefault() is ExecuteChaptersBehavior executeChaptersBehavior)
                {
                    List<SubChapter> subChapters = executeChaptersBehavior.Data.SubChapters;
                    foreach (SubChapter subChapter in subChapters)
                    {
                        subchapterNameToIndex.Add(subChapter.Chapter.Data.Name, executeChaptersBehavior);
                    }

                    if (!chapterWithExecuteBehavior.ContainsKey(chapter.Data.Name))
                    {
                        chapterWithExecuteBehavior.Add(chapter.Data.Name, executeChaptersBehavior);
                    }
                }
            }

            if (chapter.Data.Name.Contains("Optional"))
            {
                OptionalChapters.Add(chapter.Data.Name);
            }

            if (previous != null)
            {
                previous.Next = node;
            }

            previous = node;
        }

        head = nodes[0];
        lastNodeId = nodes.Count - OptionalChapters.Count - 1;
        nodes[lastNodeId].Next = null;

        while (ChaptersToAddOrRemove.Count > 0)
        {
            AddOptional(ChaptersToAddOrRemove[0].newChapter, ChaptersToAddOrRemove[0].previousChapter);
            ChaptersToAddOrRemove.RemoveAt(0);
        }

        EditorChaptersReady = true;
        PrintNodesList();
    }

    // chiamata in inizializzazione
    public void AddOptional(string chapterName, string prevName = null)
    {
        int newIndex = chapterNameToIndex[chapterName];

        if (prevName == null)
        {
            foreach (var chapterCouple in OptionalDefaultPosition)
            {
                if (chapterCouple.newChapter == chapterName)
                {
                    prevName = chapterCouple.previousChapter;
                }
            }
        }

        print("[ChapterManager] Capitolo prevName: " + prevName);

        if (!chapterNameToIndex.TryGetValue(prevName, out int prevIndex))
        {
            if (!subchapterNameToIndex.TryGetValue(prevName, out ExecuteChaptersBehavior executeChaptersBehavior))
            {
                Debug.LogWarning($"Capitolo non trovato: {prevName}");
                return;
            }
            else
            {
                var match = chapterWithExecuteBehavior.FirstOrDefault(key => key.Value == executeChaptersBehavior);

                if (match.Key == null)
                {
                    Debug.LogError("❌ Nessun match trovato in chapterWithExecuteBehavior!");
                    return;
                }

                string main_Chapter_name = match.Key;
                AddSubChapter(executeChaptersBehavior, newIndex, main_Chapter_name);
                print("Aggiunto Capitolo: " + chapterName + " come sottocapitolo in un'esecuzione parallela");
                return;
            }
        }
        else
        {
            print($"[ChapterOrderManager] capitolo {prevName}, idex {prevIndex}");
        }

            Node prevNode = nodes[prevIndex];
        Node newNode = nodes[newIndex];
        Node clNode = newNode;

        if (prevNode.OptionalNext == null)
        {
            if (newNode.OptionalNext != null)
            {
                clNode = cloneNode(newNode);
                Debug.Log("Creazione nodo clone: " + (clNode.Id + 1));
            }

            prevNode.OptionalNext = clNode;
            clNode.OptionalNext = prevNode.Next;
        }
        else
        {
            // se richiedo (6op, 3) e poi (7op, 3) ottengo 3 -> 6op -> 7op -> 4
            Node last = prevNode.OptionalNext;

            if (newNode.OptionalNext != null)
            {
                clNode = cloneNode(newNode);
                Debug.Log("Creazione nodo clone: " + (clNode.Id + 1));
            }

            while (last.OptionalNext != prevNode.Next)
            {
                last = last.OptionalNext;
            }
            last.OptionalNext = clNode;
            clNode.OptionalNext = prevNode.Next;
        }
        print("Aggiunto Capitolo: (id " + (clNode.Id + 1) + " [ch " + (clNode.chapterId + 1) + "], ch " + (clNode.Next != null ? (clNode.Next.chapterId + 1) : "null") + ", " + (clNode.OptionalNext != null ? "id " + (clNode.OptionalNext.Id + 1) + "[ch " + (clNode.OptionalNext.chapterId + 1) + "]" : "null") + ")\n"
               + "  Dopo il capitolo:  (id " + (prevNode.Id + 1) + " [ch " + (prevNode.chapterId + 1) + "], ch " + (prevNode.Next != null ? (prevNode.Next.chapterId + 1) : "null") + ", " + (prevNode.OptionalNext != null ? "id " + (prevNode.OptionalNext.Id + 1) + "[ch " + (prevNode.OptionalNext.chapterId + 1) + "]" : "null") + ")");
    }

    // chiamata runtime
    private void AddChapterNow(string chapterName)
    {
        if (chapterName == null)
        {
            Debug.LogWarning($"Inserimento nullo");
            return;
        }

        if (!chapterNameToIndex.TryGetValue(chapterName, out int newIndex))
        {
            Debug.LogWarning($"Capitolo non trovato: {chapterName}");
            return;
        }

        if (newIndex == null)
        {
            Debug.Log("Capitolo non trovato");
            return;
        }

        if (process.Data.Current.Data.Current.Data.Behaviors?.Data.Behaviors.FirstOrDefault() is ExecuteChaptersBehavior executeChaptersBehavior)
        {
            AddSubChapter(executeChaptersBehavior, newIndex, process.Data.Current.Data.Name);
            return;
        }
        // controllo che non sia l'ultimo capitolo
        if ((head.Id != process.Data.Chapters.Count - OptionalChapters.Count - 1) && !process.Data.Current.LifeCycle.DeactivateAfterActivation)
        {
            Node prevNode = head;
            Node newNode = nodes[newIndex];
            Node clNode = newNode;

            if (prevNode.OptionalNext == null)
            {
                if (newNode.OptionalNext != null)
                {
                    clNode = cloneNode(newNode);
                    Debug.Log("Creazione nodo clone: " + (clNode.Id + 1));
                }

                prevNode.OptionalNext = clNode;
                clNode.OptionalNext = prevNode.Next;
            }
            else
            {
                if (newNode.OptionalNext != null)
                {
                    clNode = cloneNode(newNode);
                    Debug.Log("Creazione nodo clone: " + (clNode.Id + 1));
                }

                clNode.OptionalNext = prevNode.OptionalNext;
                prevNode.OptionalNext = clNode;

            }
            OnListChanged?.Invoke();
            print("Aggiunto Capitolo: (id " + (clNode.Id + 1) + " [ch " + (clNode.chapterId + 1) + "], ch " + (clNode.Next != null ? (clNode.Next.chapterId + 1) : "null") + ", " + (clNode.OptionalNext != null ? "id " + (clNode.OptionalNext.Id + 1) + "[ch " + (clNode.OptionalNext.chapterId + 1) + "]" : "null") + ")\n"
              + " Dopo il capitolo:  (id " + (prevNode.Id + 1) + " [ch " + (prevNode.chapterId + 1) + "], ch " + (prevNode.Next != null ? (prevNode.Next.chapterId + 1) : "null") + ", " + (prevNode.OptionalNext != null ? "id " + (prevNode.OptionalNext.Id + 1) + "[ch " + (prevNode.OptionalNext.chapterId + 1) + "]" : "null") + ")");
        }
        else
        {
            Debug.Log("Impossibile aggiungere capitolo ora");
        }
    }

    private void AddSubChapter(ExecuteChaptersBehavior executeChaptersBehavior, int newIndex, string main_chapter_name)
    {
        List<SubChapter> subch = executeChaptersBehavior.Data.SubChapters;
        if (subch != null)
        {
            IChapter newChapter = process.Data.Chapters[nodes[newIndex].chapterId];

            executeChaptersBehavior.AddSubChapterAtRuntime(newChapter, false);

            OnSubChapterAdded.Invoke(main_chapter_name, newChapter);
            //foreach (IStep step in newChapter.Data.Steps)
            //{
            //    print("nome: " + step.Data.Name);
            //}
        }
    }

    public void RemoveChapter(string prevName = "", string chapterToRemoveName = "", int prevId = -1)
    {
        Node prevNode = null;

        if (chapterToRemoveName != "")
        {
            prevNode = nodes.FirstOrDefault(n => n.OptionalNext?.chapterId == chapterNameToIndex[chapterToRemoveName] && n.OptionalNext.OptionalNext != null);
        }
        else if (prevId != -1)//se io avessi accesso ai nodi potrei avere max precisione es remove(7op, 6op, (prevId)10)
        {
            prevNode = nodes.FirstOrDefault(n => n.Id == prevId);
        }
        else if (prevName != "")// se ho una lista aggiornata dei capitoli posso fare rimozione mirata es remove prima occorrenza di (7op, 6op)
        {
            prevNode = nodes.FirstOrDefault(n => n.chapterId == chapterNameToIndex[prevName] && n.OptionalNext != null);
        }
        else
        {
            // in alternativa posso rimuovere il primo capitolo che ho come OptionalNext
            prevNode = head;
        }

        if (prevNode != null && prevNode.OptionalNext != null && prevNode.OptionalNext.Id > (process.Data.Chapters.Count - OptionalChapters.Count - 1))
        {
            Node nodeToRemove = prevNode.OptionalNext;
            prevNode.OptionalNext = prevNode.OptionalNext.OptionalNext;
            nodeToRemove.OptionalNext = null;
            if (nodeToRemove.Id >= process.Data.Chapters.Count) // solo se era un nodo clonato, lo elimino
            {
                nodes.Remove(nodeToRemove);
            }
            OnListChanged?.Invoke();
            UnityEngine.Debug.Log("Nodo: " + (nodeToRemove.Id + 1) + "[capitolo " + (nodeToRemove.chapterId + 1) + "] rimosso");
        }
        else
        {
            RemoveCurrent(chapterToRemoveName);
        }
    }

    private void RemoveCurrent(string chapterToRemoveName = "")
    {
        if (chapterToRemoveName != "")
        {
            IChapter current = process.Data.Current;
            if (current.Data.Name == chapterToRemoveName)
            {
                OnRemoveCurrent?.Invoke();
                UnityEngine.Debug.Log("Rimosso capitolo corrente");
            }
            else
            {
                UnityEngine.Debug.Log("Capitolo non trovato o non opzionale");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Nome del capitolo passato a RemoveCurrent è null");
        }
    }
    // Nel caso il capitolo Xop fosse già stato aggiunto più avanti nella lista,
    // creo un clone per non perdere il primo inserimento
    private Node cloneNode(Node originalNode)
    {
        Node cloneNode = new Node(nodes.Count);
        cloneNode.chapterId = originalNode.chapterId;
        nodes.Add(cloneNode);
        cloneNode.Next = originalNode.Next;

        return cloneNode;
    }

    // Lettura della lista
    public void scrollNodesList()
    {
        head = nodes.FirstOrDefault(n => n.Id == currentNode);
        print("nodo precedente: (id " + (head.Id + 1) + " [ch " + (head.chapterId + 1) + "], ch " + (head.Next != null ? (head.Next.chapterId + 1) : "null") + ", " + (head.OptionalNext != null ? "id " + (head.OptionalNext.Id + 1) + "[ch " + (head.OptionalNext.chapterId + 1) + "]" : "null") + ")");
        if (head.OptionalNext != null)
        {
            Node prevHead = head;
            head = head.OptionalNext;
            prevHead.OptionalNext = null;
            currentNode = head.Id;
        }
        else if (head.Next != null)
        {
            Node prevHead = head;
            head = head.Next;
            prevHead.OptionalNext = null;
            currentNode = head.Id;
        }

        if (head.chapterId == lastNodeId)
        {
            empty = true;
            UnityEngine.Debug.Log("set empty = " + empty);
        }
        print("nodo corrente: (id " + (head.Id + 1) + " [ch " + (head.chapterId + 1) + "], ch " + (head.Next != null ? (head.Next.chapterId + 1) : "null") + ", " + (head.OptionalNext != null ? "id " + (head.OptionalNext.Id + 1) + "[ch " + (head.OptionalNext.chapterId + 1) + "]" : "null") + ")");
    }

    private void UpdateList()
    {
        // (2) aggiorno l'head ad ogni cambio capitolo
        if (head.chapterId != chapterNameToIndex[process.Data.Current.Data.Name] || nextChapter)
        {
            nextChapter = false;
            if (waitForChange)
            {
                scrollNodesList();
                OnListChanged?.Invoke();
                waitForChange = false;
            }
        }

        // (1) aspetto che il capitolo corrente sia == al capitolo del nodo 
        if (head.chapterId == chapterNameToIndex[process.Data.Current.Data.Name])
        {
            waitForChange = true;
        }
    }
    void Update()
    {
        if (process?.Data.Current != null)
        {
            UpdateList();
        }
        // per debug, poi chiamerò direttamente AddChapterNow() da un altro script
        if (addNow)
        {
            if (ChaptersToAddOrRemove.Count > 0)
            {
                AddChapterNow(ChaptersToAddOrRemove[0].newChapter);
                addNow = false;
            }
            else
            {
                Debug.Log("Inserimento impossibile: lista vuota");
                addNow = false;
            }
        }

        if (removeNow)
        {
            RemoveChapter(ChaptersToAddOrRemove.Count != 0 ? ChaptersToAddOrRemove[0].previousChapter : "", ChaptersToAddOrRemove.Count != 0 ? ChaptersToAddOrRemove[0].newChapter : "", prevId);
            //RemoveChapter("", ChaptersToAddOrRemove.Count != 0 ? ChaptersToAddOrRemove[0].newChapter : "", prevId);
            removeNow = false;
        }


    }

    public IEnumerable<string> AvailablePreviousChapters
    {
        get
        {
            yield return "None";   // voce null

            foreach (var key in chapterNameToIndex.Keys)
                yield return key;
        }
    }

    private void PrintNodesList()
    {
        foreach (Node node in nodes)
        {
            print("( id " + (node.Id + 1) + "[ch  " + (node.chapterId + 1) + "], ch " + (node.Next != null ? (node.Next.chapterId + 1) : "null") + ", id " + (node.OptionalNext != null ? (node.OptionalNext.Id + 1) : "null") + " " + (node.OptionalNext != null ? "[ch " + (node.OptionalNext.chapterId + 1) + "]" : "null") + ")");
        }

    }
}
