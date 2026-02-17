using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class JSONUpload : MonoBehaviour {
    [SerializeField] TextAsset matchDataJSON = null;
    [SerializeField] TextAsset entityDataJSON = null;
    GameManager gm = null;

    private void Start() {
        gm = GetComponent<GameManager>();
    }
    public void SendMatchData(string json) {
        StartCoroutine(SendJSON(json));
    }

    private IEnumerator SendJSON(string json) {





        var request = new UnityWebRequest(
            "https://your-server.com/match",
            "POST"
        );

        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        }
    }
    IEnumerator DownloadBalance() {
        UnityWebRequest req =
            UnityWebRequest.Get("https://your-server.com/balance");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            string json = req.downloadHandler.text;
            //ApplyEntityData(json);
        }
    }
}
