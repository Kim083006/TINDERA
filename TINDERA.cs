using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TinderaGame : MonoBehaviour
{
    // ==========================
    //   DATA STRUCTURES
    // ==========================

    [System.Serializable]
    public class Food
    {
        public string name;
        public List<string> ingredients;

        public Food(string name, List<string> ingredients)
        {
            this.name = name;
            this.ingredients = ingredients;
        }
    }

    // --- Existing ---
    public ArrayList foodList = new ArrayList(); // ArrayList for foods
    public Dictionary<string, int> ingredients = new Dictionary<string, int>();
    public Stack<string> cookedFoodStack = new Stack<string>(); // Stack for cooked foods
    public Queue<Dictionary<string, string>> customerQueue = new Queue<Dictionary<string, string>>(); // Queue for customers
    public List<Dictionary<string, string>> servedCustomers = new List<Dictionary<string, string>>();

    // --- Added ---
    public LinkedList<string> deliveryRoute = new LinkedList<string>(); // LinkedList for delivery order
    public TreeNode foodCategoryTree; // Simple food category tree
    public Graph kitchenGraph = new Graph(); // Graph representing kitchen stations

    public int coins = 0;
    private string timeOfDay = "morning";
    private float timeChangeInterval = 30f;

    // ==========================
    //   UI + AUDIO REFERENCES
    // ==========================
    public Image bgImage;
    // public Light2D lighting; // Uncomment if using URP 2D light
    public Text labelTime;
    public Text labelCoins;
    public Text labelQueue;
    public Text labelNarration;

    public AudioSource sfxCook;
    public AudioSource sfxCoin;

    public GameObject customerPrefab;
    public Transform[] spawnPoints;
    private List<GameObject> activeCustomers = new List<GameObject>();

    void Start()
    {
        Debug.Log("Welcome to Tindera!");
        InitializeData();
        BuildFoodTree();
        BuildKitchenGraph();
        SetupDeliveryRoute();

        StartCoroutine(TimeCycle());
        SpawnCustomers();
        UpdateUI();
        UpdateBackground();
        Narrate("A new day begins at Aling Nena’s humble stall. The scent of breakfast fills the air.");
    }

    // ==========================
    //   INITIAL SETUP
    // ==========================
    void InitializeData()
    {
        // Add food to ArrayList
        foodList.Add(new Food("Potato Fries", new List<string> { "potato" }));
        foodList.Add(new Food("Hotdog Bun", new List<string> { "hotdog", "bun" }));
        foodList.Add(new Food("Siomai Rice", new List<string> { "rice" }));
        foodList.Add(new Food("Gulaman", new List<string> { "jelly" }));
        foodList.Add(new Food("Buko Juice", new List<string> { "buko" }));

        // Initialize ingredients
        ingredients["potato"] = 5;
        ingredients["hotdog"] = 5;
        ingredients["bun"] = 5;
        ingredients["rice"] = 5;
        ingredients["jelly"] = 5;
        ingredients["buko"] = 5;
    }

    // ==========================
    //   LINKEDLIST (Delivery Route)
    // ==========================
    void SetupDeliveryRoute()
    {
        deliveryRoute.AddLast("Storage");
        deliveryRoute.AddLast("Cooking Station");
        deliveryRoute.AddLast("Counter");
        deliveryRoute.AddLast("Customer Area");

        Debug.Log("🚚 Delivery route created using LinkedList:");
        foreach (var loc in deliveryRoute)
            Debug.Log("- " + loc);
    }

    // ==========================
    //   TREE (Food Categories)
    // ==========================
    void BuildFoodTree()
    {
        foodCategoryTree = new TreeNode("Menu");
        var fried = new TreeNode("Fried");
        var drinks = new TreeNode("Drinks");

        fried.AddChild(new TreeNode("Potato Fries"));
        fried.AddChild(new TreeNode("Hotdog Bun"));
        drinks.AddChild(new TreeNode("Buko Juice"));
        drinks.AddChild(new TreeNode("Gulaman"));

        foodCategoryTree.AddChild(fried);
        foodCategoryTree.AddChild(drinks);

        Debug.Log("🌳 Food Category Tree built:");
        foodCategoryTree.PrintTree();
    }

    // ==========================
    //   GRAPH (Kitchen Connections)
    // ==========================
    void BuildKitchenGraph()
    {
        kitchenGraph.AddEdge("Storage", "Cooking Station");
        kitchenGraph.AddEdge("Cooking Station", "Counter");
        kitchenGraph.AddEdge("Counter", "Customer Area");
        kitchenGraph.AddEdge("Storage", "Trash Bin");

        Debug.Log("🧭 Kitchen Graph connections:");
        kitchenGraph.PrintGraph();
    }

    // ==========================
    //   TIME CONTROL SYSTEM
    // ==========================
    IEnumerator TimeCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeChangeInterval);
            switch (timeOfDay)
            {
                case "morning":
                    timeOfDay = "noon";
                    Narrate("The sun rises high—students rush in, craving their favorite snacks!");
                    break;
                case "noon":
                    timeOfDay = "night";
                    Narrate("Evening settles in; the lights flicker as the last customers arrive.");
                    break;
                case "night":
                    timeOfDay = "morning";
                    Narrate("A new day begins again with fresh smiles and sizzling pans.");
                    break;
            }
            UpdateBackground();
            Debug.Log("⏰ Time changed to: " + timeOfDay);
        }
    }

    void UpdateBackground()
    {
        if (labelTime != null)
            labelTime.text = "Time: " + char.ToUpper(timeOfDay[0]) + timeOfDay.Substring(1);
    }

    // ==========================
    //   CUSTOMER SYSTEM
    // ==========================
    void SpawnCustomers()
    {
        customerQueue.Clear();
        foreach (var c in activeCustomers) Destroy(c);
        activeCustomers.Clear();

        for (int i = 0; i < 3; i++)
        {
            string randomFood = RandomFood();
            var customer = new Dictionary<string, string>
            {
                { "name", "Customer_" + (i + 1) },
                { "order", randomFood }
            };
            customerQueue.Enqueue(customer);

            if (spawnPoints.Length > i && customerPrefab != null)
            {
                GameObject newCustomer = Instantiate(customerPrefab, spawnPoints[i].position, Quaternion.identity);
                newCustomer.name = customer["name"];
                activeCustomers.Add(newCustomer);
            }
        }
        Narrate("Three hungry customers appear, ready to place their orders.");
        UpdateUI();
    }

    public void ServeCustomer()
    {
        if (customerQueue.Count == 0)
        {
            Narrate("No customers right now... Maybe time to rest or restock.");
            return;
        }

        var customer = customerQueue.Dequeue();
        string order = customer["order"];
        if (CanCook(order))
        {
            Cook(order);
            cookedFoodStack.Push(order);
            coins += 5;
            servedCustomers.Add(customer);
            Narrate(customer["name"] + " enjoys their " + order + ".");
        }
        else
        {
            Narrate("Oops! Missing ingredients for " + order + ".");
        }

        if (customerQueue.Count == 0) StartCoroutine(RespawnDelay());
        UpdateUI();
    }

    IEnumerator RespawnDelay()
    {
        yield return new WaitForSeconds(2f);
        SpawnCustomers();
    }

    // ==========================
    //   FOOD SYSTEM
    // ==========================
    string RandomFood()
    {
        int index = Random.Range(0, foodList.Count);
        Food food = (Food)foodList[index];
        return food.name;
    }

    bool CanCook(string foodName)
    {
        foreach (Food food in foodList)
        {
            if (food.name == foodName)
            {
                foreach (string ingredient in food.ingredients)
                {
                    if (!ingredients.ContainsKey(ingredient) || ingredients[ingredient] <= 0)
                        return false;
                }
                return true;
            }
        }
        return false;
    }

    void Cook(string foodName)
    {
        foreach (Food food in foodList)
        {
            if (food.name == foodName)
            {
                foreach (string ingredient in food.ingredients)
                    ingredients[ingredient] = Mathf.Max(ingredients[ingredient] - 1, 0);

                if (sfxCook != null) sfxCook.Play();
                Narrate("Cooking " + foodName + "... The aroma fills the air!");
                return;
            }
        }
    }

    // ==========================
    //   UI + DEBUG
    // ==========================
    void UpdateUI()
    {
        if (labelCoins != null) labelCoins.text = "Coins: " + coins;
        if (labelQueue != null) labelQueue.text = "Customers: " + customerQueue.Count;
    }

    void Narrate(string text)
    {
        if (labelNarration != null) labelNarration.text = text;
        Debug.Log("[Narration] " + text);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) ServeCustomer();
        if (Input.GetKeyDown(KeyCode.Space)) ShowCookedStack();
    }

    void ShowCookedStack()
    {
        Debug.Log("🍳 Cooked Food Stack:");
        foreach (var food in cookedFoodStack)
            Debug.Log("- " + food);
    }
}

// ==========================
//   TREE STRUCTURE
// ==========================
public class TreeNode
{
    public string value;
    public List<TreeNode> children = new List<TreeNode>();

    public TreeNode(string value) { this.value = value; }

    public void AddChild(TreeNode child) => children.Add(child);

    public void PrintTree(string indent = "")
    {
        Debug.Log(indent + "• " + value);
        foreach (var child in children)
            child.PrintTree(indent + "   ");
    }
}

// ==========================
//   GRAPH STRUCTURE
// ==========================
public class Graph
{
    private Dictionary<string, List<string>> adjList = new Dictionary<string, List<string>>();

    public void AddEdge(string from, string to)
    {
        if (!adjList.ContainsKey(from)) adjList[from] = new List<string>();
        if (!adjList.ContainsKey(to)) adjList[to] = new List<string>();
        adjList[from].Add(to);
    }

    public void PrintGraph()
    {
        foreach (var node in adjList)
        {
            string connections = string.Join(", ", node.Value);
            Debug.Log("🔗 " + node.Key + " -> " + connections);
        }
    }
}
