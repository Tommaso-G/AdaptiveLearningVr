using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject che raccoglie tutti i livelli offline disponibili.
/// Creane uno solo e assegnalo al GameManager.
/// Crea asset via: tasto destro → Create → OfflineMode → Level Registry
/// </summary>
[CreateAssetMenu(menuName = "OfflineMode/Level Registry", fileName = "OfflineLevelRegistry")]
public class OfflineLevelRegistry : ScriptableObject
{
    [Tooltip("Lista ordinata dei livelli offline. L'ordine determina quello dei bottoni nel menu.")]
    public List<OfflineLevelConfig> levels = new List<OfflineLevelConfig>();
}
