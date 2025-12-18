using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public long enemyID;
    public int enemyType;
    public int maxHealth;
    public int currentHealth;

    [Header("UI")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    private Renderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponentInChildren<Renderer>();
        UpdateHealthUI();
        SetEnemyAppearance();
    }

    public void Initialize(long id, int type, Vector3 pos, Quaternion rot, int maxHp, int currentHp)
    {
        enemyID = id;
        enemyType = type;
        maxHealth = maxHp;
        currentHealth = currentHp;

        transform.position = pos;
        transform.rotation = rot;

        gameObject.name = $"Enemy_{id}_{GetEnemyTypeName(type)}";

        Debug.Log($"[Enemy] Initialized: ID={id}, Type={GetEnemyTypeName(type)}, HP={currentHp}/{maxHp}");
    }

    public void UpdatePosition(Vector3 pos, Quaternion rot)
    {
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }

    public void TakeDamage(int damage, int remainingHp)
    {
        if (maxHealth <= 0) maxHealth = Mathf.Max(maxHealth, remainingHp);

        currentHealth = Mathf.Clamp(remainingHp, 0, maxHealth);
        UpdateHealthUI();

        StartCoroutine(FlashRed());

        Debug.Log($"[Enemy {enemyID}] Took {damage} damage! Remaining: {currentHealth}/{maxHealth}");
    }

    public void Die()
    {
        Debug.Log($"[Enemy {enemyID}] Died!");
        Destroy(gameObject, 2f);
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    void SetEnemyAppearance()
    {
        if (meshRenderer != null)
        {
            switch (enemyType)
            {
                case 1: // Slime
                    meshRenderer.material.color = Color.green;
                    break;
                case 2: // Goblin
                    meshRenderer.material.color = new Color(0.5f, 0.3f, 0.1f);
                    break;
                case 3: // Wolf
                    meshRenderer.material.color = Color.gray;
                    break;
            }
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (meshRenderer != null)
        {
            Color original = meshRenderer.material.color;
            meshRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            meshRenderer.material.color = original;
        }
    }

    string GetEnemyTypeName(int type)
    {
        switch (type)
        {
            case 1: return "Slime";
            case 2: return "Goblin";
            case 3: return "Wolf";
            default: return "Unknown";
        }
    }

    void LateUpdate()
    {
        // 체력바가 항상 카메라를 향하도록
        if (healthBar != null && Camera.main != null)
        {
            healthBar.transform.LookAt(Camera.main.transform);
            healthBar.transform.Rotate(0, 180, 0);
        }
    }
}