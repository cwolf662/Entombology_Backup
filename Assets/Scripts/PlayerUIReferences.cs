//Used to help reference UI objects, prefabs can't do that for scene stuff I realized

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIReferences : MonoBehaviour{
    [Header("Stamina")]
    public GameObject staminaBar;
    public Slider staminaSlider;
    public Image staminaFillImage;

    [Header("Health")]
    public Slider healthSlider;
    public Image healthFillImage;

    [Header("Bite Cooldown")]
    public GameObject biteCooldownBar;
    public Slider biteCooldownSlider;

    [Header("Special Attack")]
    public GameObject specialCooldownBar;
    public Slider specialCooldownSlider;
    public Image specialSymbol;

    [Header("Coins & Zombies")]
    public TMP_Text coinsText;
    public TMP_Text zombiesAliveText;
}
