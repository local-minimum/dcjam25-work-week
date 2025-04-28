using UnityEngine;

public class BBRoom : MonoBehaviour
{
    [SerializeField]
    GameObject entry;

    [SerializeField]
    GameObject exit;

    [SerializeField]
    bool lower;

    public bool Lower => lower;

    const float TILE_SIZE = 1f;

    public void AlignEntryTo(BBRoom other)
    {
        var entryOffset = transform.position - entry.transform.position;

        transform.position = other.exit.transform.position 
            + Vector3.up * (lower ? -TILE_SIZE : TILE_SIZE) 
            + entryOffset;
    }

    public void TriggerSpawnNextRoom()
    {
        foreach (var collider in exit.GetComponentsInChildren<Collider2D>())
        {
            if (collider.isTrigger)
            {
                collider.enabled = false;
            }
        }

        BBRoomsManager.instance.GetSpawnConnectingRoom(this);
    }

    public void RestoreExit()
    {
        foreach (var collider in exit.GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = true;
        }
    }

    private void OnBecameInvisible()
    {
        gameObject.SetActive(false);
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"BBRoom: {entry.transform.position} -> {exit.transform.position}");
    }
}
