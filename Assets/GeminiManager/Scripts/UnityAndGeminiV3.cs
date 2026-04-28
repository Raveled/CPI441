using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;


public class UnityAndGeminiV3: MonoBehaviour
{
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;

    private string apiKey = ""; 
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"; // Edit it and choose your prefer model

    [Header("Prompt Function")]
    [TextArea(15, 20)]
    public string prompt = "";

    AIManager aiManager = null;

    private string url = "http://localhost:3000/get-key";

    

    private void Awake() {
        aiManager = GetComponent<AIManager>();
    }

    void Start()
    {
        //UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        //apiKey = jsonApiKey.key;  
        FetchApiKey();
    }
    //Called from AIManager
    public void SendNewMessage(string prompt) {
        this.prompt = prompt;
        if (prompt != "") { StartCoroutine(SendPromptRequestToGemini(prompt)); };
    }
    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        TextContent content = new TextContent
        {
            parts = new TextPart[] { new TextPart { text = promptText } }
        };

        var root = new
        {
            contents = new[] { content },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(root);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + www.downloadHandler.text);
                aiManager.ErrorReceived();
            } else
            {
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                string cleanJson = response.candidates[0].content.parts[0].text;

                Debug.Log("Balanced Stats Received: " + cleanJson);
                aiManager.ResponseReceived(cleanJson);
            }
        }
    }
    
    public void FetchApiKey() {
        StartCoroutine(GetKeyCoroutine());
    }

    IEnumerator GetKeyCoroutine() {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            KeyResponse response = JsonUtility.FromJson<KeyResponse>(request.downloadHandler.text);
            apiKey = response.apiKey;
            Debug.Log("api key is in: " + apiKey);
            //Debug.Log("api key: " + ApiKey);
        } else {
            Debug.LogError("Error: " + request.error);
        }
    }
}



