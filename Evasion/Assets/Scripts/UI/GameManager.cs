using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Bolt.EntityBehaviour<IGameManager>
{
    #region Game Flow

    [SerializeField] private TextMeshProUGUI _instructionsText;
    private Dictionary<int, string> _instructions;
    public bool AllowGameOver = true;
    [SerializeField] private TextMeshProUGUI _activateGameOverFeedback;
    [SerializeField] private DetectPlayerInOffice _endLevelArea;
    [SerializeField] private DetectPlayerInOffice _officeArea;
    private int _nbPlayersInEndArea;

    public void SetCurrentInstructionIdx(int i)
    {
        if (entity.isOwner)
        {
            state.CurrentInstructionIdx = i;
        }
    }

    public void DecreaseNbAlivePlayers()
    {
        if (entity.isOwner)
        {
            --state.NbAlivePlayers;
        }
    }

    public void PlayerInOffice(DetectPlayerInOffice trigger)
    {
        trigger.GetComponent<MeshCollider>().enabled = false;
        SetCurrentInstructionIdx(2);
    }

    #endregion

    public override void Attached()
    {
        _officeArea.OnPlayerEnter += PlayerInOffice;
        _endLevelArea.OnPlayerEnter += PlayerInEndArea;
        _nbPlayersInEndArea = 0;
        if (entity.isOwner)
        {
            state.NbAlivePlayers = 2;
        }

        SetCurrentInstructionIdx(1);
        _instructions = new Dictionary<int, string>();
        _instructions.Add(1, "Reach the meeting room.");
        _instructions.Add(2, "Kill the target.");
        _instructions.Add(3, "Escape the base.");
    }

    void Update()
    {
        Pause();
        if (entity.isAttached)
        {
            _instructionsText.text = "Objective: " + _instructions[state.CurrentInstructionIdx];
            if (_nbPlayersInEndArea == state.NbAlivePlayers)
                StartCoroutine(EndMissionAfterDelay());
        }
    }

    public void ToggleCheat()
    {
        if (AllowGameOver)
        {
            AllowGameOver = false;
            _activateGameOverFeedback.text = "Deactivated Game Over. You can't die.";
        }
        else
        {
            AllowGameOver = true;
            _activateGameOverFeedback.text = "Activated Game Over. You can die.";
        }

        StartCoroutine(DeactivateText());
    }

    IEnumerator DeactivateText()
    {
        yield return new WaitForSeconds(5);
        _activateGameOverFeedback.text = "";
    }

    #region Pause

    [SerializeField] private GameObject _pauseUI;

    private void Pause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_pauseUI.activeInHierarchy)
            {
                // Already paused, we want to resume
                _pauseUI.SetActive(false);
            }
            else
            {
                // Not paused yet
                //Time.timeScale = 0; // Not allowed by Bolt :(
                _pauseUI.SetActive(true);
            }
        }
    }

    #endregion

    #region Success

    [SerializeField] private GameObject _successPanel;

    public void PlayerInEndArea(DetectPlayerInOffice trigger)
    {
        ++_nbPlayersInEndArea;
    }

    IEnumerator EndMissionAfterDelay()
    {
        yield return new WaitForSeconds(3);
        _successPanel.SetActive(true);
    }

    public void ExitToMenu()
    {
        if (BoltNetwork.IsServer)
        {
            foreach (var connection in BoltNetwork.Connections)
            {
                connection.Disconnect();
            }
        }
        BoltNetwork.Shutdown();
        SceneManager.LoadSceneAsync("Menu");
    }

    #endregion

    #region GameOver

    [SerializeField] private GameObject _gameOverPanel;

    public void GameOver()
    {
        _gameOverPanel.SetActive(true);
    }

    #endregion

}
