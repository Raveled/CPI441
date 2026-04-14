using UnityEngine;
using UnityEngine.SceneManagement;

public class GoNextScene : MonoBehaviour
{
    public void Start()
    {
        SceneManager.LoadScene(0);
    }
}
