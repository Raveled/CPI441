using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    [SerializeField] private string gasURL;
    [SerializeField] private string prompt;

    private void Update() {
        if (Keyboard.current.oKey.wasPressedThisFrame) {
            StartCoroutine(SendDataToGAS());
        }
    }
    private IEnumerator SendDataToGAS() {
        WWWForm form = new WWWForm();
        form.AddField("parameter", prompt);
        UnityWebRequest www = UnityWebRequest.Post(gasURL, form);

        yield return www.SendWebRequest();
        string response = "";

        if(www.result == UnityWebRequest.Result.Success) {
            response = www.downloadHandler.text;
        } else {
            response = "ERR";
        }

        Debug.Log(response);
    }
}
