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

    public List<Transform> FindFeedbackPositionChild(GameObject parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("[FeedbackAutoManager] FindFeedbackPositionChild chiamato con parent nullo.");
            return null;
        }

        List<Transform> children = new List<Transform>();

        Transform feedbackPosition = parent.transform.Find("feedbackPosition");

        if (feedbackPosition.childCount > 0)
        {
            foreach (Transform child in feedbackPosition)
            {
                if (child != feedbackPosition) { children.Add(child); }
            }
        }
        else
        {
            children.Add(feedbackPosition);
        }

        if (children != null)
        {
            List<Transform> temps = new List<Transform>();
            foreach (Transform child in children)
            {
                //Debug.Log($"[FeedbackAutoManager] Figlio 'feedbackPosition' trovato in '{parent.name}'.");

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

                temps.Add(temp);
            }

            return temps;
        }
        else
        {
            Debug.LogWarning($"[FeedbackAutoManager] Nessun figlio 'feedbackPosition' trovato in '{parent.name}'.");
            return null;
        }
    }


    public void ChooseFeedback(FeedbackData feedback, List<Transform> positions, FeedbackSetHolder holder)
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

        foreach (Transform position in positions)
        {

            // ======= Caso speciale: prefab personalizzato =======
            if (feedback.PersonalizedPrefab != null)
            {
                GameObject customInstance = Instantiate(feedback.PersonalizedPrefab, position.position, position.rotation);
                customInstance.name = $"Feedback_Custom_{feedback.FeedbackName}";
                holder.activeFeedbackInstance = customInstance;

                //Debug.Log($"[FeedbackDisplayer] Istanza prefab personalizzato per feedback '{feedback.FeedbackName}'.");
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

            // ======= Verifica pagine =======
            if (feedback.pages == null || feedback.pages.Count == 0)
            {
                Debug.LogWarning($"[FeedbackDisplayer] Il feedback '{feedback.FeedbackName}' non contiene pagine.");
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

            // ======= Istanzia il container corretto in base al numero di pagine =======
            GameObject containerInstance;

            if (feedback.pages.Count == 1)
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

            //Debug.Log($"[FeedbackDisplayer] Mostrato feedback '{feedback.FeedbackName}' con {feedback.pages.Count} pagina/e.");
        }
    }



    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly string SINGLE_PATH_TITLE_TEXT = "Canvas/Title Text";
    private readonly string SINGLE_PATH_BODY_TEXT = "Canvas/Scrollable/Content/Body Text";
    private readonly string SINGLE_PATH_IMAGE_CONTAINER = "Canvas/Scrollable/Content/Image Container";
    private readonly string SINGLE_PATH_VIDEO_CONTAINER = "Canvas/Scrollable/Content/Video Container";
    private readonly string SINGLE_PATH_LAYOUT_PARENT = "Canvas";

    // ======= Metodo aggiornato =======
    private void FillSingleContainer(FeedbackData feedback, GameObject container)
    {
        if (feedback == null || container == null)
        {
            Debug.LogWarning("[FeedbackRepository] FillSingleContainer chiamato con parametri null.");
            return;
        }

        if (feedback.pages == null || feedback.pages.Count == 0)
        {
            Debug.LogWarning($"[FeedbackRepository] Il feedback '{feedback.FeedbackName}' non contiene pagine.");
            return;
        }

        var page = feedback.pages[0];

        // ======= Imposta titolo =======
        var headerText = container.transform.Find(SINGLE_PATH_TITLE_TEXT)?.GetComponent<TMPro.TMP_Text>();
        if (headerText != null)
            headerText.text = feedback.FeedbackName;
        else
            Debug.LogWarning($"[FeedbackRepository] '{SINGLE_PATH_TITLE_TEXT}' non trovato nel prefab.");

        // ======= Trova riferimenti =======
        Transform bodyTextTransform = container.transform.Find(SINGLE_PATH_BODY_TEXT);
        var bodyText = bodyTextTransform?.GetComponent<TMPro.TMP_Text>();

        Transform imageContainer = container.transform.Find(SINGLE_PATH_IMAGE_CONTAINER);
        Transform videoContainer = container.transform.Find(SINGLE_PATH_VIDEO_CONTAINER);
        VideoPlayer videoPlayer = container.GetComponentInChildren<VideoPlayer>();
        UnityEngine.UI.Image imageComponent = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();

        // Disattiva tutto
        if (bodyTextTransform != null) bodyTextTransform.gameObject.SetActive(false);
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        // ======= Validazione: immagine + video =======
        bool hasImage = page.image != null;
        bool hasVideo = page.video != null;

        if (hasImage && hasVideo)
        {
            Debug.LogError($"[FeedbackRepository] La pagina del feedback '{feedback.FeedbackName}' contiene sia un'immagine che un video. " +
                           "Ogni pagina deve contenere solo uno dei due.");
            return;
        }

        // ======= Testo =======
        if (bodyTextTransform != null && bodyText != null)
        {
            if (!string.IsNullOrEmpty(page.text))
            {
                bodyTextTransform.gameObject.SetActive(true);
                bodyText.text = page.text;
            }
            else
            {
                bodyTextTransform.gameObject.SetActive(false);
            }
        }

        // ======= Immagine =======
        if (hasImage)
        {
            if (imageContainer != null && imageComponent != null)
            {
                imageContainer.gameObject.SetActive(true);
                imageComponent.sprite = page.image;
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] '{SINGLE_PATH_IMAGE_CONTAINER}' o immagine non trovati nel prefab.");
            }
        }

        // ======= Video =======
        if (hasVideo)
        {
            if (videoContainer != null && videoPlayer != null)
            {
                videoContainer.gameObject.SetActive(true);
                videoPlayer.clip = page.video;
                videoPlayer.Play();
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] '{SINGLE_PATH_VIDEO_CONTAINER}' o VideoPlayer non trovati nel prefab.");
            }
        }

        // ======= Forza re-layout =======
        Transform layoutParent = container.transform.Find(SINGLE_PATH_LAYOUT_PARENT);
        if (layoutParent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layoutParent.GetComponent<RectTransform>());
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7
    private readonly string MULTI_PATH_CONTENT = "Layout/Canvas/Scrollable/Content";
    private readonly string MULTI_PATH_NAV_PANEL = "Layout/Nav Panel";
    private readonly string MULTI_PATH_HEADER_TEXT = "Layout/Canvas/Title Text";

    private readonly string MULTI_PATH_BODY_TEXT = "Body Text";
    private readonly string MULTI_PATH_IMAGE_CONTAINER = "Image Container";
    private readonly string MULTI_PATH_VIDEO_CONTAINER = "Video Container";

    // ===============================================================
    // MULTI-PAGE FEEDBACK
    // ===============================================================

    private void FillMultipleContainer(FeedbackData feedback, GameObject container)
    {
        if (feedback == null || container == null)
        {
            Debug.LogWarning("[FeedbackRepository] FillMultipleContainer chiamato con parametri null.");
            return;
        }

        if (feedback.pages == null || feedback.pages.Count == 0)
        {
            Debug.LogWarning($"[FeedbackRepository] Il feedback '{feedback.FeedbackName}' non contiene pagine.");
            return;
        }

        var contentParent = container.transform.Find(MULTI_PATH_CONTENT);
        var navPanel = container.transform.Find(MULTI_PATH_NAV_PANEL);

        if (contentParent == null || navPanel == null)
        {
            Debug.LogError("[FeedbackRepository] Struttura prefab non valida (manca Content o Nav Panel).");
            return;
        }

        var basePage = contentParent.Find("Page 1");
        var baseNavItem = navPanel.Find("Nav Item Toggle 1");

        if (basePage == null || baseNavItem == null)
        {
            Debug.LogError("[FeedbackRepository] Page 1 o Nav Item Toggle 1 non trovati nel prefab.");
            return;
        }

        SetHeader(container, feedback.FeedbackName);
        ClearOldPages(contentParent, basePage);
        ClearOldPages(navPanel, baseNavItem);

        CreatePages(feedback.pages, contentParent, navPanel, basePage, baseNavItem, feedback.FeedbackName);
        SetupNavigation(container, navPanel, contentParent);
        StartCoroutine(ForceLayoutNextFrame(contentParent));
    }

    // ===============================================================
    // SUPPORT METHODS
    // ===============================================================

    private void SetHeader(GameObject container, string title)
    {
        var header = container.transform.Find(MULTI_PATH_HEADER_TEXT)?.GetComponent<TMPro.TMP_Text>();
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

    private void CreatePages(List<FeedbackRepository.FeedbackPage> pages,
        Transform contentParent, Transform navPanel, Transform basePage, Transform baseNavItem, string feedbackName)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            var (page, navItem) = (i == 0)
                ? (basePage, baseNavItem)
                : (UnityEngine.Object.Instantiate(basePage, contentParent),
                   UnityEngine.Object.Instantiate(baseNavItem, navPanel));

            page.name = $"Page {i + 1}";
            navItem.name = $"Nav Item Toggle {i + 1}";

            var textComp = navItem.GetComponentInChildren<TMPro.TMP_Text>();
            if (textComp != null)
                textComp.text = (i == 0) ? "Start" : $"Step {i}";

            SetupPageContent(pages[i], page, feedbackName);
        }
    }

    private void SetupPageContent(FeedbackRepository.FeedbackPage pageData, Transform page, string feedbackName)
    {
        var bodyTextTransform = page.Find(MULTI_PATH_BODY_TEXT);
        var imageContainer = page.Find(MULTI_PATH_IMAGE_CONTAINER);
        var videoContainer = page.Find(MULTI_PATH_VIDEO_CONTAINER);

        var bodyText = bodyTextTransform?.GetComponent<TMPro.TMP_Text>();
        var image = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();
        var player = videoContainer?.GetComponentInChildren<VideoPlayer>();

        // ======= Disattiva tutto all'inizio =======
        if (bodyTextTransform != null) bodyTextTransform.gameObject.SetActive(false);
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        bool hasText = !string.IsNullOrEmpty(pageData.text);
        bool hasImage = pageData.image != null;
        bool hasVideo = pageData.video != null;

        // ======= Validazione: immagine + video non ammessi =======
        if (hasImage && hasVideo)
        {
            Debug.LogError($"[FeedbackRepository] La pagina del feedback '{feedbackName}' contiene sia un'immagine che un video. " +
                        "Ogni pagina deve contenere solo uno dei due.");
            return;
        }

        // ======= Testo =======
        if (bodyText != null && hasText)
        {
            bodyText.text = pageData.text;
            bodyTextTransform.gameObject.SetActive(true);
        }

        // ======= Immagine =======
        if (hasImage)
        {
            if (imageContainer != null && image != null)
            {
                image.sprite = pageData.image;
                imageContainer.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] Immagine presente ma '{MULTI_PATH_IMAGE_CONTAINER}' non trovato o mancante Image component.");
            }
        }

        // ======= Video =======
        if (hasVideo)
        {
            if (videoContainer != null && player != null)
            {
                player.clip = pageData.video;
                videoContainer.gameObject.SetActive(true);
                player.Play();
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] Video presente ma '{MULTI_PATH_VIDEO_CONTAINER}' o VideoPlayer non trovati.");
            }
        }

        // ======= Re-layout =======
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

        // Attiva solo la prima pagina
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

    public GameObject PrepareAndDisplayFeedback(FeedbackData feedback, List<Transform> feedbackPositions, FeedbackSetHolder holder)
    {
        if (feedback == null || holder == null)
        {
            Debug.LogWarning("[FeedbackRepository] Parametri non validi in PrepareAndDisplayFeedback.");
            return null;
        }

        // Istanzia e visualizza il feedback
        ChooseFeedback(feedback, feedbackPositions, holder);


        return holder.activeFeedbackInstance;
    }


    // 🔁 Coroutine per refreshare dopo un frame
    private IEnumerator DelayedRefresh(PageToggleLinkerIndexed linker)
    {
        yield return null; // Attendi che Unity completi l'instanziazione
        linker.RefreshLists();
        // Debug.Log("[FeedbackRepository] PageToggleLinkerIndexed aggiornato dopo la creazione dinamica delle pagine.");
    }

    private IEnumerator ForceLayoutNextFrame(Transform contentParent)
    {
        // Forza subito un refresh del canvas
        Canvas.ForceUpdateCanvases();

        // Attendi un frame per permettere a Unity di aggiornare la gerarchia UI
        yield return null;

        if (contentParent != null && contentParent.TryGetComponent(out RectTransform rect))
        {
            // Ricostruisce il layout solo dopo che le pagine sono effettivamente attive
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }
    }


}
