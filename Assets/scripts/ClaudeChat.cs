using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Minimal in-game chat client for the Claude API (Messages endpoint).
/// Attach to any GameObject, wire up a TMP_InputField + TMP_Text in the Inspector
/// (or call SendMessageToClaude(text) directly from your own UI code).
///
/// API key resolution order:
///   1) apiKeyOverride field (Inspector) - for quick local testing only, NEVER commit a real key here
///   2) ANTHROPIC_API_KEY environment variable
///
/// On macOS, apps launched from Finder/Unity Hub (not a Terminal) do NOT inherit your
/// shell's exported env vars. Set it once with:
///   launchctl setenv ANTHROPIC_API_KEY "sk-ant-..."
/// then fully quit and reopen Unity Hub / the Unity Editor.
///
/// SECURITY: This calls the Claude API directly from the client. That's fine for testing
/// in the Editor. Before shipping a build to players, move this behind your own backend
/// so the API key never ships inside the game binary.
/// </summary>
public class ClaudeChat : MonoBehaviour
{
    [Header("UI (optional - wire these for a simple chat panel)")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text chatLog;

    [Header("API")]
    [Tooltip("Leave blank to read from the ANTHROPIC_API_KEY environment variable instead. Never commit a real key here.")]
    [SerializeField] private string apiKeyOverride;
    [SerializeField] private string model = "claude-opus-5";
    [SerializeField] private int maxTokens = 1024;

    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private readonly List<ChatMessage> history = new List<ChatMessage>();
    private bool requestInFlight;

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class RequestBody
    {
        public string model;
        public int max_tokens;
        public ChatMessage[] messages;
    }

    [Serializable]
    private class ContentBlock
    {
        public string type;
        public string text;
    }

    [Serializable]
    private class ResponseBody
    {
        public ContentBlock[] content;
        public string stop_reason;
    }

    /// <summary>Hook this to the TMP_InputField's "On Submit" / "On End Edit" event.</summary>
    public void SendCurrentInput()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;
        string userText = inputField.text;
        inputField.text = string.Empty;
        SendMessageToClaude(userText);
    }

    public void SendMessageToClaude(string userText)
    {
        if (requestInFlight)
        {
            AppendToLog("[Claude] Still waiting on the previous reply...");
            return;
        }

        history.Add(new ChatMessage { role = "user", content = userText });
        AppendToLog($"You: {userText}");
        StartCoroutine(PostToClaude());
    }

    private IEnumerator PostToClaude()
    {
        string apiKey = string.IsNullOrEmpty(apiKeyOverride)
            ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            : apiKeyOverride;

        if (string.IsNullOrEmpty(apiKey))
        {
            AppendToLog("[Claude] Missing API key. Set the ANTHROPIC_API_KEY environment variable " +
                        "(or the apiKeyOverride field for local testing only).");
            yield break;
        }

        var body = new RequestBody
        {
            model = model,
            max_tokens = maxTokens,
            messages = history.ToArray()
        };
        string json = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        requestInFlight = true;

        using (var request = new UnityWebRequest(Endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", apiKey);
            request.SetRequestHeader("anthropic-version", AnthropicVersion);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                AppendToLog($"[Claude] Error: {request.error}\n{request.downloadHandler.text}");
                requestInFlight = false;
                yield break;
            }

            var response = JsonUtility.FromJson<ResponseBody>(request.downloadHandler.text);
            var reply = new StringBuilder();
            if (response.content != null)
            {
                foreach (var block in response.content)
                {
                    if (block.type == "text") reply.Append(block.text);
                }
            }

            string replyText = reply.ToString();
            history.Add(new ChatMessage { role = "assistant", content = replyText });
            AppendToLog($"Claude: {replyText}");
        }

        requestInFlight = false;
    }

    private void AppendToLog(string line)
    {
        if (chatLog != null) chatLog.text += line + "\n";
        else Debug.Log(line);
    }
}
