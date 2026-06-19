using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.OSX;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;

public class InputCheck : MonoBehaviour
{
    public enum InputController
    {
        Xbox,
        Nintendo,
        PS,
        Keyboard,
        None
    }

    private InputController inputController;
    private InputDevice lastDevice;
    private IDisposable listener;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputController = InputController.None;
    }

    private void OnEnable()
    {
        listener = InputSystem.onAnyButtonPress.Call(ctrl =>
        {
            lastDevice = ctrl.device;

            if (lastDevice is Keyboard)
            {
                Debug.Log("�L�[�{�[�h�ő���");
                inputController = InputController.Keyboard;
            }
            else if (lastDevice is Mouse)
            {
                Debug.Log("�}�E�X�ő���");
            }
            else if (lastDevice is DualShockGamepad)
            {
                Debug.Log("PS�R���g���[���[�ő���");
                inputController = InputController.PS;
            }
            else if (lastDevice is XInputController)
            {
                Debug.Log("Xbox�R���g���[���[�ő���");
                inputController = InputController.Xbox;
            }
            else if (lastDevice is Gamepad gamepad)
            {
                if (gamepad.displayName.Contains("Pro Controller") ||
                    gamepad.displayName.Contains("Nintendo") ||
                    gamepad.name.Contains("Switch"))
                {
                    Debug.Log("Nintendo�n�R���g���[���[");
                    inputController = InputController.Nintendo;
                }
            }
            else if (lastDevice is Gamepad)
            {
                Debug.Log("���̑��̃Q�[���p�b�h�ő���");
                inputController = InputController.PS;
            }
        });
    }

    private void OnDisable()
    {
        listener?.Dispose();
    }

    public InputController GetInputController()
    {
        return inputController;
    }
}
