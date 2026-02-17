using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PageToggleLinkerIndexed : MonoBehaviour
{
    [Header("Contenitore dei Toggle (genitore di tutti i Toggle)")]
    public Transform toggleContainer;

    [Header("Contenitore delle pagine (genitore di tutte le Pagine)")]
    public RectTransform pageContainer;

    private List<Toggle> toggles = new List<Toggle>();
    private List<GameObject> pages = new List<GameObject>();

    [Header("Scroll rect della pagina")]
    public ScrollRect scrollable;  

    public Scrollbar verticalScrollbar;

    private void Awake()
    {
        RefreshLists();
    }

    public void RefreshLists()
    {
        toggles.Clear();
        pages.Clear();

        if (toggleContainer != null)
        {
            foreach (Transform t in toggleContainer)
            {
                Toggle toggle = t.GetComponent<Toggle>();
                if (toggle != null)
                    toggles.Add(toggle);
            }
        }

        if (pageContainer != null)
        {
            foreach (Transform p in pageContainer)
                pages.Add(p.gameObject);
        }

        //Debug.Log($"[PageToggleLinkerIndexed] Caricati {toggles.Count} toggle e {pages.Count} pagine.");
    }



    public void OnToggleValueChanged(Toggle changedToggle, bool isOn)
    {
        if (!isOn) return;
        if (changedToggle == null) return;

        if (toggles.Count == 0 || pages.Count == 0)
            RefreshLists();

        int index = toggles.IndexOf(changedToggle);
        if (index < 0 || index >= pages.Count) return;

        // Attiva solo la pagina corrispondente
        for (int i = 0; i < pages.Count; i++)
            pages[i].SetActive(i == index);

        // Coroutine per rebuild posticipato
        StartCoroutine(ForceLayoutNextFrame(pages[index]));
        

    }

    private IEnumerator ForceLayoutNextFrame(GameObject page)
    {
        yield return null; // aspetta 1 frame
        if (page != null && page.TryGetComponent(out RectTransform rect))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();

            // ======== Reset scroll usando il riferimento pubblico ========
            if (scrollable != null)
            {
                scrollable.verticalNormalizedPosition = 1f;   // in alto
                scrollable.horizontalNormalizedPosition = 0f; // a sinistra
            }
            UpdateScrollStateAfterPageChange();
        }
    }

    private void UpdateScrollStateAfterPageChange()
        {
        if (scrollable == null || pageContainer == null)
        {
            Debug.LogWarning("[FeedbackScrollableController] ScrollRect o Content non assegnati.");
            return;
        }

        float contentHeight = pageContainer.rect.height;
        float viewportHeight = scrollable.viewport != null
            ? scrollable.viewport.rect.height
            : scrollable.GetComponent<RectTransform>().rect.height;

        bool needsScroll = contentHeight > viewportHeight + 10f;

        scrollable.enabled = needsScroll;
        if (verticalScrollbar != null)
            verticalScrollbar.gameObject.SetActive(needsScroll);

    }

}
