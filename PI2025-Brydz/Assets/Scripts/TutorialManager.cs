using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public Sprite image;
    }

    public TutorialStep[] steps;

    public Image tutorialImage;
    public Button nextButton;
    public Button backButton;
    public Button exitButton;

    private int currentStep = 0;

    void Start()
    {
        ShowStep(0);
        nextButton.onClick.AddListener(NextStep);
        backButton.onClick.AddListener(PreviousStep);
        exitButton.onClick.AddListener(ExitTutorial);
    }

    void ShowStep(int index)
    {
        currentStep = index;
        tutorialImage.sprite = steps[index].image;

        backButton.interactable = (index > 0);
        nextButton.interactable = (index < steps.Length - 1);
    }

    void NextStep()
    {
        if (currentStep < steps.Length - 1)
            ShowStep(currentStep + 1);
    }

    void PreviousStep()
    {
        if (currentStep > 0)
            ShowStep(currentStep - 1);
    }

    void ExitTutorial()
    {
        SceneManager.LoadScene("StartScene");
        gameObject.SetActive(false);
    }
}
