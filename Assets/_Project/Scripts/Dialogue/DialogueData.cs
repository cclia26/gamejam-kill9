using System.Collections.Generic;

/// <summary>
/// 单条对话数据。
/// </summary>
[System.Serializable]
public class DialogueLine
{
    /// <summary>唯一 ID，用于去重</summary>
    public string id;

    /// <summary>说话者："普罗米修斯" / "系统" / ""（旁白）</summary>
    public string speaker;

    /// <summary>对话正文</summary>
    public string text;

    /// <summary>打字机速度系数（1.0=默认速度，>1 变慢）</summary>
    public float delay = 1f;

    /// <summary>触发场景（"" 表示任意场景）</summary>
    public string triggerScene;

    /// <summary>触发条件 flag 名（"" 表示无条件）</summary>
    public string triggerCondition;

    /// <summary>触发所需的最小回车次数（-1 表示不限制）</summary>
    public int triggerEnterCount = -1;

    /// <summary>优先级，数字越大越优先</summary>
    public int priority;

    /// <summary>对话完成后设置的 flag</summary>
    public string onComplete;

    /// <summary>播放一次后不再触发</summary>
    public bool playOnce = true;
}

/// <summary>
/// 对话数据容器 — 用于 JSON 反序列化。
/// </summary>
[System.Serializable]
public class DialogueDataContainer
{
    public List<DialogueLine> dialogues = new List<DialogueLine>();
}
