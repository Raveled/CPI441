using PurrNet;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
[System.Serializable]
public class UnityAndGeminiKey {
    public string key;
}

// Text-only part
[System.Serializable]
public class TextPart {
    public string text;
}

[System.Serializable]
public class TextContent {
    public string role;
    public TextPart[] parts;
}

[System.Serializable]
public class TextCandidate {
    public TextContent content;
}

[System.Serializable]
public class TextResponse {
    public TextCandidate[] candidates;
}

[System.Serializable]
public class KeyResponse {
    public string apiKey;
}
public class AIManager : MonoBehaviour {
    //Setup
    [Header("Setup")]
    [SerializeField] GameManager gameManager;
    [SerializeField] bool ai_enabled = true;
    [SerializeField] JSON_MatchData matchData = null;
    [SerializeField] JSON_EntityData entityData = null;
    public bool connectedToServer = false;

    //Prompt
    [Header("Prompt")]
    [TextArea(15, 20)]
    [SerializeField] string promptPart1 = "";
    [TextArea(2, 2)]
    [SerializeField] string promptPart2 = "";

    //AI Prompt
    string prompt = "";
    [Header("Reponse Debug")]
    [TextArea(15, 20)]
    [SerializeField] string response = "";

    //Gemini
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;
    private string apiKey = "";
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"; // Edit it and choose your prefer model
    private string url = "http://localhost:3000/get-key";


    //Concatenate the mesage to send to ai
    void CreatePrompt() {
        //prompt1 > \n > matchdata > \n > prompt2 > \n > entitydata
        prompt = promptPart1 + "\n" + matchData.GetJSONString() + "\n\n" +
                promptPart2 + "\n" + entityData.GetJSONString();
    }

    //Called from gamemanager after a match
    public void AskAIForBalance() {
        CreatePrompt();
        if (ai_enabled) SendNewMessage(prompt);
        else ResponseReceived("");
    }
    public void ResponseReceived(string response) {
        Debug.Log("AI BALANCING DONE");

        if (ai_enabled) {
            this.response = response;

            entityData.LoadFromJSONString(response);
            entityData.SaveToJSON();
        }

        gameManager.SendToLobby();
    }
    public void ErrorReceived() {
        Debug.Log("AI ERROR RECEIVED");
        gameManager.SendToLobby();
    }

    public void SendNewMessage(string prompt) {
        if (!connectedToServer) {
            Debug.Log("NOT CONNECTED TO SERVER");
            ErrorReceived();
            return;
        }
        this.prompt = prompt;
        if (prompt != "") { StartCoroutine(SendPromptRequestToGemini(prompt)); };
    }
    private IEnumerator SendPromptRequestToGemini(string promptText) {
        string url = $"{apiEndpoint}?key={apiKey}";

        TextContent content = new TextContent {
            parts = new TextPart[] { new TextPart { text = promptText } }
        };

        var root = new {
            contents = new[] { content },
            generationConfig = new {
                response_mime_type = "application/json"
            }
        };

        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(root);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST")) {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError("API Error: " + www.downloadHandler.text);
                ErrorReceived();
            } else {
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                string cleanJson = response.candidates[0].content.parts[0].text;

                Debug.Log("Balanced Stats Received: " + cleanJson);
                ResponseReceived(cleanJson);
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
            connectedToServer = true;
            Debug.Log("api key is in: " + apiKey);
            //Debug.Log("api key: " + ApiKey);
        } else {
            Debug.LogError("Error: " + request.error);
            connectedToServer = false;
        }
    }
}
