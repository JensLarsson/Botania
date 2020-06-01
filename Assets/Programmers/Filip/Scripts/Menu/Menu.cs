﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;

public class Menu : MonoBehaviour
{
    const string MENU_BUTTON = "Cancel";

    [Header("Settings")]


    [SerializeField] GameObject _menu;
    [SerializeField] GameObject _main;
    [SerializeField] GameObject[] _rest;

    [SerializeField] MenuItemHover[] _changeNames;
    [SerializeField] GameObject _logo;

    CHARACTER_CONTROL_STATE _previousState;

    bool _onStart = true;

    bool _mainMenu = true;

    void Update()
    {
        if ((_onStart || Input.GetButtonDown(MENU_BUTTON)) && _menu != null)
        {
            _onStart = false;

            if (CharacterState.Control_State != CHARACTER_CONTROL_STATE.MENU_NO_MOVEMENT && CharacterState.Control_State != CHARACTER_CONTROL_STATE.MENU)
            {
                EnterMenu();
            }
            else if (_menu.activeSelf)
            {//If menu object is active, turn it off and set characterState to previousState
                ExitMenu();
            }
        }
    }

    public static void Save()
    {
        float masterVol, musicVol, SFXVol;
        RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_MASTER).getVolume(out masterVol);
        RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_MUSIC).getVolume(out musicVol);
        RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_SFX).getVolume(out SFXVol);


        OptionsSave saveData = new OptionsSave
            (masterVol, musicVol, SFXVol,
             Screen.currentResolution,
             Screen.fullScreen, QualitySettings.GetQualityLevel(),
             QualitySettings.masterTextureLimit, TerrainGenerator.RenderDistanceIndex,
             Player.GetPlayerCamera().fieldOfView);

        Serialization.Save(Saving.FileNames.SETTINGS, saveData);
    }

    public static void Load()
    {
        OptionsSave saveData = (OptionsSave)Serialization.Load(Saving.FileNames.SETTINGS);

        if (saveData != null)
        {
            RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_MASTER).setVolume(saveData._masterVol);
            RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_MUSIC).setVolume(saveData._musicVol);
            RuntimeManager.GetBus(AudioSettings.BUS_START_PREFIX + AudioSettings.BUS_SFX).setVolume(saveData._SFXVol);

            QualitySettings.SetQualityLevel(saveData._graphicsQuality);
            QualitySettings.masterTextureLimit = saveData._textureDetail;
            TerrainGenerator.SetRenderDistanceOnStart(saveData._renderDistance);

            Player.GetPlayerCamera().fieldOfView = saveData._FOV;

            Screen.SetResolution(saveData._resolutionWidth, saveData._resolutionHeight, saveData._fullScreen);
        }
    }


    public void EnterMenu()
    {
        ResetMenu();
        _previousState = CharacterState.Control_State;
        CharacterState.SetControlState(CHARACTER_CONTROL_STATE.MENU_NO_MOVEMENT);
        _menu.SetActive(true);
        Music_Manager.Instance.Play_OptionsMusic();
    }

    public void ExitMenu()
    {
        CharacterState.SetControlState(_previousState);
        _menu.SetActive(false);
        ResetMenu();

        if (_mainMenu)
        {
            _mainMenu = false;
            for (int i = 0; i < _changeNames.Length; i++)
            {
                _changeNames[i].ChangeName();
            }

            _logo.SetActive(false);
        }

        if (Music_Manager.Instance.StartMenu)
            Music_Manager.Instance.Stop_OptionsMusic();
    }



    private void ResetMenu()
    {
        _main.SetActive(true);

        for (int i = 0; i < _rest.Length; i++)
        {
            _rest[i].SetActive(false);
        }
    }

    public void Quit()
    {
        SaveSystem.SaveStatic();
        Application.Quit();
    }

    public void Restart()
    {
        Destroy(_menu);
        OnStartFadeIn.FadeOut();
        StartCoroutine(RestartWithFade());
    }

    IEnumerator RestartWithFade()
    {
        while (!OnStartFadeIn.Done())
        {
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
}



[System.Serializable]
public class OptionsSave
{
    public float _masterVol;
    public float _musicVol;
    public float _SFXVol;

    public int _resolutionWidth;
    public int _resolutionHeight;
    public bool _fullScreen;
    public int _graphicsQuality;
    public int _textureDetail;
    public int _renderDistance;

    public float _FOV;

    public OptionsSave(float masterVol, float musicVol, float SFXVol, Resolution resolution, bool fullScreen, int graphicsQuality, int textureDetail, int renderDistance, float FOV)
    {
        this._masterVol = masterVol;
        this._musicVol = musicVol;
        this._SFXVol = SFXVol;

        this._resolutionWidth = resolution.width;
        this._resolutionHeight = resolution.height;
        this._fullScreen = fullScreen;
        this._graphicsQuality = graphicsQuality;
        this._textureDetail = textureDetail;
        this._renderDistance = renderDistance;

        this._FOV = FOV;
    }
}
