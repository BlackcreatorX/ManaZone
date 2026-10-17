using UnityEngine;

public class estuneableplacehodder : MonoBehaviour, IStunnable
{
public void Stun()
    {
        Debug.Log($"{name} está aturdido.");
    }
}
