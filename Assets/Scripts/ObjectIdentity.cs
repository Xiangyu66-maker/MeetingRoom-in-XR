using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Object Identity")]
public sealed class ObjectIdentity : MonoBehaviour
{
    [SerializeField] private string object_id;
    [SerializeField] private string category;
    [TextArea]
    [SerializeField] private string description;

    public string ObjectId => object_id;
    public string Category => category;
    public string Description => description;

    public void SetObjectId(string value)
    {
        object_id = value;
    }

    public void SetMetadataIfEmpty(string categoryValue, string descriptionValue)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            category = categoryValue;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = descriptionValue;
        }
    }

    private void Reset()
    {
        object_id = gameObject.name;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(object_id))
        {
            object_id = gameObject.name;
        }
    }
}
