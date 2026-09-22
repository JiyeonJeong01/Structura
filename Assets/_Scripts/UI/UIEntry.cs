using UnityEngine;
using UnityEngine.Serialization;

public abstract class UIEntry<TData> : MonoBehaviour
{
    public TData Data { get; private set; }
    public UIEntryState State => _state;

    [FormerlySerializedAs("state"), SerializeField] private UIEntryState _state = UIEntryState.Normal;


    public void Bind(TData data)
    {
        Data = data;
        OnBind(data);
    }

    public virtual void SetState(UIEntryState newState)
    {
        _state = newState;
    }

    protected abstract void OnBind(TData data);
}
