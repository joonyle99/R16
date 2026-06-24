using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class InGameUIController : MonoBehaviour, IGameStateListener<InGameState>
{
    public ComboPanel ComboPanel { get; private set; }
    public MissionPanel MissionPanel { get; private set; }
    public PursuerPanel PursuerPanel { get; private set; }
    public TouchBlockPanel TouchBlockPanel { get; private set; }
    public ResultPanel ResultPanel { get; private set; }
    public WaitPanel WaitPanel { get; private set; }

    private void OnDestroy()
    {

    }

    public void Initialize()
    {
        ComboPanel = GetComponentInChildren<ComboPanel>(true);
        ComboPanel.Initialize();
        MissionPanel = GetComponentInChildren<MissionPanel>(true);
        MissionPanel.Initialize();
        PursuerPanel = GetComponentInChildren<PursuerPanel>(true);
        PursuerPanel.Initialize();
        TouchBlockPanel = GetComponentInChildren<TouchBlockPanel>(true);
        ResultPanel = GetComponentInChildren<ResultPanel>(true);
        ResultPanel.Initialize();
        WaitPanel = GetComponentInChildren<WaitPanel>(true);

        ComboPanel.SetVisible(false);
        MissionPanel.SetVisible(false);
        PursuerPanel.SetVisible(false);
        TouchBlockPanel.SetVisible(false);
        ResultPanel.SetVisible(false);
        WaitPanel.SetVisible(false);
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        // TouchBlockPanel.SetVisible(currState == InGameState.Wait);
        // WaitPanel.SetVisible(currState == InGameState.Wait);
    }

    public void ShowResultPanel(GameResultData data)
    {
        TouchBlockPanel.SetVisible(true);
        ResultPanel.Show(data);
    }
}
