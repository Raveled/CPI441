using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

// Text-only part
[System.Serializable]
public class TextPart
{
    public string text;
}

[System.Serializable]
public class TextContent
{
    public string role;
    public TextPart[] parts;
}

[System.Serializable]
public class TextCandidate
{
    public TextContent content;
}

[System.Serializable]
public class TextResponse
{
    public TextCandidate[] candidates;
}

// For text requests
[System.Serializable]
public class ChatRequest
{
    public TextContent[] contents;
    public TextContent system_instruction;
}


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
    AIServer aiServer = null;

    private void Awake() {
        aiManager = GetComponent<AIManager>();
        aiServer = GetComponent<AIServer>();
    }

    void Start()
    {
        //UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        //apiKey = jsonApiKey.key;  
        aiServer.FetchApiKey();
    }
    public void ApiKeyResponse(string key) {
        apiKey = aiServer.ApiKey;
        Debug.Log("api key is in: " + apiKey);
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
}



