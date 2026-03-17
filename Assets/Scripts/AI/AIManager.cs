using UnityEngine;
using UnityEngine.InputSystem;

public class AIManager : MonoBehaviour
{
    //Setup
    [Header("Setup")]
    [SerializeField] JSON_MatchData matchData = null;
    [SerializeField] JSON_EntityData entityData = null;
    //UnityAndGeminiV3 ai = null;

    //Prompt
    [Header("Prompt")]
    [TextArea(15, 20)]
    [SerializeField] string promptPart1 = "";
    [TextArea(15, 20)]
    [SerializeField] string promptPart2 = "";

    //AI Prompt
    string prompt = "";
    [Header("Reponse Debug")]
    [TextArea(15, 20)]
    [SerializeField]string response = "";
    private void Awake() {
        //ai = GetComponent<UnityAndGeminiV3>();
    }
    private void Update() {
        //Testing Prompt Creation
        if (Keyboard.current.bKey.wasPressedThisFrame) {
            CreatePrompt();
            Debug.Log(prompt);
        }
        //Testing Message Send
        if (Keyboard.current.oKey.wasPressedThisFrame) {
            Debug.Log("button pressed");
            CreatePrompt();
            //ai.SendNewMessage();
        }
    }
    //Concatenate the mesage to send to ai
    void CreatePrompt() {
        //prompt1 > \n > matchdata > \n > prompt2 > \n > entitydata
        prompt = promptPart1 + "\n" + matchData.GetJSONString() + "\n\n" + 
                promptPart2 + "\n" + entityData.GetJSONString();
    }

    //Called from gamemanager after a match
    public void AskAIForBalance() {
        CreatePrompt();
        //ai.SendNewMessage(prompt);
    }
    //Called from UnityAndGeminiV3 for the ai's reponse
    public void ResponseReceived(string response) {
        this.response = response;
        Debug.Log("AIManager.cs - Response Recieved");

        //TODO-------------------------------------------------------------------------------------
        //send to gamemanager to change the entity stats
        //
    }


}
