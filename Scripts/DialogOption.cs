using System.Collections.Generic;
using System.Xml.Serialization;

public class DialogOption {
    /// <summary>
    /// The unique ID of this option. Every option must have a unique ID. Audio will be formatted {id}.ogg
    /// </summary>
    [XmlElement("ID")]
    public string ID;
    
    /// <summary>
    /// The text captioning the audio
    /// </summary>
    [XmlElement("Caption")]
    public string Caption;

    /// <summary>
    /// What dialogue IDs need to have been said previously to say this
    /// </summary>
    [XmlArray("Preconditions"), XmlArrayItem("Precondition")]
    public List<string> Preconditions;

    /// <summary>
    /// What player lines trigger this
    /// </summary>
    [XmlArray("PlayerTriggers"), XmlArrayItem("Trigger")]
    public List<string> PlayerTriggers;
}