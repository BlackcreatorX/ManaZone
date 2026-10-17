
using UnityEngine;

public class CounterParry : MonoBehaviour, IParryResponse
{
    public void OnParryResult(ParryResult result)
    {
        switch (result)
        {
            case ParryResult.Parried:
                OnSuccessfulParry();
                break;

            case ParryResult.NotParried:
                OnFailedParry();
                break;
        }
    }

    private void OnSuccessfulParry()
    {
        Debug.Log("¡Parry exitoso! Ejecutar contraataque.");

        // Aquí podremos crear el contrahechizo.
    }

    private void OnFailedParry()
    {
        Debug.Log("Parry fallido.");

        // Aquí podremos añadir una penalización.
    }
}
