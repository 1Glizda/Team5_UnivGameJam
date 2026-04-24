using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        SoundManager.PlayMusic(MusicType.BACKGROUNDMUSIC);
    }

    public void OnPlayButton()
    {
        SceneManager.LoadScene("Level1-new");
    }
}