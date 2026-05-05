using UnityEngine;
using System;

public interface IDestructible
{
    event Action OnDestroyed;
}
