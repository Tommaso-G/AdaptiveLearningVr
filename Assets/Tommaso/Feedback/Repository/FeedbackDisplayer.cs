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
            Debug.LogWarning("[FeedbackRepository] Nessun feedback fornito a DisplayFeedback.");
            return;
        }

        // Determina quanti elementi multimediali ci sono
        int totalElements = 0;
        if (feedback.images != null) totalElements += feedback.images.Count;
        if (feedback.videos != null) totalElements += feedback.videos.Count;

        // Se non ci sono media, logga e interrompi
        if (totalElements == 0)
        {
            Debug.LogWarning($"[FeedbackRepository] Il feedback '{feedback.FeedbackName}' non contiene immagini né video.");
            return;
        }

        // Controllo prefabs
        if (feedbackHolder.FeedbackRepository.SingleContainer == null || feedbackHolder.FeedbackRepository.MultipleContainer == null)
        {
            Debug.LogError("[FeedbackRepository] Prefab SingleContainer o MultipleContainer non assegnato.");
            return;
        }

        GameObject containerInstance;

        // Se un solo elemento → prefab singolo
        if (totalElements == 1)
        {
            containerInstance = Instantiate(feedbackHolder.FeedbackRepository.SingleContainer, position.position, position.rotation);
            containerInstance.name = $"Feedback_Single_{feedback.FeedbackName}";
            FillSingleContainer(feedback, containerInstance);
        }
        else
        {
            // Più elementi → prefab multiplo
            containerInstance = Instantiate(feedbackHolder.FeedbackRepository.MultipleContainer, position.position, position.rotation);
            containerInstance.name = $"Feedback_Multiple_{feedback.FeedbackName}";
            FillMultipleContainer(feedback, containerInstance);
        }

        // Salva istanza attiva nel FeedbackSetHolder
        if (holder != null)
            holder.activeFeedbackInstance = containerInstance;

        //Debug.Log($"[FeedbackRepository] Mostrato feedback '{feedback.FeedbackName}' con {totalElements} elementi.");
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

private void FillMultipleContainer(FeedbackData feedback, GameObject container)
{
    if (feedback == null || container == null)
    {
        Debug.LogWarning("[FeedbackRepository] FillMultipleContainer chiamato con parametri null.");
        return;
    }

    // 1️⃣ Crea lista ordinata di media
    var allMedia = new List<(string name, object content)>();

    if (feedback.images != null)
        allMedia.AddRange(feedback.images.Where(img => img != null).Select(img => (img.name, (object)img)));

    if (feedback.videos != null)
        allMedia.AddRange(feedback.videos.Where(vid => vid != null).Select(vid => (vid.name, (object)vid)));

    if (allMedia.Count == 0)
    {
        Debug.LogWarning($"[FeedbackRepository] Nessun contenuto multimediale valido per '{feedback.FeedbackName}'.");
        return;
    }

    allMedia = allMedia.OrderBy(m => m.name).ToList();

    // 2️⃣ Trova contenitori
    Transform contentParent = container.transform.Find("Canvas/Content");
    Transform navPanel = container.transform.Find("Nav Panel");

    if (navPanel == null)
    {
        Debug.LogError("[FeedbackRepository] 'Nav Panel' non trovati nel prefab multiplo.");
        return;
    }

    if (contentParent == null || navPanel == null)
    {
        Debug.LogError("[FeedbackRepository] 'Canvas/Content' non trovato nel prefab multiplo.");
        return;
    }



    Transform basePage = contentParent.Find("Page 1");
    Transform baseNavItem = navPanel.Find("Nav Item Toggle 1");
    if (basePage == null || baseNavItem == null)
    {
        Debug.LogError("[FeedbackRepository] 'Page 1' o 'Nav Item Toggle 1' non trovati nel prefab multiplo.");
        return;
    }

    var headerText = container.transform.Find("Canvas/Header Text")?.GetComponent<TMPro.TMP_Text>();
    if (headerText != null)
        headerText.text = feedback.FeedbackName;

    // 5️⃣ Pulisci precedenti
    for (int i = contentParent.childCount - 1; i >= 0; i--)
    {
        var child = contentParent.GetChild(i);
        if (child.name.StartsWith("Page") && child != basePage)
            Destroy(child.gameObject);
    }

    for (int i = navPanel.childCount - 1; i >= 0; i--)
    {
        var child = navPanel.GetChild(i);
        if (child.name.StartsWith("Nav Item Toggle") && child != baseNavItem)
            Destroy(child.gameObject);
    }

    // 6️⃣ Crea dinamicamente le pagine
    for (int i = 0; i < allMedia.Count; i++)
    {
        Transform page;
        Transform navItem;

        if (i == 0)
        {
            page = basePage;
            navItem = baseNavItem;
        }
        else
        {
            page = Instantiate(basePage, contentParent);
            page.name = $"Page {i + 1}";
            page.SetAsLastSibling();

            navItem = Instantiate(baseNavItem, navPanel);
            navItem.name = $"Nav Item Toggle {i + 1}";
            navItem.SetAsLastSibling();
        }

        // Aggiorna testo del toggle
        var textComp = navItem.GetComponentInChildren<TMPro.TMP_Text>();
        if (textComp != null)
            textComp.text = (i == 0) ? "Introduzione" : $"Step {i}";

        // Disattiva contenitori inutilizzati
        Transform imageContainer = page.Find("Image Container");
        Transform videoContainer = page.Find("Video Container");
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        Image imageComponent = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();
        VideoPlayer videoPlayer = videoContainer?.GetComponentInChildren<VideoPlayer>();

        var media = allMedia[i].content;

        if (media is Sprite sprite)
        {
            if (imageContainer != null && imageComponent != null)
            {
                imageContainer.gameObject.SetActive(true);
                imageComponent.sprite = sprite;
            }
        }
        else if (media is VideoClip clip)
        {
            if (videoContainer != null && videoPlayer != null)
            {
                videoContainer.gameObject.SetActive(true);
                videoPlayer.clip = clip;
                videoPlayer.Play();
            }
        }

        if (page.TryGetComponent(out RectTransform pageRect))
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
    }

    // 8️⃣ Aggiorna layout
    Canvas.ForceUpdateCanvases();

    var linker = container.GetComponentInChildren<PageToggleLinkerIndexed>();
    if (linker != null)
    {
        linker.RefreshLists();

        // 🔧 Collega dinamicamente i Toggle all’evento corretto
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
    }
    else
    {
        Debug.LogWarning("[FeedbackRepository] Nessun PageToggleLinkerIndexed trovato nel prefab.");
    }

    // 9️⃣ Attiva solo prima pagina e toggle
    for (int i = 0; i < contentParent.childCount; i++)
        contentParent.GetChild(i).gameObject.SetActive(i == 0);

    for (int i = 0; i < navPanel.childCount; i++)
    {
        var toggleObj = navPanel.GetChild(i).GetComponent<Toggle>();
        if (toggleObj != null)
            toggleObj.isOn = (i == 0);
    }
}


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
