
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SpellCaster : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("References")]
    [SerializeField] private Transform castPoint;

    [Header("Equipped Spells")]
    [SerializeField] private SpellSlot offensiveSpell;
    [SerializeField] private DefensiveSpellSlot defensiveSpell;
    [SerializeField] private SpellSlot controlSpell;

    [Header("Mana")]
    [SerializeField] private ManaSystem manaSystem;

    [Header("UI")]
    [SerializeField] private TMP_Text spellSelectionText;

    private InputAction select1;
    private InputAction select2;
    private InputAction select3;
    private InputAction castAction;

    private GameObject activeShield;
    private bool isShieldActive;

    private int selectedSpell = 1;
    private readonly float[] cooldownTimers = new float[3];

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Falta asignar Input Actions Asset.", this);
            return;
        }

        InputActionMap map = inputActions.FindActionMap("Player");

        if (map == null)
        {
            Debug.LogError("No existe el Action Map 'Player'.", this);
            return;
        }

        select1 = map.FindAction("SelectSpell1");
        select2 = map.FindAction("SelectSpell2");
        select3 = map.FindAction("SelectSpell3");
        castAction = map.FindAction("CastSpell");
    }

    private void OnEnable()
    {
        select1?.Enable();
        select2?.Enable();
        select3?.Enable();
        castAction?.Enable();
    }

    private void OnDisable()
    {
        select1?.Disable();
        select2?.Disable();
        select3?.Disable();
        castAction?.Disable();
    }

   
private void Update()
{
    for (int i = 0; i < cooldownTimers.Length; i++)
    {
        cooldownTimers[i] =
            Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);
    }

    if (select1 != null && select1.WasPressedThisFrame())
        SelectSpell(1);

    if (select2 != null && select2.WasPressedThisFrame())
        SelectSpell(2);

    if (select3 != null && select3.WasPressedThisFrame())
        SelectSpell(3);

    if (castAction != null && castAction.WasPressedThisFrame())
        CastSelectedSpell();

    if (spellSelectionText != null)
        spellSelectionText.text =
            $"Hechizo seleccionado: {selectedSpell}";

    UpdateShieldMana();
}


    private void SelectSpell(int spellNumber)
    {
        selectedSpell = spellNumber;
        Debug.Log("Hechizo seleccionado: " + selectedSpell);
    }

    private void CastSelectedSpell()
    {
        int index = selectedSpell - 1;

        if (index < 0 || index >= cooldownTimers.Length)
            return;

        if (cooldownTimers[index] > 0f)
        {
            Debug.Log("Hechizo en cooldown.");
            return;
        }

        switch (selectedSpell)
        {
            case 1:
                if (offensiveSpell == null)
                    return;

                LaunchProjectile(
                    offensiveSpell.prefab,
                    offensiveSpell.cooldown,
                    offensiveSpell.manaCost,
                    index
                );
                break;

            case 2:
                if (defensiveSpell == null)
                    return;

                ActivateShield(index);
                break;

            case 3:
                if (controlSpell == null)
                    return;

                LaunchProjectile(
                    controlSpell.prefab,
                    controlSpell.cooldown,
                    controlSpell.manaCost,
                    index
                );
                break;
        }
    }

    
private void LaunchProjectile(
    GameObject prefab,
    float cooldown,
    float manaCost,
    int index)
{
    if (prefab == null || castPoint == null)
    {
        Debug.LogWarning("Falta asignar el prefab o el Cast Point.");
        return;
    }

    if (manaSystem == null)
    {
        Debug.LogError("Falta asignar ManaSystem.");
        return;
    }

    if (!manaSystem.TrySpendMana(manaCost))
    {
        Debug.Log("No tienes suficiente maná.");
        return;
    }

    GameObject projectile = Instantiate(
        prefab,
        castPoint.position,
        castPoint.rotation
    );

    Vector3 direction = castPoint.forward;

    // Comprobar si es un proyectil de control.
    CCProjectile ccProjectile =
        projectile.GetComponent<CCProjectile>();

    if (ccProjectile != null)
    {
        ccProjectile.Launch(direction);
    }
    else
    {
        // Si no es CC, comprobar si es el proyectil normal.
        SpellProjectile spellProjectile =
            projectile.GetComponent<SpellProjectile>();

        if (spellProjectile != null)
        {
            spellProjectile.Launch(direction);
        }
        else
        {
            Debug.LogError(
                "El prefab no tiene CCProjectile ni SpellProjectile.",
                projectile
            );

            Destroy(projectile);
            manaSystem.RestoreMana(manaCost);
            return;
        }
    }

    cooldownTimers[index] = cooldown;
}


    private void ActivateShield(int index)
    {
        if (defensiveSpell.defensiveMode == DefensiveMode.Shield)
            ActivateShieldMode(index);
        else
            ActivateParryMode(index);
    }

    private void ActivateShieldMode(int index)
    {
        if (isShieldActive)
        {
            DeactivateShield();
            return;
        }

        if (defensiveSpell.prefab == null || manaSystem == null)
            return;

        if (manaSystem.CurrentMana <= 0f)
        {
            Debug.Log("No tienes maná para activar el escudo.");
            return;
        }

        activeShield = Instantiate(
            defensiveSpell.prefab,
            transform.position,
            transform.rotation,
            transform
        );

        activeShield.transform.localPosition = Vector3.up;
        activeShield.transform.localRotation = Quaternion.identity;

        ShieldEffect shieldEffect = activeShield.GetComponent<ShieldEffect>();
        if (shieldEffect != null)
        {
            shieldEffect.Activate(
                manaSystem,
                defensiveSpell.manaRecovery
            );
        }
        else
        {
            Debug.LogError("El prefab del escudo necesita ShieldEffect.", activeShield);
        }

        isShieldActive = true;
        manaSystem.RegenerationEnabled = false;
        cooldownTimers[index] = defensiveSpell.cooldown;

        Debug.Log("Escudo activado.");
    }

    private void DeactivateShield()
    {
        isShieldActive = false;

        if (manaSystem != null)
            manaSystem.RegenerationEnabled = true;

        if (activeShield != null)
            Destroy(activeShield);

        activeShield = null;
        Debug.Log("Escudo desactivado.");
    }

    private void ActivateParryMode(int index)
    {
        if (defensiveSpell.prefab == null || manaSystem == null)
            return;

        if (!manaSystem.TrySpendMana(defensiveSpell.manaCost))
        {
            Debug.Log("No tienes suficiente maná para hacer parry.");
            return;
        }

        GameObject parryObject = Instantiate(
            defensiveSpell.prefab,
            transform.position,
            transform.rotation,
            transform
        );

        parryObject.transform.localPosition = Vector3.up;
        parryObject.transform.localRotation = Quaternion.identity;

        ParryEffect parryEffect =
            parryObject.GetComponent<ParryEffect>();

        if (parryEffect == null)
        {
            Debug.LogError(
                "El prefab de parry necesita ParryEffect."
            );

            Destroy(parryObject);
            manaSystem.RestoreMana(defensiveSpell.manaCost);
            return;
        }

        parryEffect.Activate(
            defensiveSpell.parryWindow,
            manaSystem,
            defensiveSpell.manaRecovery
        );
        cooldownTimers[index] = defensiveSpell.cooldown;
    }


private void UpdateShieldMana()
{
    if (!isShieldActive)
        return;

    if (manaSystem == null)
    {
        Debug.LogError("No hay ManaSystem asignado.");
        DeactivateShield();
        return;
    }

    // Coste por segundo convertido al coste de este frame.
    float manaCost =
        defensiveSpell.manaCost * Time.deltaTime;

    // Si no queda suficiente maná, desactivar el escudo.
    if (manaSystem.CurrentMana <= 0f)
    {
        Debug.Log("¡Maná agotado! Escudo desactivado.");
        DeactivateShield();
        return;
    }

    // Si no puede pagar el coste, desactivar el escudo.
    if (!manaSystem.TrySpendMana(manaCost))
    {
        Debug.Log("¡Maná insuficiente! Escudo desactivado.");
        DeactivateShield();
        return;
    }
}


    private void OnDestroy()
    {
        if (manaSystem != null)
            manaSystem.RegenerationEnabled = true;

        if (activeShield != null)
            Destroy(activeShield);
    }
}
