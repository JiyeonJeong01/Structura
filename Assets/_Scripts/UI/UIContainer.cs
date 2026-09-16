using System.Collections.Generic;
using UnityEngine;

public abstract class UIContainer<TData> : MonoBehaviour
{
    public abstract void Refresh(IReadOnlyList<TData> data);
    public abstract void Clear();
}
