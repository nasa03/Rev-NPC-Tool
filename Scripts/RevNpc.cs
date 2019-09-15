using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class RevNpc : MonoBehaviour {


    public float TimeBetweenDistanceChecks = 3;
    public float DistanceToConverse = 8;
    public float MinimumCorrelation = 0.8f;    // If the Dice Coefficient is below this, don't understand

    [Space(10)]
    public List<TextAsset> XmlDialogOptions;
    private Dictionary<string, DialogOption> dialogOptions;
    private Dictionary<string, DialogOption> loadingOptions;
    private Dictionary<string, DialogOption> noAnswerOptions;
    private HashSet<string> spokenIds = new HashSet<string>();
    private HashSet<string> occuredEvents = new HashSet<string>();

    [Space(10)]
    public List<TextAsset> NoAnswerIds;    // The Id's of any dialog options to play if NPC does not understand
    public List<TextAsset> LoadingIds;     // The Id's of any dialog options to play while loading

    [Space(10)]
    public string InitialDialogId;
        
    private float lastCheck = 0;
    private RevManager revManager;
    private Transform playerLocation;
    private AudioSource audioSource;
    
    // Start is called before the first frame update
    void Start() {
        revManager = GameObject.FindObjectOfType<RevManager>();
        playerLocation = revManager.gameObject.transform;
        dialogOptions = LoadDialogOptions(XmlDialogOptions);
        loadingOptions = LoadDialogOptions(LoadingIds);
        noAnswerOptions = LoadDialogOptions(NoAnswerIds);
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update() {
        FindPlayer();
    }

    private Dictionary<string, DialogOption> LoadDialogOptions(List<TextAsset> files) {
        Dictionary<string, DialogOption> options = new Dictionary<string, DialogOption>(); 
        
        files.ForEach(f => {
            DialogOption option = LoadDialogOption(f);
            options.Add(option.ID, option);
        });
    
        return options;
    }

    public void SpeakWhileLoading() {
        Debug.LogWarning("Speak while loading");
        Speak(GetLoadingOption());
    }

    private static DialogOption LoadDialogOption(TextAsset file) {
        XmlSerializer serializer = new XmlSerializer(typeof(DialogOption));
        
        using (MemoryStream stream = new MemoryStream(file.bytes)) {
            return (DialogOption)serializer.Deserialize(stream);
        }

    }

    public void SetDialogOccurence(string id) {
        spokenIds.Add(id);
    }
    
    public void SetEvent(string eventName) {
        occuredEvents.Add(eventName);
    }

    private bool HasPermissionToSay(List<string> preconditions) {

        if (!preconditions.Any()) {
            Debug.Log("No conditions");
            return true;
        }
        
        // If at least one precondition was spoken or has occured then it may say this
        foreach (var precondition in preconditions) {
            if (spokenIds.Contains(precondition) || occuredEvents.Contains(precondition)) {
                return true;
            }
        }

        return false;
    }

    private void Speak(DialogOption option) {
        Debug.Log(option.ID + ".m4a");
        AudioClip clip = (AudioClip)Resources.Load(option.ID + ".m4a");
        audioSource.clip = clip;
        
        if (!HasPermissionToSay(option.Preconditions)) {
            return;
        }
        
        revManager.Caption.text = option.Caption;
        SetDialogOccurence(option.ID);
        
        spokenIds.Add(option.ID);
        
        Debug.Log(option.Caption);
    }

    public void Speak(string id) {
        var option = dialogOptions[id];
        Speak(option);
        
    }
    
    private void FindPlayer() {
        if (revManager.LockedNpc() != null) {
            return;
        }
        
        float now = Time.time;
        if (now - lastCheck > TimeBetweenDistanceChecks) {
            lastCheck = now;

            bool selfIsLocked = revManager.LockedNpc() != null && revManager.LockedNpc().Equals(this);
            
            if (Vector3.Distance(transform.position, playerLocation.position) < DistanceToConverse) {
                InitiateDialog();
            }
        }
    }

    public void HearPlayer(string playerText) {
        Tuple<DialogOption, float> closest = GetClosestOption(playerText);
        if (closest.Item2 < MinimumCorrelation) {
            Debug.LogWarning(closest);
            Speak(GetInvalidResponseOption());
        } else {
            Speak(closest.Item1);
        }
    }

    private void InitiateDialog() {
        Debug.Log("Hey! I'm speaking to you!");

        if (revManager.LockConversationWithNpc(this)) {
            Speak(InitialDialogId);
        }
    }

    private DialogOption GetInvalidResponseOption() {
        System.Random rand = new System.Random();
        return noAnswerOptions.ElementAt(rand.Next(0, noAnswerOptions.Count)).Value;
    }
    
    // its 3:22 am and im tired
    private DialogOption GetLoadingOption() {
        System.Random rand = new System.Random();
        return loadingOptions.ElementAt(rand.Next(0, loadingOptions.Count)).Value;
    }

    private Tuple<DialogOption, float> GetClosestOption(string heard) {

        HashSet<string> wordsSet = StringToSet(heard);
        
        DialogOption maxOption = null;
        float maxCoefficient = -1;
        
        foreach (DialogOption option in dialogOptions.Values) {
            if (!HasPermissionToSay(option.Preconditions)) {
                continue;
            }
            
            foreach (string possiblePlayerChoice in option.PlayerTriggers) {
                float coefficient = DiceCoefficient(wordsSet, StringToSet(possiblePlayerChoice));
                if(coefficient > maxCoefficient) {
                    maxOption = option;
                    maxCoefficient = coefficient;
                }
            }
        }
        
        return new Tuple<DialogOption, float>(maxOption, maxCoefficient);
        
    }

    private static HashSet<string> StringToSet(string s) {
        return new HashSet<string>(s.Split(' '));
    }

    private float DiceCoefficient(HashSet<string> a, HashSet<string> b) {
        float top = a.Intersect(b).Count() * 2;
        float bottom = a.Count + b.Count;
        return top / bottom;
    }
}
