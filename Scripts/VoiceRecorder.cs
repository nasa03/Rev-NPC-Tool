using System;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour {

    public string DialogKey;
    public int RecordingTime = 5;
    public int AudioFrequency = 44100;

    private bool isRecording = false;
    private AudioClip currentRecording;
    private string audioDevice;

    private RevManager revManager;
    
    void Start() {
        audioDevice = Microphone.devices[0];
        revManager = GetComponent<RevManager>();
    }
    
    void Update() {
        if (!isRecording && Input.GetKeyDown(DialogKey)) {
            StartRecording();
        } else if (isRecording && Input.GetKeyUp(DialogKey)) {
            StopRecording();
        }
    }

    private void StartRecording() {
        Debug.Log("Starting to record!");
        currentRecording = Microphone.Start(audioDevice, true, 5, AudioFrequency);
        isRecording = true;
    }

    private void StopRecording() {
        Debug.Log("Recording stopped");
        Microphone.End(audioDevice);

        if (currentRecording != null) {

            string audioName = "rev-npc";
            string savedAudio = SavWav.Save(audioName, currentRecording);

            Debug.LogWarning(savedAudio);
            revManager.BeginTranscription(savedAudio);

        } else {
            Debug.LogError("Current Recording is null");
        }

        isRecording = false;
    }
}