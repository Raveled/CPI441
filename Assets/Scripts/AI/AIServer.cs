using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AIServer : MonoBehaviour {
    private string url = "http://localhost:3000/get-key";
    public string ApiKey { get; private set; }

    [System.Serializable]
    public class KeyResponse {
        public string apiKey;
    }

    public void FetchApiKey() {
        StartCoroutine(GetKeyCoroutine());
    }

    IEnumerator GetKeyCoroutine() {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            KeyResponse response = JsonUtility.FromJson<KeyResponse>(request.downloadHandler.text);
            ApiKey = response.apiKey;
            GetComponent<UnityAndGeminiV3>().ApiKeyResponse(ApiKey);
            //Debug.Log("api key: " + ApiKey);
        } else {
            Debug.LogError("Error: " + request.error);
        }
    }
}