using UnityEngine;

public abstract class UIEntry<TData> : MonoBehaviour
{
    [SerializeField] private UIEntryState state = UIEntryState.Normal;

    public TData Data { get; private set; }
    public UIEntryState State => state;

    public void Bind(TData data)
    {
        Data = data;
        OnBind(data);
    }

    public virtual void SetState(UIEntryState newState)
    {
        state = newState;
    }

    protected abstract void OnBind(TData data);
}
