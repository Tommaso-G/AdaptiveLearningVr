using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PageToggleLinkerIndexed : MonoBehaviour
{
    [Header("Contenitore dei Toggle (genitore di tutti i Toggle)")]
    public Transform toggleContainer;

    [Header("Contenitore delle pagine (genitore di tutte le Pagine)")]
    public Transform pageContainer;

    private List<Toggle> toggles = new List<Toggle>();
    private List<GameObject> pages = new List<GameObject>();

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

        Debug.Log($"[PageToggleLinkerIndexed] Caricati {toggles.Count} toggle e {pages.Count} pagine.");
    }

    /// <summary>
    /// Metodo da collegare direttamente all'evento OnValueChanged (bool) del Toggle.
    /// </summary>
public void OnToggleValueChanged(Toggle changedToggle, bool isOn)
{
    if (!isOn)
        return;

    if (changedToggle == null)
    {
        Debug.LogWarning("[PageToggleLinkerIndexed] Toggle nullo passato all'evento.");
        return;
    }

    if (toggles.Count == 0 || pages.Count == 0)
        RefreshLists();

    int index = toggles.IndexOf(changedToggle);
    if (index < 0 || index >= pages.Count)
    {
        Debug.LogWarning($"[PageToggleLinkerIndexed] Nessuna pagina trovata per il toggle {changedToggle.name}");
        return;
    }

    // Attiva solo la pagina corrispondente
    for (int i = 0; i < pages.Count; i++)
        pages[i].SetActive(i == index);

    Debug.Log($"[PageToggleLinkerIndexed] Attivata pagina {pages[index].name} per toggle {changedToggle.name}");
}

}
