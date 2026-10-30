using TMPro;
using UnityEngine;

public class PlayerCoins : MonoBehaviour {
    [Header("Coins")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private TMP_Text coinsText;

    private static int sharedCoins;
    private static bool initialized;

    public int CurrentCoins => sharedCoins;

    private void Awake() {
        if (!initialized) {
            sharedCoins = startingCoins;
            initialized = true;
        }

        UpdateCoinsUI();
    }

    //Lets scripts update coin text
    public void ConfigureCoinsUI(TMP_Text text) {
        coinsText = text;
        UpdateCoinsUI();
    }

    //Lets zombie scripts give coins on death
    public void AddCoins(int amount){
        if (amount <= 0)
            return;

        sharedCoins += amount;
        UpdateCoinsUI();
    }

    //Lets the shop take coins
    public bool SpendCoins(int amount)
    {
        if (amount < 0 || sharedCoins < amount)
            return false;

        sharedCoins -= amount;
        UpdateCoinsUI();
        return true;
    }

    public static int GetSharedCoins() {return sharedCoins;}

    public static bool TrySpendSharedCoins(int amount) {
        if (amount < 0 || sharedCoins < amount)
            return false;

        sharedCoins -= amount;
        return true;
    }

    public static void SetSharedCoins(int amount) {
        sharedCoins = Mathf.Max(0, amount);
        initialized = true;
    }

    private void UpdateCoinsUI() {
        if (coinsText != null)
            coinsText.text = "Coins: " + sharedCoins;
    }
}