using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Core.SceneObjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using System;
using System.ComponentModel.Design;
using UnityEngine.UI;
using static FeedbackRepository;
using UnityEngine.Video;

public class FeedbackDisplayer : MonoBehaviour
{
    [Header("Riferimenti")]
    public FeedbackSetHolder feedbackHolder;

    public Transform FindFeedbackPositionChild(GameObject parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("[FeedbackAutoManager] FindFeedbackPositionChild chiamato con parent nullo.");
            return null;
        }

        Transform child = parent.transform.Find("feedbackPosition");

        if (child != null)
        {
            Debug.Log($"[FeedbackAutoManager] Figlio 'feedbackPosition' trovato in '{parent.name}'.");

            // Log dettagliati del child originale
            //Debug.Log($"[FeedbackAutoManager] CHILD LOCAL Position: {child.localPosition}, LOCAL Rotation: {child.localEulerAngles}, LOCAL Scale: {child.localScale}");
            //Debug.Log($"[FeedbackAutoManager] CHILD GLOBAL Position: {child.position}, GLOBAL Rotation: {child.eulerAngles}, GLOBAL Scale: {child.lossyScale}");

            // Crea un Transform temporaneo non parentato
            GameObject tempGO = new GameObject($"Temp_{child.name}");
            Transform temp = tempGO.transform;

            // 1️⃣ Posizione globale identica al child
            temp.position = child.position;

            // 2️⃣ Rotazione corretta: resetta X e Z, mantieni Y come nell’Inspector
            Vector3 euler = child.eulerAngles;
            temp.rotation = Quaternion.Euler(0f, euler.y, 0f);

            // 3️⃣ Scala globale identica
            temp.localScale = child.lossyScale;

            // Log del Transform pulito
            //Debug.Log($"[FeedbackAutoManager] TEMP Transform Position: {temp.position}, Rotation: {temp.eulerAngles}, Scale: {temp.localScale}");

            return temp;
        }
        else
        {
            Debug.LogWarning($"[FeedbackAutoManager] Nessun figlio 'feedbackPosition' trovato in '{parent.name}'.");
            return null;
        }
    }


public void ChooseFeedback(FeedbackData feedback, Transform position, FeedbackSetHolder holder)
{
    if (feedback == null)
    {
        Debug.LogWarning("[FeedbackDisplayer] Nessun feedback fornito a ChooseFeedback.");
        return;
    }

    if (holder == null)
    {
        Debug.LogError("[FeedbackDisplayer] Holder nullo passato a ChooseFeedback.");
        return;
    }

    // ======= Caso speciale: prefab personalizzato =======
    if (feedback.PersonalizedPrefab != null)
    {
        GameObject customInstance = Instantiate(feedback.PersonalizedPrefab, position.position, position.rotation);
        customInstance.name = $"Feedback_Custom_{feedback.FeedbackName}";
        holder.activeFeedbackInstance = customInstance;

        Debug.Log($"[FeedbackDisplayer] Istanza prefab personalizzato per feedback '{feedback.FeedbackName}'.");
        return; // Ignora tutto il resto
    }

    // ======= Scegli repository attivo (Profiling ha priorità) =======
    var activeRepo = holder.ProfilingFeedbackRepository != null
        ? holder.ProfilingFeedbackRepository as ScriptableObject
        : holder.FeedbackRepository as ScriptableObject;

    if (activeRepo == null)
    {
        Debug.LogError("[FeedbackDisplayer] Nessun repository valido trovato nel holder.");
        return;
    }

    // ======= Conta elementi multimediali =======
    int totalElements = 0;
    if (feedback.images != null) totalElements += feedback.images.Count;
    if (feedback.videos != null) totalElements += feedback.videos.Count;

    if (totalElements == 0)
    {
        Debug.LogWarning($"[FeedbackDisplayer] Il feedback '{feedback.FeedbackName}' non contiene immagini né video.");
        return;
    }

    // ======= Risolvi i prefab dal repository corretto =======
    GameObject singlePrefab = null;
    GameObject multiplePrefab = null;

    if (holder.ProfilingFeedbackRepository != null)
    {
        singlePrefab = holder.ProfilingFeedbackRepository.SingleContainer;
        multiplePrefab = holder.ProfilingFeedbackRepository.MultipleContainer;
    }
    else if (holder.FeedbackRepository != null)
    {
        singlePrefab = holder.FeedbackRepository.SingleContainer;
        multiplePrefab = holder.FeedbackRepository.MultipleContainer;
    }

    if (singlePrefab == null || multiplePrefab == null)
    {
        Debug.LogError("[FeedbackDisplayer] Prefab SingleContainer o MultipleContainer non assegnato nel repository attivo.");
        return;
    }

    // ======= Istanzia il container corretto =======
    GameObject containerInstance;

    if (totalElements == 1)
    {
        containerInstance = Instantiate(singlePrefab, position.position, position.rotation);
        containerInstance.name = $"Feedback_Single_{feedback.FeedbackName}";
        FillSingleContainer(feedback, containerInstance);
    }
    else
    {
        containerInstance = Instantiate(multiplePrefab, position.position, position.rotation);
        containerInstance.name = $"Feedback_Multiple_{feedback.FeedbackName}";
        FillMultipleContainer(feedback, containerInstance);
    }

    holder.activeFeedbackInstance = containerInstance;

    //Debug.Log($"[FeedbackDisplayer] Mostrato feedback '{feedback.FeedbackName}' con {totalElements} elementi.");
}


    

    private void FillSingleContainer(FeedbackData feedback, GameObject container)
    {
        if (feedback == null || container == null)
        {
            Debug.LogWarning("[FeedbackRepository] FillSingleContainer chiamato con parametri null.");
            return;
        }

        // Imposta titolo del feedback
        var headerText = container.transform.Find("Content/Modal Content Text")?.GetComponent<TMPro.TMP_Text>();
        if (headerText != null)
        {
            headerText.text = feedback.FeedbackName;
        }
        else
        {
            Debug.LogWarning("[FeedbackRepository] 'Modal Content Text' non trovato nel prefab.");
        }

        // Trova i contenitori nel prefab
        Transform imageContainer = container.transform.Find("Content/Image Container");
        Transform videoContainer = container.transform.Find("Content/Video Container");
        VideoPlayer videoPlayer = container.GetComponentInChildren<VideoPlayer>();
        UnityEngine.UI.Image imageComponent = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();

        // Disattiva entrambi all’inizio
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        // Determina se c’è un’immagine o un video
        bool hasImage = feedback.images != null && feedback.images.Count > 0 && feedback.images[0] != null;
        bool hasVideo = feedback.videos != null && feedback.videos.Count > 0 && feedback.videos[0] != null;

        if (hasImage)
        {
            if (imageContainer != null && imageComponent != null)
            {
                imageContainer.gameObject.SetActive(true);
                imageComponent.sprite = feedback.images[0];
                //Debug.Log($"[FeedbackRepository] Immagine impostata per '{feedback.FeedbackName}'.");
            }
            else
            {
                Debug.LogWarning("[FeedbackRepository] Immagine presente ma 'Image Container' non trovato nel prefab.");
            }
        }
        else if (hasVideo)
        {
            if (videoContainer != null && videoPlayer != null)
            {
                videoContainer.gameObject.SetActive(true);
                videoPlayer.clip = feedback.videos[0];
                videoPlayer.Play();
               // Debug.Log($"[FeedbackRepository] Video impostato per '{feedback.FeedbackName}'.");
            }
            else
            {
                Debug.LogWarning("[FeedbackRepository] Video presente ma 'Video Container' o 'Video Player' non trovati.");
            }
        }
        else
        {
            Debug.LogWarning($"[FeedbackRepository] Nessun contenuto multimediale valido trovato per '{feedback.FeedbackName}'.");
        }

            // Forza il re-layout del prefab
        Transform layoutParent = container.transform.Find("Content"); // o il transform che ha il VerticalLayoutGroup
        if (layoutParent != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layoutParent.GetComponent<RectTransform>());
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7
    private const string CONTENT_PATH = "Canvas/Content";
    private const string NAV_PANEL_PATH = "Nav Panel";
    private const string BASE_PAGE_NAME = "Page 1";
    private const string BASE_NAV_NAME = "Nav Item Toggle 1";

    private void FillMultipleContainer(FeedbackData feedback, GameObject container)
    {
        if (feedback == null || container == null)
        {
            Debug.LogWarning("[FeedbackRepository] FillMultipleContainer chiamato con parametri null.");
            return;
        }

        var allMedia = CollectMedia(feedback);
        if (allMedia.Count == 0)
        {
            Debug.LogWarning($"[FeedbackRepository] Nessun contenuto multimediale valido per '{feedback.FeedbackName}'.");
            return;
        }

        var contentParent = container.transform.Find(CONTENT_PATH);
        var navPanel = container.transform.Find(NAV_PANEL_PATH);

        if (contentParent == null || navPanel == null)
        {
            Debug.LogError("[FeedbackRepository] Struttura prefab non valida (manca Canvas/Content o Nav Panel).");
            return;
        }

        var basePage = contentParent.Find(BASE_PAGE_NAME);
        var baseNavItem = navPanel.Find(BASE_NAV_NAME);
        if (basePage == null || baseNavItem == null)
        {
            Debug.LogError("[FeedbackRepository] Page 1 o Nav Item Toggle 1 non trovati.");
            return;
        }

        SetHeader(container, feedback.FeedbackName);
        ClearOldPages(contentParent, basePage);
        ClearOldPages(navPanel, baseNavItem);

        CreatePages(allMedia, contentParent, navPanel, basePage, baseNavItem);
        SetupNavigation(container, navPanel, contentParent);
    }

    private List<(string name, object content)> CollectMedia(FeedbackData feedback)
    {
        var media = new List<(string name, object content)>();
        if (feedback.images != null)
            media.AddRange(feedback.images
                .Where(i => i != null)
                .Select(i => (i.name, (object)i)));

        if (feedback.videos != null)
            media.AddRange(feedback.videos
                .Where(v => v != null)
                .Select(v => (v.name, (object)v)));

        return media.OrderBy(m => m.name).ToList();
    }


    private void SetHeader(GameObject container, string title)
    {
        var header = container.transform.Find("Canvas/Header Text")?.GetComponent<TMPro.TMP_Text>();
        if (header != null)
            header.text = title;
    }

    private void ClearOldPages(Transform parent, Transform keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != keep && (child.name.StartsWith("Page") || child.name.StartsWith("Nav Item Toggle")))
                UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    private void CreatePages(List<(string name, object content)> allMedia,
        Transform contentParent, Transform navPanel, Transform basePage, Transform baseNavItem)
    {
        for (int i = 0; i < allMedia.Count; i++)
        {
            var (page, navItem) = (i == 0)
                ? (basePage, baseNavItem)
                : (UnityEngine.Object.Instantiate(basePage, contentParent), UnityEngine.Object.Instantiate(baseNavItem, navPanel));

            page.name = $"Page {i + 1}";
            navItem.name = $"Nav Item Toggle {i + 1}";

            var textComp = navItem.GetComponentInChildren<TMPro.TMP_Text>();
            if (textComp != null)
                textComp.text = (i == 0) ? "Start" : $"Step {i}";

            SetupMedia(allMedia[i].content, page);
        }
    }

    private void SetupMedia(object media, Transform page)
    {
        var imgContainer = page.Find("Image Container");
        var vidContainer = page.Find("Video Container");

        if (imgContainer != null) imgContainer.gameObject.SetActive(false);
        if (vidContainer != null) vidContainer.gameObject.SetActive(false);

        if (media is Sprite sprite)
        {
            var img = imgContainer?.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null)
            {
                imgContainer.gameObject.SetActive(true);
                img.sprite = sprite;
            }
        }
        else if (media is VideoClip clip)
        {
            var player = vidContainer?.GetComponentInChildren<VideoPlayer>();
            if (player != null)
            {
                vidContainer.gameObject.SetActive(true);
                player.clip = clip;
                player.Play();
            }
        }

        if (page.TryGetComponent(out RectTransform rect))
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void SetupNavigation(GameObject container, Transform navPanel, Transform contentParent)
    {
        Canvas.ForceUpdateCanvases();
        var linker = container.GetComponentInChildren<PageToggleLinkerIndexed>();
        if (linker == null)
        {
            Debug.LogWarning("[FeedbackRepository] Nessun PageToggleLinkerIndexed trovato nel prefab.");
            return;
        }

        linker.RefreshLists();

        foreach (Transform navItem in navPanel)
        {
            var toggle = navItem.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((isOn) => linker.OnToggleValueChanged(toggle, isOn));
            }
        }

        linker.StartCoroutine(DelayedRefresh(linker));

        // attiva solo la prima pagina e toggle
        for (int i = 0; i < contentParent.childCount; i++)
            contentParent.GetChild(i).gameObject.SetActive(i == 0);
        for (int i = 0; i < navPanel.childCount; i++)
        {
            var toggle = navPanel.GetChild(i).GetComponent<Toggle>();
            if (toggle != null)
                toggle.isOn = (i == 0);
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7

    public GameObject PrepareAndDisplayFeedback(FeedbackData feedback, Transform feedbackPosition, FeedbackSetHolder holder)
    {
        if (feedback == null || holder == null)
        {
            Debug.LogWarning("[FeedbackRepository] Parametri non validi in PrepareAndDisplayFeedback.");
            return null;
        }

        // Istanzia e visualizza il feedback
        ChooseFeedback(feedback, feedbackPosition, holder);


        return holder.activeFeedbackInstance;
    }



        // 🔁 Coroutine per refreshare dopo un frame
        private IEnumerator DelayedRefresh(PageToggleLinkerIndexed linker)
        {
            yield return null; // Attendi che Unity completi l'instanziazione
            linker.RefreshLists();
           // Debug.Log("[FeedbackRepository] PageToggleLinkerIndexed aggiornato dopo la creazione dinamica delle pagine.");
        }


}
