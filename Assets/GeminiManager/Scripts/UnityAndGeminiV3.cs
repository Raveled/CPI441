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

    [Header("ChatBot Function")]
    public TMP_InputField inputField;
    public TMP_Text uiText;
    public string botInstructions;
    private TextContent[] chatHistory;


    [Header("Prompt Function")]
    [TextArea(15, 20)]
    public string prompt = "";

    AIManager aiManager = null;

    private void Awake() {
        aiManager = GetComponent<AIManager>();
    }

    void Start()
    {
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;   
        chatHistory = new TextContent[] { };
    }
    //Called from AIManager
    public void SendNewMessage(string prompt) {
        this.prompt = prompt;
        if (prompt != "") { StartCoroutine(SendPromptRequestToGemini(prompt)); };
    }
    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        // Construct the object structure to ensure valid JSON escaping
        TextContent content = new TextContent
        {
            parts = new TextPart[] { new TextPart { text = promptText } }
        };

        // We use a wrapper to include the 'generationConfig' for strict JSON mode
        // Note: You may need to add this class to your serializable list at the top
        var root = new
        {
            contents = new[] { content },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(root);
        // Pro-tip: Use Newtonsoft.Json if possible for complex nested objects, 
        // otherwise stick to your manual string if you prefer, but remove the extra {}.

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
            } else
            {
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                string cleanJson = response.candidates[0].content.parts[0].text;

                // This is your pure JSON data
                Debug.Log("Balanced Stats Received: " + cleanJson);
                aiManager.ResponseReceived(cleanJson);
            }
        }
    }

    public void SendChat()
    {
        string userMessage = inputField.text;
        StartCoroutine( SendChatRequestToGemini(userMessage));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage)
    {

        string url = $"{apiEndpoint}?key={apiKey}";
     
        TextContent userContent = new TextContent
        {
            role = "user",
            parts = new TextPart[]
            {
                new TextPart { text = newMessage }
            }
        };

        TextContent instruction = new TextContent
        {
            parts = new TextPart[]
            {
                new TextPart {text = botInstructions}
            }
        }; 

        List<TextContent> contentsList = new List<TextContent>(chatHistory);
        contentsList.Add(userContent);
        chatHistory = contentsList.ToArray(); 

        ChatRequest chatRequest = new ChatRequest { contents = chatHistory, system_instruction = instruction };

        string jsonData = JsonUtility.ToJson(chatRequest);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string reply = response.candidates[0].content.parts[0].text;
                        TextContent botContent = new TextContent
                        {
                            role = "model",
                            parts = new TextPart[]
                            {
                                new TextPart { text = reply }
                            }
                        };

                        Debug.Log(reply);
                        //This part shows the text in the Canvas
                        uiText.text = reply;
                        //This part adds the response to the chat history, for your next message
                        contentsList.Add(botContent);
                        chatHistory = contentsList.ToArray();
                    }
                else
                {
                    Debug.Log("No text found.");
                }
             }
        }  
    }
}



