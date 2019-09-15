using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Windows;

public class RevManager : MonoBehaviour {

    public int SecondsBetweenPolling = 1;
    public string RevKey;
    public Text Caption;
    
    private float lastPoll = 0;
    private string revId = "";

    private bool locked = false;
    private float timeLocked;
    private RevNpc npc;

    private void Update() {
        if (npc != null && Vector3.Distance(transform.position, npc.gameObject.transform.position) > 5) {
            FreeNpcLock();
        }
    }

    public void BeginTranscription(string absolutePath) {
        // Unity is single-threaded so the lock{} statement will not work for us
        // Need to manually implement a lock system
        if (locked) {
            throw new InvalidOperationException("RevManager.BeginTranscription should not be called while a job is in progress.");
        } else {
            LockSelf(true);
            timeLocked = Time.time;
            StartCoroutine(RevRequest(absolutePath));
        }
    }

    public RevNpc LockedNpc() {
        return npc;
    }

    public bool LockConversationWithNpc(RevNpc requestingNpc) {
//        if (locked) {
//            return false;
//        }

        npc = requestingNpc;
        return true;
    }

    public void FreeNpcLock() {
        npc = null;
    }

    private void LockSelf(bool shouldLock) {
        locked = shouldLock;
    }

    private IEnumerator RevRequest(string absolutePath) {
        List<IMultipartFormSection> form = new List<IMultipartFormSection>();
 
        form.Add(new MultipartFormFileSection("media", File.ReadAllBytes(absolutePath), "Rev.m4a", "audio/m4a"));
 
        UnityWebRequest www = UnityWebRequest.Post ("https://api.rev.ai/speechtotext/v1/jobs", form);
        
        www.SetRequestHeader("Authorization","Bearer " + RevKey);
 
        yield return www.SendWebRequest();
        
        if (www.isNetworkError || www.isHttpError) {
            Debug.LogError(www.downloadHandler.text);
        } else {
            string id = JSON.Parse(www.downloadHandler.text)["id"];
            Debug.Log("Starting to poll");
            StartCoroutine(PollRev(id));
            npc.SpeakWhileLoading();
        }
    }

    private IEnumerator PollRev(string revId) {
        if (Time.time - lastPoll > SecondsBetweenPolling) {
            
            lastPoll = Time.time;
            
            UnityWebRequest www = UnityWebRequest.Get("https://api.rev.ai/speechtotext/v1/jobs/" + revId + "/transcript");
            www.SetRequestHeader("Authorization","Bearer " + RevKey);
            www.SetRequestHeader("Accept","application/vnd.rev.transcript.v1.0+json");
            
            yield return www.SendWebRequest();
        
            if (www.isNetworkError || www.isHttpError) {
                if (www.responseCode == 409) {
                    Debug.LogError("Error 409");
                    StartCoroutine(PollRev(revId));
                } else {
                    Debug.LogError(www.downloadHandler.text);
                }
                
                LockSelf(false);
                
            } else {
                if (www.responseCode == 200 && www.downloadHandler != null) {
                    HandleTranscriptionResponse(www.downloadHandler.text);
                }
            }
        } else {
            yield return new WaitForSeconds(SecondsBetweenPolling);
            StartCoroutine(PollRev(revId));
        }
    }

    private void HandleTranscriptionResponse(string responseText) {
        string playerText = ParseRevResponse(responseText);
        if (npc != null) {
            npc.HearPlayer(playerText);
        }
        
        LockSelf(false);
        
        Debug.Log("Time to receive response: " + (Time.time - timeLocked));
    }

    private string ParseRevResponse(string response) {
        Debug.Log("Starting the parsing");

        var words = JSON.Parse(response)["monologues"][0]["elements"];
        
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var wordContainer in words) {
            stringBuilder.Append(wordContainer.Value["value"].ToString());
        }

        string result = RemoveSpecialCharacters(stringBuilder.ToString());
        
        Debug.LogWarning(result);

        return result;
    }
    
    public static string RemoveSpecialCharacters(string str) {
        return Regex.Replace(str, "[^a-zA-Z0-9_ ]+", "", RegexOptions.Compiled);
    }
}
