using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 底部旁白显示条 — 对话播放时才出现，打字机逐字输出，播放完自动隐藏。
/// 挂载在 Desktop Canvas 下的 DialoguePanel 上。
/// </summary>
public class DialogueDisplay : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultSpeed = 0.04f;
    [SerializeField] private float hideDelay = 1.5f;

    private bool _isTyping;

    public bool IsTyping => _isTyping;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 播放一段对话（外部 await 此 IEnumerator 可等待完成）。
    /// </summary>
    public IEnumerator PlayText(string text, float speed)
    {
        _isTyping = true;

        // 提到最上层，不被终端等窗口遮挡
        transform.SetAsLastSibling();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(speed > 0 ? speed : defaultSpeed);
            }
        }

        // 停顿后自动隐藏
        yield return new WaitForSeconds(hideDelay);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        _isTyping = false;
    }

    /// <summary>
    /// 强制停止并隐藏。
    /// </summary>
    public void StopAndHide()
    {
        StopAllCoroutines();
        _isTyping = false;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}
