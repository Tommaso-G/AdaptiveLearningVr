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
using UnityEditor.Overlays;

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

        Debug.Log($"[FeedbackAutoManager] Chiamato FindFeedbackPositiononChild per '{parent.name}'.");


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
                GameObject tempGO = new GameObject($"Temp_{child.name}");
                Transform temp = tempGO.transform;

                temp.position = child.position;

                Vector3 euler = child.eulerAngles;
                temp.rotation = Quaternion.Euler(0f, euler.y, 0f);

                temp.localScale = child.lossyScale;

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

    // ======= Versione sincrona mantenuta per compatibilità =======
    public GameObject PrepareAndDisplayFeedback(FeedbackData feedback, List<Transform> feedbackPositions, FeedbackSetHolder holder)
    {
        if (feedback == null || holder == null)
        {
            Debug.LogWarning("[FeedbackRepository] Parametri non validi in PrepareAndDisplayFeedback.");
            return null;
        }

        ChooseFeedback(feedback, feedbackPositions, holder);
        return holder.activeFeedbackInstance;
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

        var profile = FindFirstObjectByType<LearningProfile>();


        foreach (Transform position in positions)
        {
            if (feedback.PersonalizedPrefab != null)
            {
                GameObject customInstance = Instantiate(feedback.PersonalizedPrefab, position.position, position.rotation);
                customInstance.name = $"Feedback_Custom_{feedback.FeedbackName}";

                var ctrl = customInstance.GetComponent<FeedbackPrefabController>();
                if (ctrl != null){ 
                    ctrl.needsButtonToBeCompleted =  feedback.needsButtonToBeCompleted;
                    ctrl.applyReflectiveEffects = feedback.applyReflectiveEffects;
            }

                holder.activeFeedbackInstance = customInstance;
                return;
            }

            var activeRepo = holder.ProfilingFeedbackRepository != null
                ? holder.ProfilingFeedbackRepository as ScriptableObject
                : holder.FeedbackRepository as ScriptableObject;

            if (activeRepo == null)
            {
                Debug.LogError("[FeedbackDisplayer] Nessun repository valido trovato nel holder.");
                return;
            }

            if (feedback.pages == null || feedback.pages.Count == 0)
            {
                Debug.LogWarning($"[FeedbackDisplayer] Il feedback '{feedback.FeedbackName}' non contiene pagine.");
                return;
            }

            GameObject singlePrefab = null;
            GameObject multiplePrefab = null;

            bool isActive = profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;

            if (holder.ProfilingFeedbackRepository != null)
            {
                var repo = holder.ProfilingFeedbackRepository;
                singlePrefab = repo.SingleContainer;
                multiplePrefab = repo.MultipleContainer;
            }
            else
            {
                var repo = holder.FeedbackRepository;
                singlePrefab = isActive ? repo.SingleContainer_Attivo : repo.SingleContainer_Riflessivo;
                multiplePrefab = repo.MultipleContainer;
            }

            if (singlePrefab == null || multiplePrefab == null)
            {
                Debug.LogError("[FeedbackDisplayer] Prefab SingleContainer o MultipleContainer non assegnato nel repository attivo.");
                return;
            }

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

            var containerCtrl = containerInstance.GetComponent<FeedbackPrefabController>();
            if (containerCtrl != null){ 
                containerCtrl.needsButtonToBeCompleted =  feedback.needsButtonToBeCompleted;
                containerCtrl.applyReflectiveEffects = feedback.applyReflectiveEffects;
            }

            holder.activeFeedbackInstance = containerInstance;
        }
    }

    private readonly string SINGLE_PATH_TITLE_TEXT = "Canvas/Title Text";
    private readonly string SINGLE_PATH_BODY_TEXT = "Canvas/Scrollable/Content/Body Text";
    private readonly string SINGLE_PATH_IMAGE_CONTAINER = "Canvas/Scrollable/Content/Image Container";
    private readonly string SINGLE_PATH_VIDEO_CONTAINER = "Canvas/Scrollable/Content/Video Container";
    private readonly string SINGLE_PATH_LAYOUT_PARENT = "Canvas";
    private readonly string SINGLE_PATH_SCROLLABLE = "Canvas/Scrollable";

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

        var headerText = container.transform.Find(SINGLE_PATH_TITLE_TEXT)?.GetComponent<TMPro.TMP_Text>();
        if (headerText != null)
            headerText.text = feedback.FeedbackName;
        else
            Debug.LogWarning($"[FeedbackRepository] '{SINGLE_PATH_TITLE_TEXT}' non trovato nel prefab.");

        Transform bodyTextTransform = container.transform.Find(SINGLE_PATH_BODY_TEXT);
        var bodyText = bodyTextTransform?.GetComponent<TMPro.TMP_Text>();

        Transform imageContainer = container.transform.Find(SINGLE_PATH_IMAGE_CONTAINER);
        Transform videoContainer = container.transform.Find(SINGLE_PATH_VIDEO_CONTAINER);
        Transform scrollableContainer = container.transform.Find(SINGLE_PATH_SCROLLABLE);

        VideoPlayer videoPlayer = container.GetComponentInChildren<VideoPlayer>(true);
        UnityEngine.UI.Image imageComponent = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();

        if (bodyTextTransform != null) bodyTextTransform.gameObject.SetActive(false);
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        SlideData slideData = scrollableContainer.GetComponent<SlideData>();
        slideData.setLearningEnums(page.Sequenzale_Globale, page.Visivo_Verbale);
        slideData.setIntrodactoryField(page.isIntroductory);

        bool hasImage = page.image != null;
        bool hasVideo = page.video != null;

        if (hasImage && hasVideo)
        {
            Debug.LogError($"[FeedbackRepository] La pagina del feedback '{feedback.FeedbackName}' contiene sia un'immagine che un video.");
            return;
        }

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

        if (hasVideo)
        {
            if (videoContainer != null && videoPlayer != null)
            {
                if (videoContainer != null && videoPlayer != null)
                {
                    videoContainer.gameObject.SetActive(true);
                    AssignUniqueRenderTexture(videoPlayer, container, container);

                    videoPlayer.clip = page.video;
                    videoPlayer.Play();
                }
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] '{SINGLE_PATH_VIDEO_CONTAINER}' o VideoPlayer non trovati nel prefab.");
            }
        }

        Transform layoutParent = container.transform.Find(SINGLE_PATH_LAYOUT_PARENT);
        if (layoutParent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layoutParent.GetComponent<RectTransform>());
    }

    private readonly string MULTI_PATH_CONTENT = "Layout/Canvas/Scrollable/Content";
    private readonly string MULTI_PATH_NAV_PANEL = "Layout/Nav Panel";
    private readonly string MULTI_PATH_HEADER_TEXT = "Layout/Canvas/Title Text";
    private readonly string MULTI_PATH_BODY_TEXT = "Body Text";
    private readonly string MULTI_PATH_IMAGE_CONTAINER = "Image Container";
    private readonly string MULTI_PATH_VIDEO_CONTAINER = "Video Container";

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

        bool useGlobalOverviewLabels = feedbackHolder.ProfilingFeedbackRepository != null;

        CreatePages(feedback.pages, contentParent, navPanel, basePage, baseNavItem, feedback.FeedbackName, useGlobalOverviewLabels, container);
        SetupNavigation(container, navPanel, contentParent);
        StartCoroutine(ForceLayoutNextFrame(contentParent));
    }

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
        Transform contentParent, Transform navPanel, Transform basePage, Transform baseNavItem,
        string feedbackName, bool useGlobalOverviewLabels = false, GameObject container = null)
    {
        // Se stiamo usando il ProfilingFeedbackRepository, mescoliamo le pagine dalla seconda in poi
        List<FeedbackRepository.FeedbackPage> orderedPages = new List<FeedbackRepository.FeedbackPage>(pages);

        if (useGlobalOverviewLabels && orderedPages.Count > 1)
        {
            var intro = orderedPages[0];
            var rest = orderedPages.Skip(1).ToList();

            // Fisher-Yates shuffle
            for (int n = rest.Count - 1; n > 0; n--)
            {
                int k = UnityEngine.Random.Range(0, n + 1);
                (rest[k], rest[n]) = (rest[n], rest[k]);
            }

            orderedPages = new List<FeedbackRepository.FeedbackPage> { intro };
            orderedPages.AddRange(rest);
        }

        for (int i = 0; i < orderedPages.Count; i++)
        {
            var (page, navItem) = (i == 0)
                ? (basePage, baseNavItem)
                : (UnityEngine.Object.Instantiate(basePage, contentParent),
                UnityEngine.Object.Instantiate(baseNavItem, navPanel));

            page.name = $"Page {i + 1}";
            navItem.name = $"Nav Item Toggle {i + 1}";

            var textComp = navItem.GetComponentInChildren<TMPro.TMP_Text>();
            if (textComp != null)
            {
                if (i == 0)
                    textComp.text = useGlobalOverviewLabels ? "INTRO" : "Start";
                else if (useGlobalOverviewLabels && orderedPages[i].Visivo_Verbale == LearningEnums.VisivoVerbale.Visivo)
                    textComp.text = "VIDEO";
                else if (useGlobalOverviewLabels && orderedPages[i].Sequenzale_Globale == LearningEnums.SequenzialeGlobale.Globale)
                    textComp.text = "OVERVIEW";
                else if (useGlobalOverviewLabels && orderedPages[i].Sequenzale_Globale == LearningEnums.SequenzialeGlobale.Sequenziale)
                    textComp.text = $"STEPS";    
                else textComp.text = $"Page {i +1}";
            }

            SetupPageContent(orderedPages[i], page, feedbackName, container);
        }
    }

    private void SetupPageContent(FeedbackRepository.FeedbackPage pageData, Transform page, string feedbackName, GameObject container)
    {
        var bodyTextTransform = page.Find(MULTI_PATH_BODY_TEXT);
        var imageContainer = page.Find(MULTI_PATH_IMAGE_CONTAINER);
        var videoContainer = page.Find(MULTI_PATH_VIDEO_CONTAINER);

        SlideData slideData = page.GetComponent<SlideData>();
        slideData.setLearningEnums(pageData.Sequenzale_Globale, pageData.Visivo_Verbale);
        slideData.setIntrodactoryField(pageData.isIntroductory);

        var bodyText = bodyTextTransform?.GetComponent<TMPro.TMP_Text>();
        var image = imageContainer?.GetComponentInChildren<UnityEngine.UI.Image>();
        var player = container.GetComponentInChildren<VideoPlayer>(true); // ← cerca nel prefab root

        if (bodyTextTransform != null) bodyTextTransform.gameObject.SetActive(false);
        if (imageContainer != null) imageContainer.gameObject.SetActive(false);
        if (videoContainer != null) videoContainer.gameObject.SetActive(false);

        bool hasText = !string.IsNullOrEmpty(pageData.text);
        bool hasImage = pageData.image != null;
        bool hasVideo = pageData.video != null;

        if (hasImage && hasVideo)
        {
            Debug.LogError($"[FeedbackRepository] La pagina del feedback '{feedbackName}' contiene sia un'immagine che un video.");
            return;
        }

        if (bodyText != null && hasText)
        {
            bodyText.text = pageData.text;
            bodyTextTransform.gameObject.SetActive(true);
        }

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

        if (hasVideo)
        {
            if (videoContainer != null && player != null)
            {
                videoContainer.gameObject.SetActive(true);
                AssignUniqueRenderTexture(player, page.gameObject, container);
                player.clip = pageData.video;
                player.Play();
            }
            else
            {
                Debug.LogWarning($"[FeedbackRepository] Video presente ma '{MULTI_PATH_VIDEO_CONTAINER}' o VideoPlayer non trovati.");
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

        for (int i = 0; i < contentParent.childCount; i++)
            contentParent.GetChild(i).gameObject.SetActive(i == 0);

        for (int i = 0; i < navPanel.childCount; i++)
        {
            var toggle = navPanel.GetChild(i).GetComponent<Toggle>();
            if (toggle != null)
                toggle.isOn = (i == 0);
        }
    }

    private void AssignUniqueRenderTexture(VideoPlayer player, GameObject scopeObject, GameObject rootContainer)
    {
        if (player == null) return;

        RenderTexture rt = new RenderTexture(1920, 1080, 0);
        rt.name = $"RT_{scopeObject.name}_{System.Guid.NewGuid()}";
        rt.Create();

        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;

        var rawImage = scopeObject.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
        if (rawImage != null)
            rawImage.texture = rt;

        // Registra la RT nel controller per la pulizia
        var ctrl = rootContainer.GetComponent<FeedbackPrefabController>();
        if (ctrl != null)
            ctrl.RegisterRuntimeRenderTexture(rt);
        else
            Debug.LogWarning("[FeedbackDisplayer] FeedbackPrefabController non trovato sul root.");
    }

    private IEnumerator DelayedRefresh(PageToggleLinkerIndexed linker)
    {
        yield return null;
        linker.RefreshLists();
    }

    private IEnumerator ForceLayoutNextFrame(Transform contentParent)
    {
        Canvas.ForceUpdateCanvases();
        yield return null;

        if (contentParent != null && contentParent.TryGetComponent(out RectTransform rect))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }
    }
}