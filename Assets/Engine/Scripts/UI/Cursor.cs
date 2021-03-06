﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cursor : MonoBehaviour {

    public CursorMode mode;
    public bool oneUse;
    public bool keepSelectedIndex;
    public float moveCooldown;
    public List<GameObject> optionObjects;

    public AudioClip moveSound;
    public AudioClip okPressSound;
    public AudioClip cancelPressSound;

    public float movementTime;
    public bool enableStartAnimation;
    public bool enableIdleAnimation;
    [HideInInspector]
    public custom_inputs inputManager;

    [HideInInspector]
    public int selectedIndex;
    [HideInInspector]
    public int previousSelectedIndex;

    private List<ISelectable> options;
    private AudioSource audioSource;

    private bool active;
    private float timeSinceLastMove;
    private Vector3 startPosition;

    private Vector3 animVelocity = Vector3.zero;

    private Vector3 targetPosition;

    private const float cursorMovementFactor = 0.003f;
    private const float cursorSpeedFactor = 8;

    void OnEnable() {
        if (optionObjects.Count > 0) {
            if(!keepSelectedIndex){
                selectedIndex = 0;
                previousSelectedIndex = 0;
            }
        } else {
            Debug.LogWarning("A cursor without at least one valid option was enabled! Disabling!");
            gameObject.SetActive(false);
        }
        PlayerMachine player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMachine>();
        audioSource = player.audioSource;
        inputManager = player.gameManager.inputManager;

        options = new List<ISelectable>();
        foreach(GameObject obj in optionObjects){
            ISelectable option = obj.GetComponent<ISelectable>();
            options.Add(option);
            option.OnCursorInit(this);
        }

        targetPosition = options[selectedIndex].GetGrabPoint();

        if(enableStartAnimation){
            if(startPosition == Vector3.zero){
                startPosition = transform.position;
            }
            transform.position = startPosition;
        } else {
            transform.position = targetPosition;
        }
        
        options[selectedIndex].OnCursorSelect();

        active = true;
    }

    void Update() {
        if (active && IsDelayOver()) {
            if (inputManager.isInputDown[4] && options[selectedIndex].GetActive()) {
                audioSource.PlayOneShot(okPressSound);
                options[selectedIndex].OnOKPressed();
                if (oneUse) { active = false; }
            } else if (inputManager.isInputDown[5] && options[selectedIndex].GetActive()) {
                audioSource.PlayOneShot(cancelPressSound);
                options[selectedIndex].OnCancelPressed();
                if (oneUse) { active = false; }
            } else if (inputManager.isInput[0]) {
                if(mode == CursorMode.VERTICAL && selectedIndex > 0)
                    CursorMoved(-1);
                options[selectedIndex].OnSideKeyPressed(Utils.EnumDirection.UP);
            } else if (inputManager.isInput[1]) {
                if (mode == CursorMode.VERTICAL && selectedIndex < optionObjects.Count - 1)
                    CursorMoved(1);
                options[selectedIndex].OnSideKeyPressed(Utils.EnumDirection.DOWN);
            } else if (inputManager.isInput[2]) {
                if (mode == CursorMode.HORIZONTAL && selectedIndex > 0)
                    CursorMoved(-1);
                options[selectedIndex].OnSideKeyPressed(Utils.EnumDirection.LEFT);
            } else if (inputManager.isInput[3]) {
                if (mode == CursorMode.HORIZONTAL && selectedIndex < optionObjects.Count - 1)
                    CursorMoved(1);
                options[selectedIndex].OnSideKeyPressed(Utils.EnumDirection.RIGHT);
            }
        }

        if (enableIdleAnimation) {
            float offset = Mathf.Sin(Time.fixedTime * cursorSpeedFactor) * Screen.width * cursorMovementFactor;
            targetPosition = new Vector3(options[selectedIndex].GetGrabPoint().x + offset, options[selectedIndex].GetGrabPoint().y, options[selectedIndex].GetGrabPoint().z);
        }

        if (transform.position != targetPosition) {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref animVelocity, movementTime);
        }

        if (moveCooldown > 0) {
            timeSinceLastMove += Time.deltaTime;
        }

    }

    public void CursorMoved(int amount) {
        previousSelectedIndex = selectedIndex;

        selectedIndex += amount;
        options[selectedIndex] = options[selectedIndex];

        if (!enableIdleAnimation) {
            targetPosition = options[selectedIndex].GetGrabPoint();
        }

        audioSource.PlayOneShot(moveSound);
        options[previousSelectedIndex].OnCursorLeave();
        options[selectedIndex].OnCursorSelect();

        if (moveCooldown > 0) {
            timeSinceLastMove = 0;
        }
    }

    bool IsDelayOver() {
        if (timeSinceLastMove >= moveCooldown) {
            return true;
        } else {
            return false;
        }
    }

    public void SetActivityStatus(bool active){
        this.active = active;
        GetComponent<Image>().enabled = active;
        enableIdleAnimation = active;
    }

}

public enum CursorMode{
    VERTICAL, HORIZONTAL
}
